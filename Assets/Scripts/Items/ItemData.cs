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
        [Tooltip("이 아이템의 기본 등급. 광물은 상점에서 이 등급으로 팔리고, 제작품은 레시피에 정해진 이 등급 하나뿐이다.")]
        [SerializeField] private ItemGrade defaultGrade = ItemGrade.Common;

        [Header("판매")]
        [Tooltip("Common 등급 기준 판매가. 다른 등급은 등급 배율(ItemGradeInfo)을 곱해 자동 계산한다. 제작품은 쓰지 않는다.")]
        [SerializeField, Min(0)] private int sellPrice;

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

        /// <summary>기본 등급 기준 구매가. 구매할 수 없는 아이템은 0.</summary>
        public int BuyPrice => GetBuyPrice(defaultGrade);

        /// <summary>구매가. Common 기준 구매가에 등급 배율을 곱한다 (반올림). 구매할 수 없는 아이템은 0.</summary>
        public virtual int GetBuyPrice(ItemGrade grade) => 0;

        /// <summary>Common 기준 판매가에 등급 배율을 곱한 판매가 (반올림). 광물에 쓴다.</summary>
        public int GetSellPrice(ItemGrade grade) => ItemGradeInfo.ScalePrice(sellPrice, grade);

        /// <summary>기본 등급 기준 판매가.</summary>
        public int GetSellPrice() => GetSellPrice(defaultGrade);

        /// <summary>보유한 항목 하나(개별 상태 포함)의 판매 단가. 제작품은 재료 합산 가격 x 품질 배율로 덮어쓴다.</summary>
        public virtual int GetSellUnitPrice(ItemVariant variant) => GetSellPrice(variant.Grade);

        protected virtual void OnValidate()
        {
            if (maxStack < 1) maxStack = 1;
            if (sellPrice < 0) sellPrice = 0;
        }
    }
}
