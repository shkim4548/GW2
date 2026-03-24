using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_KDA : UI_Base
{
    public static Action<int, int, int> OnKdaUpdate; // kill, death, assist

    enum Texts { Kill_txt, Death_txt, Assist_txt }

    private int _kill = 0;
    private int _death = 0;
    private int _assist = 0;

    public override void Init()
    {
        Bind<Text>(typeof(Texts));

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
        GetText((int)Texts.Kill_txt).text = _kill.ToString();
        GetText((int)Texts.Death_txt).text = _death.ToString();
        GetText((int)Texts.Assist_txt).text = _assist.ToString();
    }

    private void OnDestroy()
    {
        OnKdaUpdate -= HandleKdaUpdate;
    }
}
