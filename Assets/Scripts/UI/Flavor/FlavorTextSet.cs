using System;
using System.Collections.Generic;
using UnityEngine;

// 執筆者向け: 空行2つでページ区切りです。実際の表示時に各ページへ分割されます。
[CreateAssetMenu(menuName = "UI/Flavor Text Set")]
public class FlavorTextSet : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;
        public string title;
        [TextArea(4, 12)] public string text;
    }

    public List<Entry> entries = new();

    public bool TryGet(string key, out string title, out string text)
    {
        string trimmedKey = key?.Trim();
        if (!string.IsNullOrEmpty(trimmedKey) && entries != null)
        {
            foreach (Entry entry in entries)
            {
                if (entry != null && string.Equals(entry.key?.Trim(), trimmedKey,
                    StringComparison.OrdinalIgnoreCase))
                {
                    title = entry.title ?? string.Empty;
                    text = entry.text ?? string.Empty;
                    return true;
                }
            }
        }

        title = null;
        text = null;
        return false;
    }
}
