using System;
using System.Collections.Generic;
using System.Linq;
using LightCycle;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Light Cycle
/// Created by Timwi
/// </summary>
public class LightCycleModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    void Start()
    {
        Debug.Log("[Light Cycle] Started");
    }

    void ActivateModule()
    {
        Debug.Log("[Light Cycle] Activated");
    }
}
