using System;
using System.Collections;
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

    public Material[] LitMats;
    public Material[] UnlitMats;
    public MeshRenderer[] Leds;

    private int[] _order;
    private int _curLed;

    void Start()
    {
        _order = Enumerable.Range(0, 6).ToArray();
        _order.Shuffle();

        for (int i = 0; i < 6; i++)
            Leds[i].material = UnlitMats[_order[i]];

        StartCoroutine(Blinkenlights());
    }

    private IEnumerator Blinkenlights()
    {
        _curLed = 0;
        while (true)
        {
            Leds[_curLed].material = LitMats[_order[_curLed]];
            yield return new WaitForSeconds(.5f);
            Leds[_curLed].material = UnlitMats[_order[_curLed]];

            _curLed = (_curLed + 1) % 6;
        }
    }

    void ActivateModule()
    {

    }
}
