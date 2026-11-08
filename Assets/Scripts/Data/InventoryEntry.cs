using System;
using UnityEngine;
using BlackSmith.Items;

namespace BlackSmith.Data
{
    /// <summary>
    /// 소지 아이템 한 항목. 아이템 ID + 개별 상태(등급, 품질, 재료 합산 가격) 조합마다 하나씩 존재하고 수량을 가진다.
    /// ItemData 참조 대신 ID 문자열로 저장해 JSON 저장에 적합하다.
    /// quality, ingredientValue는 나중에 추가된 필드라 옛 저장 파일에는 없고, 그때는 기본값(None, 0)으로 읽힌다.
    /// </summary>
    [Serializable]
    public class InventoryEntry
    {
        [SerializeField] private string itemId;
        [SerializeField] private ItemGrade grade;
        [SerializeField] private ItemQuality quality;
        [SerializeField] private int ingredientValue;
        [SerializeField] private int count;

        public string ItemId => itemId;
        public ItemGrade Grade => grade;
        public ItemQuality Quality => quality;
        public int IngredientValue => ingredientValue;
        public int Count => count;

        public ItemVariant Variant => new ItemVariant(grade, quality, ingredientValue);

        public InventoryEntry(string itemId, ItemVariant variant, int count)
        {
            this.itemId = itemId;
            grade = variant.Grade;
            quality = variant.Quality;
            ingredientValue = variant.IngredientValue;
            this.count = count;
        }

        public InventoryEntry(string itemId, ItemGrade grade, int count)
            : this(itemId, new ItemVariant(grade), count)
        {
        }

        internal void SetCount(int value) => count = value;
    }
}
