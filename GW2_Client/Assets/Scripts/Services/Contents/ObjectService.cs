using Google.Protobuf.Enum;
using Google.Protobuf.Struct;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
        //int objectId = Bootstrapper.Instance.NetworkService.GetNetworkId();
        Debug.Log($"[ObjectService::Add] objectId : {objectId}");
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
            Vector3 initPos = new Vector3(info.PosInfo.X, info.PosInfo.Y, info.PosInfo.Z);
            _resourceService = Bootstrapper.Instance.ResourceService;

            string prefabPath = GetPlayerPrefabPath(info.Name, myPlayer);
            go = _resourceService.Instantiate(prefabPath);
            if (go == null)
            {
                Debug.LogError($"[ObjectService] Player prefab not found: {prefabPath}");
                return;
            }

            if (myPlayer)
            {
                NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = false;
                go.transform.position = initPos;
                if (agent != null) agent.enabled = true;

                MyPlayer = go.GetComponent<MyPlayerController>();
                MyPlayer.Id = objectId;
                MyPlayer.RoomId = (int)roomId;
                MyPlayer._campType = (CampType)info.TeamFlag;
                MyPlayer.CampType = (CampType)info.TeamFlag;
                MyPlayerController.OnStatInfoUpdate?.Invoke(info.StatInfo);
            }
            else
            {
                PlayerController pc = go.GetComponent<PlayerController>();
                pc.transform.position = initPos;
                pc.Id = objectId;
                pc._campType = (CampType)info.TeamFlag;
            }

            _objects.Add(objectId, go);
        }
        else if (objectType == ObjectType.Minion)
        {
            Vector3 initPos = new Vector3(info.PosInfo.X, info.PosInfo.Y, info.PosInfo.Z);
            //go = _resourceService.Instantiate("Minion/TestMinion");
            if(info.TeamFlag == (int)Google.Protobuf.Enum.CampType.CampHuman)
            {
                go = _resourceService.Instantiate("Minion/HumanRangeMinion");
            }
            else
            {
                go = _resourceService.Instantiate("Minion/CyborgRangeMinion");

            }
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
        else if(objectType == ObjectType.Nexus)
        {
            Vector3 initPos = new Vector3(info.PosInfo.X, info.PosInfo.Y, info.PosInfo.Z);
            //string prefabName = info.TeamFlag == (int)CampType.CampHuman
            //    ? "Nexus/HumanNexus"
            //    : "Nexus/CyborgNexus";
            Debug.Log($"[ObjectService] Nexus Add objectId={objectId} team={info.TeamFlag} pos={initPos}");

            string prefabName = "Nexus/Nexus";
            go = _resourceService.Instantiate(prefabName);
            if (go == null) 
            {
                Debug.LogError("[ObjectService] Nexus prefab not found");
                return; 
            }

            NexusController nc = go.GetComponent<NexusController>();
            nc.transform.position = initPos;
            nc.Id = objectId;
            nc._campType = (Google.Protobuf.Enum.CampType)info.TeamFlag;
            _objects.Add(objectId, go);
        }
        else if (objectType == ObjectType.Baron)
        {
            Vector3 initPos = new Vector3(info.PosInfo.X, info.PosInfo.Y, info.PosInfo.Z);
            go = _resourceService.Instantiate("Minion/NeutralMob");
            if (go == null)
            {
                Debug.LogError("[ObjectService] Baron prefab not found");
                return;
            }

            BaronController bc = go.GetComponent<BaronController>();
            bc.transform.position = initPos;
            bc.Id = objectId;
            bc._campType = CampType.CampNeutural;
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
        {
            return;
        }
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
    private string GetPlayerPrefabPath(string playerTypeName, bool isMyPlayer)
    {
        string prefix = isMyPlayer ? "My" : "";
        switch (playerTypeName)
        {
            case "Police": return $"Player/{prefix}Police";
            case "FireFighter": return $"Player/{prefix}FireFighter";
            case "Monk": return $"Player/{prefix}Monk";
            case "LightSabre": return $"Player/{prefix}LightSabre";
            default:
                Debug.LogWarning($"[ObjectService] Unknown player type: {playerTypeName}, defaulting to Police");
                return $"Player/{prefix}Police";
        }
    }

}
