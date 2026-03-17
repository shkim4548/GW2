using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_GameScene: UI_Scene
{
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

        UI_MiniMap miniMap = Get<UI_MiniMap>((int)SubItems.UI_Minimap);
        UI_KDA kda = Get<UI_KDA>((int)SubItems.UI_KDA);
        UI_StatusBox statBox = Get<UI_StatusBox>((int)SubItems.UI_StatusBox);
        UI_StatusBar statBar = Get<UI_StatusBar>((int)SubItems.UI_StatusBar);
        UI_CardPanel cardPanel = Get<UI_CardPanel>((int)SubItems.UI_CardPanel);
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
