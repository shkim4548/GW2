// Editor/CardIconSOEditor.cs
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardIconSO))]
public class CardIconSOEditor : Editor
{
    private const string CARD_IMAGE_PATH = "Assets/Resources/Texture/Card/card_image";
    private const string CARD_STATS_PATH = "Json/CardStats";

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        if (GUILayout.Button("Auto Fill from CardStats + card_image", GUILayout.Height(30)))
            AutoFill();
    }

    private void AutoFill()
    {
        CardIconSO so = (CardIconSO)target;

        string jsonPath = "Assets/Resources/Json/CardStats.json";
        string jsonText = System.IO.File.ReadAllText(jsonPath);
        Data.CardInfoList data = JsonUtility.FromJson<Data.CardInfoList>(jsonText);


        // 교체 후
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { CARD_IMAGE_PATH });
        var sprites = new System.Collections.Generic.List<Sprite>();
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null)
                sprites.Add(s);
            else
                Debug.LogWarning($"Sprite 로드 실패 (Texture Type 확인 필요): {System.IO.Path.GetFileName(path)}");
        }
        Debug.Log($"발견된 스프라이트 수: {sprites.Count}");
        foreach (var s in sprites) Debug.Log($"  발견: '{s.name}'");


        so.entries.Clear();
        int matched = 0;

        foreach (var card in data.cards)
        {
            Sprite sprite = sprites.FirstOrDefault(s => {
                string path = AssetDatabase.GetAssetPath(s);
                string filename = System.IO.Path.GetFileNameWithoutExtension(path);
                return filename == card.name;
            });
            so.entries.Add(new CardIconSO.Entry { cardId = card.id, icon = sprite });

            if (sprite != null)
            {
                Debug.Log($"[✅] id={card.id} '{card.name}' 매칭 성공");
                matched++;
            }
            else
            {
                Debug.LogWarning($"[⚠️] id={card.id} 검색명='{card.name}'(len:{card.name.Length}) — sprite.name 예시: '{sprites.FirstOrDefault()?.name}'");
            }
        }

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        Debug.Log($"완료: {matched}/{data.cards.Count} 매칭");
    }
}
#endif
