using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public interface IPoolService
{
    public void Init();
    public void CreatePool(GameObject original, int count = 5);
    public void Push(Poolable poolable);
    public Poolable Pop(GameObject original, Transform parent = null);
    public GameObject GetOriginal(string name);
    public void Clear();
}

public class PoolService : IPoolService
{
    // 중첩클래스, PoolService만 사용하는 클래스가 되는 것이다
    class Pool
    {
        public GameObject Original { get; private set; }
        public Transform Root { get; set; }
        Stack<Poolable> _poolStack = new Stack<Poolable>();

        public void Init(GameObject original, int count = 5)
        {
            Original = original;
            Root = new GameObject().transform;
            Root.name = $"{original.name}_Root";

            for (int i = 0; i < count; i++)
            {
                Push(Create());
            }
        }
        Poolable Create()
        {
            GameObject go = Object.Instantiate<GameObject>(Original);
            go.name = Original.name;
            return go.GetOrAddComponent<Poolable>();
        }

        public void Push(Poolable poolable)
        {
            if (poolable == null)
                return;

            poolable.transform.parent = Root;
            poolable.gameObject.SetActive(false);
            poolable.IsUsing = false;

            _poolStack.Push(poolable);
        }

        public Poolable Pop(Transform parent)
        {
            Poolable poolable;

            if (_poolStack.Count > 0)
                poolable = _poolStack.Pop();
            else
                poolable = Create();

            poolable.gameObject.SetActive(true);

            // DontDestroyOnLoad 해제 용도
            //if (parent == null)
            //poolable.transform.parent = Managers.Scene.CurretScene.transform;

            poolable.transform.parent = parent;
            poolable.IsUsing = true;

            return poolable;
        }
    }

    Dictionary<string, Pool> _pool = new Dictionary<string, Pool>();
    Transform _root;

    public void Clear()
    {
        throw new System.NotImplementedException();
    }

    public void CreatePool(GameObject original, int count = 5)
    {
        throw new System.NotImplementedException();
    }

    public GameObject GetOriginal(string name)
    {
        throw new System.NotImplementedException();
    }

    public void Init()
    {
        throw new System.NotImplementedException();
    }

    public Poolable Pop(GameObject original, Transform parent = null)
    {
        throw new System.NotImplementedException();
    }

    public void Push(Poolable poolable)
    {
        throw new System.NotImplementedException();
    }
}
