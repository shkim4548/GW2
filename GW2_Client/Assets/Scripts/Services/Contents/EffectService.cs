using Data;
using System.Collections.Generic;
using UnityEngine;

public class EffectService : MonoBehaviour
{
    public static EffectService Instance { get; private set; }

    private Dictionary<int, SkillEffectEntry> _map = new();

    void Awake()
    {
        Instance = this;
        Debug.Log("[EffectService] Awake Called");
        var data = Resources.Load<SkillEffectData>("Data/SkillEffectData");
        if (data == null)
        {
            Debug.LogWarning("[EffectService] SkillEffectData not found at Resources/Data/");
            return;
        }
        foreach (var entry in data.entries)
            _map[entry.skillId] = entry;

        Debug.Log($"[EffectService] _map.Count={_map.Count}");
    }

    // 시그니처 변경: attackerPos + worldPos 분리
    public void SpawnEffect(int skillId, Vector3 attackerPos, Vector3 worldPos, Vector3 dir)
    {
        if (!_map.TryGetValue(skillId, out var entry)) return;
        if (entry.effectPrefab == null) return;

        Vector3 spawnPos = entry.spawnType switch
        {
            EffectSpawnType.ClickPoint => worldPos,
            _ => attackerPos,   // Self, Projectile 모두 시전자 위치
        };

        // 바닥 평행 회전 (Y축 고정, XZ 평면으로 flatten)
        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
        Quaternion rot = flatDir.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(flatDir)
            : Quaternion.identity;

        GameObject fx = Instantiate(entry.effectPrefab, spawnPos, rot);
        Destroy(fx, entry.duration);
    }

    public void SpawnHitEffect(int skillId, Vector3 pos)
    {
        if (!_map.TryGetValue(skillId, out var entry)) 
            return;
        if (entry.hitEffectPrefab == null) 
            return;

        GameObject fx = Instantiate(entry.hitEffectPrefab, pos, Quaternion.identity);
        Destroy(fx, 1.0f);
    }
}
