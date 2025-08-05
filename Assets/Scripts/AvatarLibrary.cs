using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AvatarEntry        // one row in the list
{
    public string id;            // e.g. "cat_03"
    public Sprite sprite;        // the artwork
}


[CreateAssetMenu(menuName = "Poker/Avatar Library")]
public class AvatarLibrary : ScriptableObject
{
    [SerializeField] private List<AvatarEntry> entries = new();

    // Built on first access, never serialized
    private Dictionary<string, Sprite> lookup;

    public Sprite GetSprite(string id)
    {
        // Lazily build the map once
        if (lookup == null)
        {
            lookup = new Dictionary<string, Sprite>(entries.Count);
            foreach (var e in entries)
                lookup[e.id] = e.sprite;
        }
        return lookup.TryGetValue(id, out var s) ? s : null;
    }
}
