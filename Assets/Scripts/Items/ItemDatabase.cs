using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 아이템 데이터베이스.
/// Assets/Resources/Items/ 아래(하위 폴더 포함)의 모든 ItemData 에셋을 게임 시작 시 자동으로 읽어 등록한다.
/// 별도 오브젝트나 목록 관리는 필요 없다.
/// </summary>
public static class ItemDatabase
{
    private const string ResourcesPath = "Items";

    private static readonly Dictionary<string, ItemData> byId = new Dictionary<string, ItemData>();
    private static readonly List<ItemData> all = new List<ItemData>();
    private static bool loaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Reload();
    }

    /// <summary>등록된 모든 아이템.</summary>
    public static IReadOnlyList<ItemData> All
    {
        get { EnsureLoaded(); return all; }
    }

    public static int Count
    {
        get { EnsureLoaded(); return all.Count; }
    }

    /// <summary>Resources에서 아이템을 다시 읽는다. (게임 시작 때 자동 호출됨)</summary>
    public static void Reload()
    {
        byId.Clear();
        all.Clear();

        ItemData[] items = Resources.LoadAll<ItemData>(ResourcesPath);
        foreach (ItemData item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                Debug.LogWarning($"[ItemDatabase] ID가 비어 있어 등록하지 않았습니다: {item.name}", item);
                continue;
            }

            if (byId.TryGetValue(item.Id, out ItemData existing))
            {
                Debug.LogError($"[ItemDatabase] 중복 ID '{item.Id}': '{existing.name}'이(가) 이미 등록되어 있어 '{item.name}'은(는) 무시했습니다.", item);
                continue;
            }

            byId.Add(item.Id, item);
            all.Add(item);
        }

        loaded = true;
    }

    /// <summary>ID로 아이템을 찾는다. 없으면 경고를 남기고 null.</summary>
    public static ItemData Get(string id)
    {
        if (TryGet(id, out ItemData item)) return item;
        Debug.LogWarning($"[ItemDatabase] 존재하지 않는 아이템 ID: '{id}'");
        return null;
    }

    public static bool TryGet(string id, out ItemData item)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(id))
        {
            item = null;
            return false;
        }
        return byId.TryGetValue(id, out item);
    }

    /// <summary>종류별 아이템 목록.</summary>
    public static List<ItemData> GetByType(ItemType type)
    {
        EnsureLoaded();
        List<ItemData> result = new List<ItemData>();
        foreach (ItemData item in all)
        {
            if (item.Type == type) result.Add(item);
        }
        return result;
    }

    private static void EnsureLoaded()
    {
        if (!loaded) Reload();
    }
}
