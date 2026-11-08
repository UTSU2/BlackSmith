using System;
using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Items;
using BlackSmith.Shop;

namespace BlackSmith.UI
{
    /// <summary>
    /// 구매/판매 겸용 팝업. 선택한 아이템과 수량(화살표로 조절), 총액을 보여준다.
    /// 구매 모드는 금액이, 판매 모드는 보유 수량이 부족하면 버튼을 눌러도 아무 반응이 없다.
    /// (interactable은 쓰지 않고 색만 어둡게 한다)
    /// </summary>
    public class ShopBuyPopup : MonoBehaviour
    {
        private enum PopupMode { Buy, Sell }

        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text quantityLabel;
        [SerializeField] private Text totalLabel;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private Image buyButtonImage;
        [SerializeField] private Button closeButton;
        [Tooltip("팝업 제목. 구매/판매에 따라 글자가 바뀐다. 비워 두면 바꾸지 않는다.")]
        [SerializeField] private Text headerLabel;
        [Tooltip("확정 버튼의 글자. 구매/판매에 따라 바뀐다. 비워 두면 바꾸지 않는다.")]
        [SerializeField] private Text actionLabel;

        private static readonly Color DisabledTint = new Color(0.45f, 0.45f, 0.45f, 1f);

        private ShopService service;
        private ItemData item;
        private ItemVariant variant;
        private PopupMode mode = PopupMode.Buy;
        private int quantity = ShopService.MinPurchaseQuantity;

        /// <summary>구매 또는 판매에 성공했을 때 호출.</summary>
        public event Action Completed;

        private void Awake()
        {
            if (decreaseButton != null) decreaseButton.onClick.AddListener(OnDecrease);
            if (increaseButton != null) increaseButton.onClick.AddListener(OnIncrease);
            if (buyButton != null) buyButton.onClick.AddListener(OnConfirm);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (decreaseButton != null) decreaseButton.onClick.RemoveListener(OnDecrease);
            if (increaseButton != null) increaseButton.onClick.RemoveListener(OnIncrease);
            if (buyButton != null) buyButton.onClick.RemoveListener(OnConfirm);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        public void OpenBuy(ShopService shopService, ItemData target)
        {
            Open(PopupMode.Buy, shopService, target, new ItemVariant(target.DefaultGrade));
        }

        public void OpenSell(ShopService shopService, ItemData target, ItemVariant targetVariant)
        {
            Open(PopupMode.Sell, shopService, target, targetVariant);
        }

        private void Open(PopupMode newMode, ShopService shopService, ItemData target, ItemVariant targetVariant)
        {
            mode = newMode;
            service = shopService;
            item = target;
            variant = targetVariant;
            quantity = ShopService.MinPurchaseQuantity;

            if (headerLabel != null) headerLabel.text = mode == PopupMode.Buy ? "구매" : "판매";
            if (actionLabel != null) actionLabel.text = mode == PopupMode.Buy ? "구매" : "판매";
            if (nameLabel != null) nameLabel.text = item.DisplayName;
            if (iconImage != null)
            {
                iconImage.sprite = item.Icon;
                iconImage.enabled = item.Icon != null;
            }

            gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            item = null;
            gameObject.SetActive(false);
        }

        /// <summary>보유 금액/수량이 바뀌었을 때 등 외부에서 표시를 다시 맞춘다.</summary>
        public void Refresh()
        {
            if (item == null || service == null) return;

            quantity = ShopService.ClampQuantity(quantity, GetMaxQuantity());
            if (quantityLabel != null) quantityLabel.text = quantity.ToString();
            if (totalLabel != null) totalLabel.text = $"{GetTotalPrice():N0} G";
            if (buyButtonImage != null)
            {
                buyButtonImage.color = CanConfirm() ? Color.white : DisabledTint;
            }
        }

        private int GetMaxQuantity()
        {
            return mode == PopupMode.Buy
                ? ShopService.MaxPurchaseQuantity
                : service.GetOwnedCount(item, variant);
        }

        private int GetTotalPrice()
        {
            return mode == PopupMode.Buy
                ? service.GetTotalPrice(item, quantity)
                : service.GetSellTotalPrice(item, variant, quantity);
        }

        private bool CanConfirm()
        {
            return mode == PopupMode.Buy
                ? service.CanBuy(item, quantity)
                : service.CanSell(item, variant, quantity);
        }

        private void OnDecrease() => SetQuantity(quantity - 1);

        private void OnIncrease() => SetQuantity(quantity + 1);

        private void SetQuantity(int value)
        {
            if (item == null || service == null) return;
            quantity = ShopService.ClampQuantity(value, GetMaxQuantity());
            Refresh();
        }

        private void OnConfirm()
        {
            // 조건이 맞지 않으면 TryBuy/TrySell이 false를 돌려주므로 아무 일도 일어나지 않는다.
            if (item == null || service == null) return;

            bool done = mode == PopupMode.Buy
                ? service.TryBuy(item, quantity)
                : service.TrySell(item, variant, quantity);
            if (!done) return;

            // 구매/판매가 이루어졌으면 팝업을 닫는다. (실패하면 위에서 이미 반환해 열려 있다)
            Close();
            Completed?.Invoke();
        }
    }
}
