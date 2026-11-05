using System;
using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Items;

namespace BlackSmith.UI
{
    /// <summary>
    /// 상점 목록의 한 줄. 아이콘, 이름, 가격을 보여주고 누르면 Clicked를 알린다.
    /// 구매 목록은 Bind(item), 판매 목록은 Bind(item, grade, count)로 채운다.
    /// </summary>
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text priceLabel;

        private ItemData item;
        private ItemGrade grade;

        public event Action<ItemData, ItemGrade> Clicked;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
        }

        /// <summary>구매 목록용: 이름과 구매가.</summary>
        public void Bind(ItemData data)
        {
            item = data;
            grade = data.DefaultGrade;
            if (nameLabel != null) nameLabel.text = data.DisplayName;
            if (priceLabel != null) priceLabel.text = $"{data.BuyPrice:N0} G";
            SetIcon(data);
        }

        /// <summary>판매 목록용: 이름(등급, 보유 수량)과 판매 단가.</summary>
        public void Bind(ItemData data, ItemGrade itemGrade, int count)
        {
            item = data;
            grade = itemGrade;
            if (nameLabel != null) nameLabel.text = $"{data.DisplayName} ({itemGrade}) x{count}";
            if (priceLabel != null) priceLabel.text = $"{data.GetSellPrice(itemGrade):N0} G";
            SetIcon(data);
        }

        private void SetIcon(ItemData data)
        {
            if (iconImage == null) return;
            iconImage.sprite = data.Icon;
            iconImage.enabled = data.Icon != null;
        }

        private void OnClick()
        {
            if (item != null) Clicked?.Invoke(item, grade);
        }
    }
}
