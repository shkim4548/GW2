using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IResourceService
{
    public T Load<T>(string path) where T : Object;
    public GameObject Instantiate(string path, Transform parent = null);
    public void Destroy(GameObject go);
}

public class ResourceService : IResourceService
{
    public void Destroy(GameObject go)
    {
        if (go == null)
            return;

        // TODO : Object Pooling
        Object.Destroy(go);
    }

    public GameObject Instantiate(string path, Transform parent = null)
    {
        GameObject original = Load<GameObject>($"Prefabs/{path}");
        if(original == null)
        {
            Debug.Log($"Failed To Load Prefab : {path}");
            return null;
        }
        // TODO : Object Pooling
        GameObject go = Object.Instantiate(original, parent);
        go.name = original.name;
        return go;
    }

    public T Load<T>(string path) where T : Object
    {
    
        if (typeof(T) == typeof(GameObject))
        {
            string name = path;
            int index = name.LastIndexOf('/');
            if (index >= 0)
            {
                name = name.Substring(index + 1);
            }
            // TODO : ObjectPooling
        }
        return Resources.Load<T>(path);
    }
}
