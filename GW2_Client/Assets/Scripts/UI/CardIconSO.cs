using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardIconSO", menuName = "Data/CardIconSO")]
public class CardIconSO : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public int cardId;
        public Sprite icon;
    }

    public List<Entry> entries;

    private Dictionary<int, Sprite> _dict;

    public void Init()
    {
        _dict = new Dictionary<int, Sprite>();
        foreach (var e in entries)
            _dict[e.cardId] = e.icon;
    }

    public Sprite Get(int cardId)
    {
        _dict.TryGetValue(cardId, out Sprite s);
        return s;
    }
}
