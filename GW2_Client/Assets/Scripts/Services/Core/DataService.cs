using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
}

public interface IDataService
{
    public void Init();

}

public class DataService : IDataService
{
    public void Init()
    {

    }
}
