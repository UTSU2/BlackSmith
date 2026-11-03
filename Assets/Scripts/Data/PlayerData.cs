using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 데이터: 보유 금액과 소지 아이템.
/// 순수 C# 직렬화 클래스라 JsonUtility로 그대로 저장할 수 있다. (저장/불러오기는 아직 없음)
/// 소지 아이템은 (아이템 ID, 등급)별 수량 목록이며 칸 수 제한과 maxStack은 적용하지 않는다.
/// </summary>
[Serializable]
public class PlayerData
{
    /// <summary>새 게임 시작 금액. 아직 미정이라 임시로 0.</summary>
    public const int DefaultStartMoney = 0;

    [SerializeField] private int money = DefaultStartMoney;
    [SerializeField] private List<InventoryEntry> items = new List<InventoryEntry>();

    /// <summary>금액이 바뀔 때 호출 (새 금액).</summary>
    [field: NonSerialized] public event Action<int> OnMoneyChanged;

    /// <summary>소지 아이템이 바뀔 때 호출.</summary>
    [field: NonSerialized] public event Action OnInventoryChanged;

    public int Money => money;
    public IReadOnlyList<InventoryEntry> Items => items;

    // ---- 금액 ----

    public bool CanAfford(int amount) => amount >= 0 && money >= amount;

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        money = (int)Math.Min((long)money + amount, int.MaxValue);
        OnMoneyChanged?.Invoke(money);
    }

    /// <summary>금액을 차감한다. 부족하면 아무것도 하지 않고 false.</summary>
    public bool TrySpendMoney(int amount)
    {
        if (amount < 0 || money < amount) return false;
        if (amount == 0) return true;
        money -= amount;
        OnMoneyChanged?.Invoke(money);
        return true;
    }

    // ---- 아이템 ----

    public int GetCount(string itemId, ItemGrade grade)
    {
        InventoryEntry entry = Find(itemId, grade);
        return entry != null ? entry.Count : 0;
    }

    public bool HasItem(string itemId, ItemGrade grade, int count = 1)
    {
        return count > 0 && GetCount(itemId, grade) >= count;
    }

    /// <summary>아이템을 추가한다. 존재하지 않는 ID거나 수량이 0 이하이면 false.</summary>
    public bool AddItem(string itemId, int count, ItemGrade grade)
    {
        if (count <= 0) return false;
        if (!ItemDatabase.TryGet(itemId, out _))
        {
            Debug.LogWarning($"[PlayerData] 존재하지 않는 아이템 ID라 추가하지 않았습니다: '{itemId}'");
            return false;
        }

        InventoryEntry entry = Find(itemId, grade);
        if (entry != null)
        {
            entry.SetCount((int)Math.Min((long)entry.Count + count, int.MaxValue));
        }
        else
        {
            items.Add(new InventoryEntry(itemId, grade, count));
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>아이템을 제거한다. 수량이 부족하면 아무것도 하지 않고 false. 0이 되면 항목을 삭제한다.</summary>
    public bool TryRemoveItem(string itemId, int count, ItemGrade grade)
    {
        if (count <= 0) return false;
        InventoryEntry entry = Find(itemId, grade);
        if (entry == null || entry.Count < count) return false;

        if (entry.Count == count) items.Remove(entry);
        else entry.SetCount(entry.Count - count);

        OnInventoryChanged?.Invoke();
        return true;
    }

    private InventoryEntry Find(string itemId, ItemGrade grade)
    {
        foreach (InventoryEntry entry in items)
        {
            if (entry.ItemId == itemId && entry.Grade == grade) return entry;
        }
        return null;
    }
}
