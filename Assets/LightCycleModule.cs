using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using LightCycle;
using UnityEngine;

/// <summary>
/// On the Subject of Light Cycle
/// Created by Timwi
/// </summary>
public class LightCycleModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMColorblindMode ColorblindMode;

    public Material[] LitMats;
    public Material[] UnlitMats;
    public MeshRenderer[] Leds;
    public MeshRenderer[] ConfirmLeds;
    public TextMesh[] ColorblindTexts;

    public KMSelectable Button;

    private int[] _colors;
    private int _curLed;
    private bool _isSolved;
    private bool _solveAnimationDone;
    private int _seqIndex = 0;
    private int[] _solution;
    private bool _colorblindMode;
    private const string _colorNames = "RYGBMW";
    private static readonly string[] _cbNames = new[] { "Red", "Yellow", "Green", "Blue", "Magenta", "White" };

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    private static readonly string[][] _table = @"
5B;BR;MG;Y5;41;RW;64;16;23;3M;GY;W2
2R;6M;43;5B;R5;Y2;1G;MY;W6;34;BW;G1
MY;24;YR;35;W2;GB;1W;R3;5G;46;BM;61
56;63;14;M2;RY;2M;WR;BG;YW;3B;G1;45
BR;W2;23;14;MB;56;YW;RM;GY;6G;35;41
RY;2G;1M;Y5;5R;WB;63;B1;M4;G6;32;4W
Y1;54;2W;RY;1R;B3;6G;G6;MB;W5;42;3M
35;WY;G2;2B;5G;MR;B3;14;46;YM;6W;R1
RM;45;5W;B1;M6;32;WB;GY;YR;14;6G;23
WB;R6;5Y;41;25;Y3;MW;32;BG;GM;1R;64
64;B2;WG;R5;G1;2Y;YR;MB;16;3W;53;4M
64;B5;W6;1G;R2;4R;GW;3M;2B;Y3;5Y;M1
W3;3G;24;YM;M2;R5;6R;B6;GY;5B;1W;41
1Y;6M;21;GR;3G;5B;R4;43;W2;YW;B5;M6
R5;3G;23;W4;B2;1M;56;M1;4Y;GB;6R;YW
14;4B;62;3W;MR;Y6;BY;2G;5M;G5;R3;W1
5G;MB;4W;Y2;RM;W4;61;36;BY;15;GR;23
MG;56;GM;W5;Y2;R4;B1;1B;2R;43;6W;3Y
RY;65;5G;GB;WM;43;1W;B1;36;24;Y2;MR
G3;B2;6W;MB;15;Y4;5M;WR;46;3Y;2G;R1
51;W3;45;34;YW;1Y;BG;62;M6;GR;2M;RB
M6;6B;1G;35;WR;B4;GM;R1;2W;52;4Y;Y3
YM;B1;53;2G;32;R5;14;W6;4W;GR;MY;6B
42;RB;W5;YM;2Y;51;BR;G3;MG;36;6W;14
GY;1R;54;4G;3B;M6;25;Y2;R1;W3;BW;6M
GB;BG;15;M1;3M;R3;YW;6Y;52;46;WR;24
2R;RB;5G;W2;Y1;4Y;35;1M;BW;G6;64;M3
R4;W6;32;2W;4Y;65;BR;5G;YB;GM;M1;13
4B;B3;64;W1;MY;R6;G5;YW;52;2R;3G;1M
B6;M3;4B;14;25;Y1;GY;RW;WG;52;6M;3R
MR;2B;W5;6Y;B3;42;G1;Y6;5G;3M;RW;14
Y1;56;1W;W4;BG;G5;4M;2B;3R;63;M2;RY
34;WB;YG;5M;R1;GW;12;6Y;BR;M6;43;25
4G;65;Y4;GB;31;MY;53;1M;2R;R2;BW;W6
YB;R2;WR;53;1W;35;BM;G4;6Y;4G;21;M6
GY;31;5M;R2;6W;MB;Y6;24;4G;B5;1R;W3
".Replace("\r", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Split(';')).ToArray();

    // Arduino-related stuff (from Qkrisi)
    private readonly Dictionary<string, List<int>> colorValues = new Dictionary<string, List<int>>()
    {
        {"Red",  new List<int>() {255,0,0}},
        {"Yellow", new List<int>() {255,255,0}},
        {"Green", new List<int>() {0,255,0}},
        {"Blue", new List<int>() {0,0,255}},
        {"Magenta", new List<int>() {255,0,255}},
        {"White", new List<int>() {255,255,255}},
    };

#pragma warning disable 414
    private List<int> arduinoRGBValues = new List<int>() { 0, 0, 0 };
#pragma warning restore 414
    private bool arduinoConnected;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _colors = Enumerable.Range(0, 6).ToArray();
        _colors.Shuffle();
        _isSolved = false;
        _colorblindMode = ColorblindMode.ColorblindModeActive;
        arduinoRGBValues = colorValues[_cbNames[_colors[0]]];

        for (int i = 0; i < 6; i++)
        {
            Leds[i].material = UnlitMats[_colors[i]];
            ConfirmLeds[i].material = UnlitMats[5];
            ColorblindTexts[i].gameObject.SetActive(_colorblindMode);
            ColorblindTexts[i].text = _cbNames[_colors[i]];
        }

        StartCoroutine(Blinkenlights());

        Module.OnActivate = Activate;
    }

    void Activate()
    {
        Debug.LogFormat("[Light Cycle #{1}] Start sequence: {0}", _colors.Select(x => _colorNames[x]).JoinString(), _moduleId);

        _solution = _colors.ToArray();
        var serial = Bomb.GetSerialNumber();
        for (int i = 0; i < 6; i++)
        {
            var ch1 = convert(serial[i]);
            var ch2 = convert(serial[5 - i]);
            var entry = _table[ch1][ch2 / 3];
            var ix1 = _colorNames.IndexOf(entry[0]);
            if (ix1 == -1)
                ix1 = entry[0] - '1';
            else
                ix1 = Array.IndexOf(_solution, ix1);
            var ix2 = _colorNames.IndexOf(entry[1]);
            if (ix2 == -1)
                ix2 = entry[1] - '1';
            else
                ix2 = Array.IndexOf(_solution, ix2);
            var t = _solution[ix1];
            _solution[ix1] = _solution[ix2];
            _solution[ix2] = t;
            Debug.LogFormat("[Light Cycle #{5}] SN {0}{1}, swap {2}/{3}, sequence now: {4}", serial[i], serial[5 - i], entry[0], entry[1], _solution.Select(x => _colorNames[x]).JoinString(), _moduleId);
        }

        Button.OnInteract = delegate
        {
            ProcessButtonPress();
            return false;
        };
    }

    bool? ProcessButtonPress()
    {
        Button.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Button.transform);
        if (_isSolved)
            return null;

        if (_solution[_seqIndex] != _colors[_curLed])
        {
            Debug.LogFormat("[Light Cycle #{2}] Pressed button at {0}, but expected {1}.", _colorNames[_colors[_curLed]], _colorNames[_solution[_seqIndex]], _moduleId);
            Module.HandleStrike();
            return false;
        }
        else
        {
            ConfirmLeds[_curLed].material = LitMats[5];
            Debug.LogFormat("[Light Cycle #{1}] Pressed button at {0}: correct.", _colorNames[_colors[_curLed]], _moduleId);
            _seqIndex++;
            Audio.PlaySoundAtTransform("Ding" + _seqIndex, Leds[_curLed].transform);
            if (_seqIndex == _solution.Length)
            {
                _isSolved = true;
                StartCoroutine(Victory());
                return true;
            }
        }
        return null;
    }

    private IEnumerator Victory()
    {
        yield return new WaitForSeconds(.5f);
        arduinoRGBValues = new List<int>() { 0, 0, 0 };
        for (int i = 0; i < 6; i++)
        {
            Leds[i].material = LitMats[_colors[i]];
            Audio.PlaySoundAtTransform("Ding" + (i + 1), Button.transform);
            yield return new WaitForSeconds(.05f);
            Leds[i].material = UnlitMats[_colors[i]];
        }
        Module.HandlePass();
        _solveAnimationDone = true;
    }

    private IEnumerator Blinkenlights()
    {
        _curLed = 0;
        while (!_isSolved)
        {
            Leds[_curLed].material = LitMats[_colors[_curLed]];
            yield return new WaitForSeconds(.5f);
            arduinoRGBValues = colorValues[_cbNames[_colors[(_curLed + 1) % 6]]];
            Leds[_curLed].material = UnlitMats[_colors[_curLed]];
            _curLed = (_curLed + 1) % 6;
        }
    }

    private int convert(char ch)
    {
        return ch >= 'A' && ch <= 'Z' ? ch - 'A' : ch - '0' + 26;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} B R W M G Y [permissible colors are R (red), Y (yellow), G (green), B (blue), M (magenta), and W (white)] | !{0} colorblind";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        if (_isSolved)
            yield break;

        command = command.Trim().ToUpperInvariant();

        if (command.Equals("COLORBLIND"))
        {
            _colorblindMode = !_colorblindMode;
            for (int i = 0; i < 6; i++)
                ColorblindTexts[i].gameObject.SetActive(_colorblindMode);
            yield return null;
            yield break;
        }

        var colors = command.Where(ch => !char.IsWhiteSpace(ch)).Select(ch => new { Index = _colorNames.IndexOf(ch), Char = ch }).ToArray();
        if (colors.Length == 0)
            yield break;

        yield return null;

        var invalid = colors.FirstOrDefault(col => col.Index == -1);
        if (invalid != null)
        {
            yield return string.Format("sendtochaterror {0} is not a valid color.", invalid.Char);
            yield break;
        }

        for (int i = 0; i < colors.Length; i++)
        {
            // Wait for the light cycle to get to the requested color
            while (_colors[_curLed] != colors[i].Index)
                yield return new WaitForSeconds(.1f);

            // Push the button. ProcessButtonPress() has been written so that it will return
            //  true if this results in a solve;
            //  false if this results in a strike;
            //  null otherwise.
            var result = ProcessButtonPress();
            if (result == true)
            {
                // We need to communicate the solve to TwitchPlays because it hasn’t happened yet;
                // it happens after a flourish animation.
                yield return "solve";
                yield break;
            }
            else if (result == false)
            {
                // We do not communicate the strike because it already happened during the ProcessButtonPress() call.
                // TwitchPlays will process it automatically.
                yield break;
            }
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        while (!_isSolved)
        {
            while (_solution[_seqIndex] != _colors[_curLed])
                yield return true;

            Button.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        while (!_solveAnimationDone)
            yield return true;
    }
}
