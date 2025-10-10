using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public interface ISceneService
{
    public BaseScene CurrentScene { get { return GameObject.FindObjectOfType<BaseScene>(); } }
    public void LoadScene(Define.Scene type);
    public string GetSceneName(Define.Scene type);
    public void Clear();
}

public class SceneService : ISceneService
{
    public BaseScene CurrentScene { get { return GameObject.FindObjectOfType<BaseScene>(); } }

    public void Clear()
    {
        CurrentScene.Clear();
    }

    public string GetSceneName(Define.Scene type)
    {
        string name = System.Enum.GetName(typeof(Define.Scene), type);
        return name;
    }

    public void LoadScene(Define.Scene type)
    {
        // TODO : Scene상의 모든 GameObject 삭제

        SceneManager.LoadScene(GetSceneName(type));
    }
}
