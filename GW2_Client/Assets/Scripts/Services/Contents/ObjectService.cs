using Google.Protobuf.Enum;
using Google.Protobuf.Struct;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObjectService
{
    public MyPlayerController MyPlayer { get; set; }
    public ObjectType GetObjectTypeById(ulong id);
    public void Add(ObjectInfo info, bool myPlayer = false);
    public void Remove(ulong id);
    public GameObject FindById(ulong id);
    public void Clear();
}

public class ObjectService : IObjectService
{
    protected IResourceService _resourceService;

    public MyPlayerController MyPlayer { get; set; }
    Dictionary<ulong, GameObject> _objects = new Dictionary<ulong, GameObject>();

    public ObjectType GetObjectTypeById(ulong id)
    {
        ulong type = (id >> 24) & 0x7F;
        return (ObjectType)type;
    }

    public void Add(ObjectInfo info, bool myPlayer = false)
    {
        ulong objectId = info.ObjectId;
        GameObject go;
        if (_objects.TryGetValue(objectId, out go))
            return;

        ObjectType objectType = info.ObjectType;
        Debug.Log($"[ObjectService] ObjectType : {objectType}");
        if (objectType == ObjectType.None)
        {
            Debug.LogError($"[ObjectService] Adding Type is invalid!");
        }
        else if (objectType == ObjectType.Player)
        {
            Vector3 initPos = new Vector3(info.PosInfo.X, info.PosInfo.Y, info.PosInfo.Z);
            // TODO : 캐릭터 타입 받아서 바꾸는 것으로 전환한다.
            _resourceService = DI.Container.Resolve<IResourceService>();
            go = _resourceService.Instantiate("Player/Police");
            go.transform.position = initPos;
            _objects.Add(objectId, go);

        }
        else if (objectType == ObjectType.Monster)
        {
            
        }
        else
        {
            Debug.LogError($"[ObjectService] Adding Type is invalid!");
        }
    }

    public void Remove(ulong id)
    {
        if (MyPlayer != null && MyPlayer.Id == id)
            return;
        if (_objects.ContainsKey(id) == false)
            return;

        GameObject go = FindById(id);
        if (go == null)
            return;

        _objects.Remove(id);
        _resourceService.Destroy(go);
    }

    public GameObject FindById(ulong id)
    {
        GameObject go = null;
        _objects.TryGetValue(id, out go);
        return go;
    }

    public void Clear()
    {
        foreach (GameObject obj in _objects.Values)
            _resourceService.Destroy(obj);
        _objects.Clear();
        MyPlayer = null;
    }
}
