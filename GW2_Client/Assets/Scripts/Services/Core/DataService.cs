using System;
using System.Collections.Generic;
using UnityEngine;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
}

public interface IDataService
{
    Dictionary<int, Data.CardInfo> CardDict { get; }
    CardIconSO CardIcons { get; }
    void Init();
}

public class DataService : IDataService
{
    public Dictionary<int, Data.CardInfo> CardDict { get; private set; }

    public CardIconSO CardIcons { get; private set; }

    public void Init()
    {
        CardDict = LoadJson<Data.CardInfoList, int, Data.CardInfo>("Json/Stats");
        CardIcons = Resources.Load<CardIconSO>("Data/CardIconSO");
        CardIcons?.Init();
        //foreach (KeyValuePair<int, Data.CardInfo> card in CardDict)
        //{
        //    Debug.Log($"{card.Key}, {card.Value.name}");
        //}
    }

    private Dictionary<Key, Value> LoadJson<Loader, Key, Value>(string path)
        where Loader : ILoader<Key, Value>
    {
        TextAsset textAsset = Resources.Load<TextAsset>(path);
        Loader loader = JsonUtility.FromJson<Loader>(textAsset.text);
        return loader.MakeDict();
    }
}
