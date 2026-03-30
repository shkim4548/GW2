using Data;
using System.Collections.Generic;
using UnityEngine;

public class EffectService : MonoBehaviour
{
    public static EffectService Instance { get; private set; }

    [SerializeField] private SkillEffectData _effectData;

    private Dictionary<int, SkillEffectEntry> _map = new();

    void Awake()
    {
        Instance = this;
        if (_effectData == null) return;
        foreach (var entry in _effectData.entries)
            _map[entry.skillId] = entry;
    }

    public void SpawnEffect(int skillId, Vector3 pos, Vector3 dir)
    {
        if (!_map.TryGetValue(skillId, out var entry)) return;
        if (entry.effectPrefab == null) return;

        Quaternion rot = dir.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(dir) : Quaternion.identity;

        GameObject fx = Instantiate(entry.effectPrefab, pos, rot);
        Destroy(fx, entry.duration);
    }

    public void SpawnHitEffect(int skillId, Vector3 pos)
    {
        if (!_map.TryGetValue(skillId, out var entry)) return;
        if (entry.hitEffectPrefab == null) return;

        GameObject fx = Instantiate(entry.hitEffectPrefab, pos, Quaternion.identity);
        Destroy(fx, 1.0f);
    }
}
