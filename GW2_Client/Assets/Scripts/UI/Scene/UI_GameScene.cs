using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_GameScene: UI_Scene
{
    private UI_StatusBar _statusBar;

    // Start is called before the first frame update
    enum SubItems
    {
        UI_StatusBar,
        UI_StatusBox,
        UI_Deck,
        UI_Minimap,
        UI_KDA,
        UI_CardPanel,
    }

    public override void Init()
    {
        base.Init();

        Bind<GameObject>(typeof(SubItems));
        IUIService uiService = Bootstrapper.Instance.UIService;
        //_statusBar = Get<UI_StatusBar>((int)SubItems.UI_StatusBar);
        _statusBar = GetObject((int)SubItems.UI_StatusBar).GetComponent<UI_StatusBar>();

        _statusBar.Init();
    }

    public void InitCards()
    {

    }

    public void InitKdaPanel()
    {

    }

    public void InitStatBox()
    {

    }

    public void InitMiniMap()
    {

    }

    public void HandleHpBar()
    {

    }

    public void HandleManaUI()
    {

    }
}
