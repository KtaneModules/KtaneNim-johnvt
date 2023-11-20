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
    public KMSelectable Match;

    private List<KMSelectable> _matches;

    protected void Start()
    {
        for (var i = 0; i < 1; i++)
        {
            KMSelectable match = Instantiate(Match);
            _matches.Add(match);
        }
        Match.OnInteract += HandlePress;
	}

    protected bool HandlePress()
    {
        // TODO: match sound?
        // KMAudio.PlaySoundAtTransform("tick", this.transform);



        // TODO
        // BombModule.HandlePass();



        return false;
    }

}
