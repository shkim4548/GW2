#if UNITY_EDITOR
using Data;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillEffectData))]
public class SkillEffectDataEditor : Editor
{
    private const string EFFECT_PATH = "Assets/Resources/Prefabs/Particle";
    private const string CARD_STATS_PATH = "Assets/Resources/Json/Stats.json";

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        if (GUILayout.Button("Auto Fill from CardStats + Particle", GUILayout.Height(30)))
            AutoFill();
    }

    private void AutoFill()
    {
        SkillEffectData so = (SkillEffectData)target;

        // 1. CardStats.json 파싱
        string jsonText = System.IO.File.ReadAllText(CARD_STATS_PATH);
        Data.CardInfoList data = JsonUtility.FromJson<Data.CardInfoList>(jsonText);

        // 2. Particle 폴더 프리팹 전부 로드
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { EFFECT_PATH });
        var prefabs = new List<GameObject>();
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) prefabs.Add(go);
        }
        Debug.Log($"[SkillEffect] 발견된 프리팹 수: {prefabs.Count}");

        // 3. 카드별 매칭 (프리팹 이름에 card.name 포함 여부)
        var result = new List<SkillEffectEntry>();
        int matched = 0;

        foreach (var card in data.cards)
        {
            GameObject prefab = prefabs.FirstOrDefault(p =>
                p.name.IndexOf(card.name, System.StringComparison.OrdinalIgnoreCase) >= 0);

            result.Add(new SkillEffectEntry
            {
                skillId = card.id,
                effectPrefab = prefab,
                duration = 2.0f
            });

            if (prefab != null)
            {
                Debug.Log($"[?] id={card.id} '{card.name}' → '{prefab.name}'");
                matched++;
            }
            else
                Debug.LogWarning($"[??] id={card.id} '{card.name}' ? 매칭 프리팹 없음");
        }

        so.entries = result.ToArray();
        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SkillEffect] 완료: {matched}/{data.cards.Count} 매칭");
    }
}
#endif
