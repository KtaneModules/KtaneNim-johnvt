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

    private GameObject[,] _matches = new GameObject[5,15];
    private KMSelectable[] _rowButtons = new KMSelectable[5];

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _solved = false;
    private int? _fromHeap = null;
    private int _take = 0;
    private int[] _heaps = new int[5];
    private Coroutine _timer;
    private float _elapsedTime;

    protected void Start()
    {
        _moduleId = _moduleIdCounter++;
        Setup();

        for (int i = 0; i < _heaps.Length; i++)
        {
            var j = i;
            _rowButtons[i] = Buttons.transform.Find("Row (" + i.ToString() + ")").GetComponent<KMSelectable>();
            _rowButtons[i].OnInteract += delegate () { TakeFromHeap(j); return false; };
            _rowButtons[i].transform.Translate(0, 0, -.026f * i);
        }

        for (int heap = 0; heap < _heaps.Length; heap++)
        {
            for (int i = 0; i < 15; i++)
            {
                _matches[heap,i] = Matches
                    .transform.Find("Heap (" + heap.ToString() + ")")
                    .transform.Find("Match (" + i.ToString() + ")")
                    .gameObject;
                _matches[heap, i].transform.Translate(
                    .007f * i + .008f * (i / 5) + UnityEngine.Random.Range(-.0008f, .0008f),
                    0,
                    -.026f * heap + UnityEngine.Random.Range(-.0008f, .0008f)
                );
                _matches[heap, i].transform.Rotate(0, UnityEngine.Random.Range(-5, 5), 0);
            }
        }

        UpdateDisplay();
    }

    private void TakeFromHeap(int heap)
    {
        if (_solved) return;

        // Reset countdown to Done
        if (_timer != null)
            StopCoroutine(_timer);
        _timer = StartCoroutine(Timer());

        // You are already taking from another heap
        if (_fromHeap != null && heap != _fromHeap) return;

        // If you weren't taking from this heap already, now you are
        _fromHeap = heap;

        // TODO: match sound?
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

        _take++;
        _heaps[heap]--;

        if (_heaps.Sum() == 0)
        {
            _solved = true;
            BombModule.HandlePass();
        }

        UpdateDisplay();
    }

    private IEnumerator Timer()
    {
        _elapsedTime = 0f;
        while (_elapsedTime < 2f)
        {
            yield return null;
            _elapsedTime += Time.deltaTime;
        }
        Done();
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

            for (int i = 0; i < _heaps.Length; i++)
            {
                // Random heap size between 5 and 15, leaning towards the higher end
                // Decrease second argument of Pow to increase the curve towards bigger numbers
                _heaps[i] = (int)Math.Floor(Math.Pow(UnityEngine.Random.Range(0f, 1f), .5) * 11) + 5;
            }

            // Top heap needs status light to fit on the right
            if (_heaps[0] > 12) continue;

            // It cannot be a winning condition
            if (NimSum(_heaps) == 0) continue;

            // It cannot contain 4 or 5 the same heaps
            if (_heaps.GroupBy(x => x).Any(g => g.Count() >= 4)) continue;

            // It cannot contain 2 pairs either
            if (_heaps.GroupBy(x => x).Count(g => g.Count() == 2) == 2) continue;

            // You should not be able to remove a complete row right away (heap size equals nim sum)
            if (Array.Exists(_heaps, i => i == NimSum(_heaps))) continue;

            break;
        }

        Log("Solution found in {0} tries. Nim sum {1}", tries, NimSum(_heaps));
    }

    private int NimSum(int[] arr)
    {
        int nimSum = 0;

        foreach (int num in arr) nimSum ^= num;

        return nimSum;
    }

    private void Done()
    {
        Log("Taken {0} from heap {1}", _take, _fromHeap);
        _take = 0;
        _fromHeap = null;

        int totalNimSum = NimSum(_heaps);

        // Player made a mistake somewhere, we keep playing optimal until strike
        if (totalNimSum != 0)
        {
            for (int i = 0; i < _heaps.Length; i++)
            {
                int heapSum = NimSum(new int[] { totalNimSum, _heaps[i] });
                if (heapSum < _heaps[i])
                {
                    _heaps[i] = heapSum;
                    break;
                }
            }

            if (_heaps.Sum() == 0) {
                BombModule.HandleStrike();
                Setup();
            }
        }

        // Player played optimal
        else
        {
            while (true) {

                // Pick a random heap until it's not empty
                int heap = UnityEngine.Random.Range(0, _heaps.Length);
                if (_heaps[heap] == 0) continue;

                // Take a random number of matches (actually set it to a random number that's less than current)
                _heaps[heap] = UnityEngine.Random.Range(0, _heaps[heap]);
                break;
            }
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        // Show/hide matches
        for (int heap = 0; heap < _heaps.Length; heap++)
        {
            for (int i = 0; i < 15; i++)
            {
                _matches[heap, i].gameObject.SetActive(_heaps[heap] > i);
            }
        }

        // (De)activate buttons
        for (int i = 0; i < _heaps.Length; i++)
        {
            _rowButtons[i].transform.Find("Highlight").gameObject.SetActive(
                !_solved
                &&
                (_fromHeap == null || _fromHeap == i)
                &&
                _heaps[i] > 0
            );
        }
    }

    private void Log(string message, params object[] args)
    {
        Debug.LogFormat("[Nim #" + _moduleId + "] " + message, args);
    }

}
