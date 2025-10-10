using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class BaseScene : MonoBehaviour
{
    [Inject]
    IResourceService _resourceService;
    [Inject]
    protected Lazy<IUIService> _uiService;

    public Define.Scene SceneType { get; protected set; } = Define.Scene.Unknown;


    void Start()
    {
        Init();
    }

    protected virtual void Init()
    {
        UnityEngine.Object obj = GameObject.FindObjectOfType(typeof(EventSystem));
        if (obj == null)
            _resourceService.Instantiate("UI/EventSystem").name = "@EventSystem";
    }

    public abstract void Clear();
}
