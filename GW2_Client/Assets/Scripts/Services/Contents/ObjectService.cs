using Google.Protobuf.Enum;
using Google.Protobuf.Struct;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObjectService
{
    public MyPlayerController MyPlayer { get; set; }
    public ObjectType GetObjectTypeById(int id);
    public void Add(ObjectInfo info, bool myPlayer = false);
    public void Remove(int id);
    public GameObject FindById(int id);
    public void Clear();
}

public class ObjectService : IObjectService
{
    protected IResourceService _resourceService;

    public MyPlayerController MyPlayer { get; set; }
    Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();

    public ObjectType GetObjectTypeById(int id)
    {
        int type = (id >> 24) & 0x7F;
        return (ObjectType)type;
    }

    public void Add(ObjectInfo info, bool myPlayer = false)
    {
        int objectId = info.ObjectId;
        int roomId = info.RoomId;
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
            if (myPlayer)
            {
                Vector3 initPos = new Vector3(info.PosInfo.X, info.PosInfo.Y, info.PosInfo.Z);
                // TODO : 캐릭터 타입 받아서 바꾸는 것으로 전환한다.
                //_resourceService = DI.Container.Resolve<IResourceService>();
                _resourceService = Bootstrapper.Instance.ResourceService;
                go = _resourceService.Instantiate("Player/Police");
                if(go == null)
                {
                    Debug.LogError("resourceService instantiate failed");
                }
                //go.transform.position = initPos;
                MyPlayer = go.GetComponent<MyPlayerController>();
                MyPlayer.transform.position = initPos;
                MyPlayer.Id = objectId;
                MyPlayer.RoomId = (int)roomId;

                _objects.Add(objectId, go);
            }
            else
            {
                Vector3 initPos = new Vector3(info.PosInfo.X, info.PosInfo.Y, info.PosInfo.Z);
                go = _resourceService.Instantiate("player/FireFighter");
                if( go == null )
                {
                    Debug.Log("resource service instantiate failed");
                }

                PlayerController pc = go.GetComponent<PlayerController>();
                pc.transform.position = initPos;
                pc.Id = objectId;
                pc._campType = (Google.Protobuf.Enum.CampType)info.TeamFlag;
                // TODO : Adding RoomId
                _objects.Add(objectId, go);
            }
        }
        else if (objectType == ObjectType.Minion)
        {
            Vector3 initPos = new Vector3(info.PosInfo.X, info.PosInfo.Y, info.PosInfo.Z);
            go = _resourceService.Instantiate("Minion/TestMinion");
            if (go == null)
            {
                Debug.Log("resource service instantiate failed");
            }

            MinionController mc = go.GetComponent<MinionController>();
            mc.transform.position = initPos;
            mc.Id = objectId;
            mc._campType = (Google.Protobuf.Enum.CampType)info.TeamFlag;
            // TODO : Adding RoomId
            _objects.Add(objectId, go);
        }
        else if(objectType == ObjectType.Turret)
        {
            Debug.Log($"[ObjectService] object type turret");
            Vector3 initPos = new Vector3(info.PosInfo.X, info.PosInfo.Y, info.PosInfo.Z);
            go = _resourceService.Instantiate("Turret/Tower");
            if(go == null)
            {
                Debug.LogError($"Turret is nullptr");
                return;
            }

            TurretController tc = go.GetComponent<TurretController>();
            tc.transform.position = initPos;
            tc.Id = objectId;
            tc._campType = (Google.Protobuf.Enum.CampType)info.TeamFlag;
            _objects.Add(objectId, go);
        }
        else
        {
            Debug.LogError($"[ObjectService] Adding Type is invalid!");
        }
    }

    public void Remove(int id)
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

    public GameObject FindById(int id)
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
