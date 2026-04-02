using UnityEngine;

public enum EffectSpawnType
{
    Self,        // 자기 위치 (버프, 힐)
    Projectile,  // 자기 위치 → 방향으로
    ClickPoint   // 클릭 지점 (Cannon, Grenade)
}

[System.Serializable]
public class SkillEffectEntry
{
    public int skillId;
    public GameObject effectPrefab;
    public GameObject hitEffectPrefab;
    public float duration = 2.0f;
    public EffectSpawnType spawnType = EffectSpawnType.Projectile;  // ← 추가
}

[CreateAssetMenu(fileName = "SkillEffectData", menuName = "GW2/SkillEffectData")]
public class SkillEffectData : ScriptableObject
{
    public SkillEffectEntry[] entries;
}
