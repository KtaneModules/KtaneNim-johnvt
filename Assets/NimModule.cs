using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NimModule : MonoBehaviour
{
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio KMAudio;
    public GameObject Matches;
    public GameObject Buttons;

    private GameObject[,] _matches = new GameObject[5, 15];
    private KMSelectable[] _rowButtons = new KMSelectable[5];

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved = false;
    private int? _fromRow = null;
    private int _take = 0;
    private int[] _rows = new int[5];
    private Coroutine _timer;
    private float _elapsedTime;
    private bool _thinking = false;

    protected void Start()
    {
        _moduleId = _moduleIdCounter++;
        Setup();

        for (int i = 0; i < _rows.Length; i++)
        {
            var j = i;
            _rowButtons[i] = Buttons.transform.Find("Row (" + i.ToString() + ")").GetComponent<KMSelectable>();
            _rowButtons[i].transform.localPosition += new Vector3(0, 0, -3.2f * i);
            _rowButtons[i].OnInteract += delegate () { TakeFromRow(j); return false; };
        }

        for (int row = 0; row < _rows.Length; row++)
        {
            for (int i = 0; i < 15; i++)
            {
                _matches[row, i] = Matches
                    .transform.Find("Row (" + row.ToString() + ")")
                    .transform.Find("Match (" + i.ToString() + ")")
                    .gameObject;

                _matches[row, i].transform.localPosition += new Vector3(
                    .8f * i + .9f * (i / 5) + UnityEngine.Random.Range(-.08f, .08f),
                    0,
                    -3.2f * row + UnityEngine.Random.Range(-.08f, .08f)
                );

                _matches[row, i].transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(-5, 5), 0);
            }
        }

        UpdateDisplay();
    }

    private void TakeFromRow(int row)
    {
        if (_moduleSolved) return;

        // You are already taking from another row
        if (_fromRow != null && row != _fromRow) return;

        // You can't take from an empty row
        if (_rows[row] == 0) return;

        // If you weren't taking from this row already, now you are
        _fromRow = row;

        // Reset timer to detect when defuser is done taking
        if (_timer != null) StopCoroutine(_timer);
        _timer = StartCoroutine(Timer());

        _take += 1;
        _rows[row]--;

        GetComponent<KMSelectable>().AddInteractionPunch(.25f);
        GetComponent<KMAudio>().PlaySoundAtTransform("match", transform);
        UpdateDisplay();
    }

    private IEnumerator TakeFromRowAnim(int row, int newCount)
    {
        while (_rows[row] > newCount)
        {
            _rows[row]--;

            GetComponent<KMSelectable>().AddInteractionPunch(.25f);
            GetComponent<KMAudio>().PlaySoundAtTransform("match", transform);
            UpdateDisplay();
            yield return new WaitForSeconds(.2f);
        }

        _thinking = false;
        UpdateDisplay();
        Log("New configuration: {0}. Nim sum: {1}", string.Join(",", Array.ConvertAll(_rows, x => x.ToString())), NimSum(_rows));

        if (_rows.Sum() == 0)
        {
            Log("I win. You get a strike. Let's try again.", _take, _fromRow);
            BombModule.HandleStrike();
            Setup();
            UpdateDisplay();
        }
    }

    private IEnumerator Timer()
    {
        _elapsedTime = 0f;
        while (_elapsedTime < 2f + _rows.Sum() * .07)
        {
            if (_elapsedTime > 2f)
            {
                _thinking = true;
                UpdateDisplay();
            }
            yield return null;
            _elapsedTime += Time.deltaTime;
        }
        DefuserDoneTaking();
    }

    private void Setup()
    {
        int tries = 0;
        while (true)
        {
            tries++;
            if (tries == 100)
            {
                Log("Tried {0} times, giving up", tries);
                return;
            }

            for (int i = 0; i < _rows.Length; i++)
            {
                // Random row size between 5 and 15, leaning towards the higher end
                // Decrease second argument of Pow to increase the curve towards bigger numbers
                _rows[i] = (int)Math.Floor(Math.Pow(UnityEngine.Random.Range(0f, 1f), .5) * 11) + 5;
            }

            // Top row needs status light to fit on the right
            if (_rows[0] > 12) continue;

            // It cannot be a winning condition
            if (NimSum(_rows) == 0) continue;

            // It cannot contain 4 or 5 the same rows
            if (_rows.GroupBy(x => x).Any(g => g.Count() >= 4)) continue;

            // It cannot contain 2 pairs either
            if (_rows.GroupBy(x => x).Count(g => g.Count() == 2) == 2) continue;

            // You should not be able to remove a complete row right away (row size equals nim sum)
            if (Array.Exists(_rows, i => i == NimSum(_rows))) continue;

            // We found a config that meets all criteria
            break;
        }
        Log("Initial configuration: {0}. Nim sum: {1}", string.Join(",", Array.ConvertAll(_rows, x => x.ToString())), NimSum(_rows));
    }

    private int NimSum(int[] arr)
    {
        int nimSum = 0;

        foreach (int num in arr) nimSum ^= num;

        return nimSum;
    }

    private void DefuserDoneTaking()
    {
        if (_moduleSolved) return;

        Log("You take {0} from row {1}", _take, _fromRow+1);
        _take = 0;
        _fromRow = null;

        if (_rows.Sum() == 0)
        {
            Log("Module solved!");
            _moduleSolved = true;
            BombModule.HandlePass();
            return;
        }

        Log("New configuration: {0}. Nim sum: {1}", string.Join(",", Array.ConvertAll(_rows, x => x.ToString())), NimSum(_rows));

        int totalNimSum = NimSum(_rows);

        // Player made a mistake somewhere, we keep playing optimal until strike
        if (totalNimSum != 0)
        {

            List<KeyValuePair<int, int>> possibleMoves = new List<KeyValuePair<int, int>>();

            for (int row = 0; row < _rows.Length; row++)
            {
                int newRowSize = NimSum(new int[] { totalNimSum, _rows[row] });
                if (newRowSize < _rows[row])
                {
                    possibleMoves.Add(new KeyValuePair<int, int>(row, newRowSize));
                }
            }

            // Choose a random move from the list of possible moves
            int randomMoveIndex = UnityEngine.Random.Range(0, possibleMoves.Count);
            KeyValuePair<int, int> selectedMove = possibleMoves[randomMoveIndex];

            int selectedRow = selectedMove.Key;
            int selectedNewRowSize = selectedMove.Value;

            Log("You made a mistake, so I'm playing optimal. I take {0} from row {1}", _rows[selectedRow] - selectedNewRowSize, selectedRow + 1);
            StartCoroutine(TakeFromRowAnim(selectedRow, selectedNewRowSize));
        }

        // Player played optimal
        else
        {
            int tries = 1;

            while (true)
            {
                tries++;
                int[] rowsCopy = _rows.ToArray();

                // Pick a random row until it's not empty
                int row = UnityEngine.Random.Range(0, rowsCopy.Length);
                if (rowsCopy[row] == 0) continue;

                // Take a random number of matches
                int newRowSize = UnityEngine.Random.Range(0, rowsCopy[row]);
                rowsCopy[row] = newRowSize;

                // We only try 10 times because sometimes we cannot prevent equal rows
                if (tries > 10)
                {
                    // We prefer not having 4 or 5 of the same rows
                    if (rowsCopy.GroupBy(x => x).Any(g => g.Count() >= 4)) continue;

                    // We prefer not having 2 pairs
                    if (rowsCopy.GroupBy(x => x).Count(g => g.Count() == 2) == 2) continue;

                    // We prefer you're not be able to remove a complete row right away (row size equals nim sum)
                    if (Array.Exists(rowsCopy, i => i == NimSum(rowsCopy))) continue;
                }

                Log("You are playing optimal, so I'm playing random. I take {0} from row {1}", rowsCopy[row] - newRowSize, row+1);
                StartCoroutine(TakeFromRowAnim(row, newRowSize));
                break;
            }
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        // Show/hide matches
        for (int row = 0; row < _rows.Length; row++)
        {
            for (int i = 0; i < 15; i++)
            {
                _matches[row, i].gameObject.SetActive(_rows[row] > i);
            }
        }

        // (De)activate buttons
        for (int i = 0; i < _rows.Length; i++)
        {
            _rowButtons[i].transform.Find("Highlight").gameObject.SetActive(
                !_moduleSolved
                &&
                !_thinking
                &&
                (_fromRow == null || _fromRow == i)
                &&
                _rows[i] > 0
            );
        }
    }

    private void Log(string message, params object[] args)
    {
        Debug.LogFormat("[Nim #" + _moduleId + "] " + message, args);
    }

    // Twitch Plays help message
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} take 2 5 [Takes 2 matches from the bottom row]";
    #pragma warning restore 414

    // Twitch Plays command processor
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        // Command must start with the word "take"
        if (parameters[0].EqualsIgnoreCase("take"))
        {
            // Command must be 3 parameters total
            if (parameters.Length != 3)
                yield break;
            // The 2nd and 3rd parameters must be integers
            int temp;
            int temp2;
            if (!int.TryParse(parameters[1], out temp) || !int.TryParse(parameters[2], out temp2))
                yield break;
            // The 3rd parameter must be a valid row and the 2nd parameter must be a positive integer
            if (temp2 < 1 || temp2 > 5 || temp < 1)
                yield break;
            // Check if 2nd parameter is asking for more than the row has
            if (_rows[temp2 - 1] < temp)
            {
                yield return "sendtochaterror The specified row does not have the amount of matches you wish to take!";
                yield break;
            }
            // Prevent taking if the module is thinking
            if (_thinking)
            {
                yield return "sendtochaterror Matches cannot be taken while the module is thinking!";
                yield break;
            }
            // Command has passed all tests, return something to tell TP it's valid and to focus on the mod
            yield return null;
            // Take the specified number of matches from the specified row
            for (int i = 0; i < temp; i++)
            {
                _rowButtons[temp2 - 1].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            // The module takes a while to process after interaction, so tell TP to award strikes/solves to whoever executed this command
            yield return NimSum(_rows) != 0 ? "strike" : "solve";
        }
    }
}
