using System;
using UnityEngine;

/// <summary>
/// 소지 아이템 한 항목. 아이템 ID + 등급 조합마다 하나씩 존재하고 수량을 가진다.
/// ItemData 참조 대신 ID 문자열로 저장해 JSON 저장에 적합하다.
/// </summary>
[Serializable]
public class InventoryEntry
{
    [SerializeField] private string itemId;
    [SerializeField] private ItemGrade grade;
    [SerializeField] private int count;

    public string ItemId => itemId;
    public ItemGrade Grade => grade;
    public int Count => count;

    public InventoryEntry(string itemId, ItemGrade grade, int count)
    {
        this.itemId = itemId;
        this.grade = grade;
        this.count = count;
    }

    internal void SetCount(int value) => count = value;
}
