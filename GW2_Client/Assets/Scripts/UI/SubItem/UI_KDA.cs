using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_KDA : UI_Base
{
    public static Action<int, int, int> OnKdaUpdate; // kill, death, assist

    enum Texts { Killnum, Deathnum, Timenum }

    private int _kill = 0;
    private int _death = 0;
    private int _assist = 0;

    public override void Init()
    {
        Bind<TMP_Text>(typeof(Texts));   // Text ¡æ TMP_Text


        OnKdaUpdate -= HandleKdaUpdate;
        OnKdaUpdate += HandleKdaUpdate;

        Refresh();
    }

    private void HandleKdaUpdate(int kill, int death, int assist)
    {
        _kill = kill;
        _death = death;
        _assist = assist;
        Refresh();
    }

    private void Refresh()
    {
        GetTMP((int)Texts.Killnum).text = _kill.ToString();
        GetTMP((int)Texts.Deathnum).text = _death.ToString();
        GetTMP((int)Texts.Timenum).text = _assist.ToString();
    }

    private void OnDestroy()
    {
        OnKdaUpdate -= HandleKdaUpdate;
    }
}
