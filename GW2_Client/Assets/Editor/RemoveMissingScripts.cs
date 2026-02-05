#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class RemoveMissingScripts : EditorWindow
{
    [MenuItem("Tools/Remove Missing Scripts")]
    static void RemoveMissing()
    {
        GameObject[] gameObjects = FindObjectsOfType<GameObject>();
        int count = 0;
        foreach (GameObject go in gameObjects)
        {
            // 이 기능이 핵심: null 컴포넌트를 한 번에 삭제 [1]
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0) count += removed;
        }
        Debug.Log($"Removed {count} missing scripts.");
    }
}
#endif
