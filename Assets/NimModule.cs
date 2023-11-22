using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

public class NimModule : MonoBehaviour
{
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio KMAudio;
    public GameObject Matches;
    public GameObject Buttons;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private GameObject[,] _matches = new GameObject[5,15];
    private KMSelectable[] _takeButtons = new KMSelectable[5];
    private KMSelectable _doneButton = new KMSelectable();

    private int[] _rows = new int[5];

    protected void Start()
    {
        _moduleId = _moduleIdCounter++;

        _rows = new int[]{ 10, 12, 8, 15, 9};

        for (int i = 0; i < 5; i++)
        {
            var j = i;
            _takeButtons[i] = Buttons.transform.Find("Take (" + i.ToString() + ")").GetComponent<KMSelectable>();
            _takeButtons[i].OnInteract += delegate () { TakeFromRow(j); return false; };

            _takeButtons[i].transform.Translate(0, 0, -.027f * i);
        }

        for (int row = 0; row < 5; row++)
        {
            for (int i = 0; i < 15; i++)
            {
                _matches[row,i] = Matches
                    .transform.Find("Row (" + row.ToString() + ")")
                    .transform.Find("Match (" + i.ToString() + ")")
                    .gameObject;
                _matches[row, i].transform.Translate(
                    .006f * i + .009f * (i / 5) + UnityEngine.Random.Range (-.0008f, .0008f),
                    0,
                    -.027f * row + UnityEngine.Random.Range(-.0008f, .0008f)
                );
                _matches[row, i].transform.Rotate(0, UnityEngine.Random.Range(-5, 5), 0);
                Log("row={0}, i={1}", row, i);
            }
        }

        UpdateDisplay();
    }

    private void TakeFromRow(int i)
    {
        Log("Take from row {0}", i);
        _rows[i]--;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        for (int row = 0; row < 5; row++)
        {
            for (int i = 0; i < 15; i++)
            {
                _matches[row, i].gameObject.SetActive(_rows[row] > i);
            }
        }
    }

    protected bool HandlePress()
    {
        // TODO: match sound?
        // KMAudio.PlaySoundAtTransform("tick", this.transform);



        // TODO
        // BombModule.HandlePass();



        return false;
    }

    private void Log(string message, params object[] args)
    {
        Debug.LogFormat("[Nim #" + _moduleId + "] " + message, args);
    }

}
