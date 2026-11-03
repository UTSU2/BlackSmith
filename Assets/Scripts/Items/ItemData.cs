using UnityEngine;

namespace BlackSmith.Items
{
    /// <summary>
    /// 모든 아이템의 공통 데이터. 아이템 하나가 ScriptableObject 에셋 하나다.
    /// 에셋은 Assets/Resources/Items/ 아래에 두면 ItemDatabase가 자동으로 읽는다.
    /// 직접 만들지 말고 Tools > BlackSmith > Item Creator 창을 사용한다.
    /// </summary>
    public abstract class ItemData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("고유 ID. 영소문자, 숫자, 밑줄만 사용 (예: iron_ore). 다른 아이템과 겹치면 안 된다.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField, TextArea(2, 5)] private string description;

        [Header("인벤토리")]
        [Tooltip("인벤토리 한 칸에 쌓을 수 있는 최대 수량.")]
        [SerializeField, Min(1)] private int maxStack = 1;
        [Tooltip("이 아이템의 기본 등급. 광물은 고정 등급, 제작품의 실제 등급은 나중에 제작 완성도로 결정된다.")]
        [SerializeField] private ItemGrade defaultGrade = ItemGrade.Common;

        [Header("판매")]
        [SerializeField, Min(0)] private int sellPrice;
        [Tooltip("등급별 판매가 배율. 순서는 ItemGrade enum 순서와 같다. 배율 값은 미정이라 임시로 모두 1.")]
        [SerializeField] private float[] sellPriceMultipliers;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public string Description => description;
        public int MaxStack => maxStack;
        public ItemGrade DefaultGrade => defaultGrade;
        public int SellPrice => sellPrice;

        public abstract ItemType Type { get; }

        /// <summary>상점에서 구매할 수 있는가. 광물만 true.</summary>
        public virtual bool CanBuy => false;

        /// <summary>상점에 판매할 수 있는가. 광물과 제작품 모두 true.</summary>
        public bool CanSell => true;

        /// <summary>구매가. 구매할 수 없는 아이템은 0.</summary>
        public virtual int BuyPrice => 0;

        /// <summary>등급에 따른 판매가 = 기본 판매가 x 등급 배율 (반올림).</summary>
        public int GetSellPrice(ItemGrade grade)
        {
            float multiplier = 1f;
            int index = (int)grade;
            if (sellPriceMultipliers != null && index >= 0 && index < sellPriceMultipliers.Length)
            {
                multiplier = sellPriceMultipliers[index];
            }
            return Mathf.RoundToInt(sellPrice * multiplier);
        }

        /// <summary>기본 등급 기준 판매가.</summary>
        public int GetSellPrice() => GetSellPrice(defaultGrade);

        protected virtual void OnValidate()
        {
            if (maxStack < 1) maxStack = 1;
            if (sellPrice < 0) sellPrice = 0;
            EnsureMultipliers();
        }

        private void EnsureMultipliers()
        {
            int count = ItemGradeInfo.Count;
            if (sellPriceMultipliers != null && sellPriceMultipliers.Length == count) return;

            float[] resized = new float[count];
            for (int i = 0; i < count; i++)
            {
                resized[i] = (sellPriceMultipliers != null && i < sellPriceMultipliers.Length) ? sellPriceMultipliers[i] : 1f;
            }
            sellPriceMultipliers = resized;
        }
    }
}
