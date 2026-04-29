using Data;
using System.Collections;
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
    public void SpawnEffect(int skillId, Vector3 attackerPos, Vector3 worldPos, Vector3 dir, Transform attackerTransform = null)
    {
        if (!_map.TryGetValue(skillId, out var entry)) 
            return;
        if (entry.effectPrefab == null) 
            return;

        Vector3 spawnPos = entry.spawnType switch
        {
            EffectSpawnType.ClickPoint => worldPos,
            _ => attackerPos,
        };

        // ClickPoint(장판)는 방향 회전 없이 수평 유지
        Quaternion rot;
        if (entry.spawnType == EffectSpawnType.ClickPoint)
        {
            rot = Quaternion.identity;
        }
        else
        {
            Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
            rot = flatDir.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(flatDir)
                : Quaternion.identity;
        }

        GameObject fx = Instantiate(entry.effectPrefab, spawnPos, rot);

        if (entry.spawnType == EffectSpawnType.Self && attackerTransform != null)
        {
            // Self: 플레이어 자식으로 → 위치 고정 추적
            fx.transform.SetParent(attackerTransform, worldPositionStays: true);
        }
        else if (entry.spawnType == EffectSpawnType.Projectile)
        {
            StartCoroutine(MoveProjectile(fx, worldPos, entry.duration));
            return; // MoveProjectile 내부에서 Destroy 처리
        }

        Destroy(fx, entry.duration);
    }

    private IEnumerator MoveProjectile(GameObject fx, Vector3 dest, float duration)
    {
        if (fx == null) yield break;
        Vector3 start = fx.transform.position;
        float elapsed = 0f;
        while (elapsed < duration && fx != null)
        {
            elapsed += Time.deltaTime;
            fx.transform.position = Vector3.Lerp(start, dest, elapsed / duration);
            yield return null;
        }
        if (fx != null) Destroy(fx);
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
