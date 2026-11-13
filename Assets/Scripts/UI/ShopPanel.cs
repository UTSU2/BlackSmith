using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Data;
using BlackSmith.Items;
using BlackSmith.Shop;

namespace BlackSmith.UI
{
    /// <summary>
    /// 상점 패널. 구매 탭과 판매 탭을 오가며 목록을 보여주고, 보유 금액을 표시하며,
    /// 아이템을 누르면 구매/판매 팝업을 연다.
    /// 구매/판매 규칙은 ShopService가 맡고 이 클래스는 표시만 담당한다.
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        public enum Mode { Buy, Sell }

        [SerializeField] private ShopItemView itemViewPrefab;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private Text moneyLabel;
        [SerializeField] private ShopBuyPopup buyPopup;
        [SerializeField] private Button closeButton;
        [Tooltip("보이고 숨기는 연출. 비워 두면 즉시 켜고 끈다.")]
        [SerializeField] private SaveLoadPanelTransition transition;

        [Header("탭")]
        [SerializeField] private Button buyTabButton;
        [SerializeField] private Button sellTabButton;
        [Tooltip("탭 버튼에 쓰는 이미지. 선택된 탭은 눌린 스프라이트로 보여준다.")]
        [SerializeField] private Image buyTabImage;
        [SerializeField] private Image sellTabImage;
        [SerializeField] private Sprite tabNormalSprite;
        [SerializeField] private Sprite tabSelectedSprite;
        [Tooltip("목록이 비었을 때 보여줄 안내 글자. 비워 두면 표시하지 않는다.")]
        [SerializeField] private Text emptyLabel;

        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        private readonly List<ShopItemView> views = new List<ShopItemView>();
        private ShopService service;
        private PlayerData subscribedPlayer;
        private Mode mode = Mode.Buy;

        public Mode CurrentMode => mode;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (buyTabButton != null) buyTabButton.onClick.AddListener(OnBuyTab);
            if (sellTabButton != null) sellTabButton.onClick.AddListener(OnSellTab);
            if (buyPopup != null) buyPopup.gameObject.SetActive(false);
            ApplyFont();
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (buyTabButton != null) buyTabButton.onClick.RemoveListener(OnBuyTab);
            if (sellTabButton != null) sellTabButton.onClick.RemoveListener(OnSellTab);
            Unsubscribe();
        }

        public void Open()
        {
            if (transition != null && transition.IsPlaying) return;

            Unsubscribe();
            PlayerData player = GameSession.Current;
            service = new ShopService(player);
            subscribedPlayer = player;
            subscribedPlayer.OnMoneyChanged += OnMoneyChanged;
            subscribedPlayer.OnInventoryChanged += OnInventoryChanged;

            // 목록을 채우기 전에 패널이 먼저 활성화되어 있어야 한다. (transition.Show도 즉시 활성화함)
            if (transition != null) transition.Show();
            else gameObject.SetActive(true);
            if (buyPopup != null) buyPopup.gameObject.SetActive(false);
            mode = Mode.Buy;
            Rebuild();
            OnMoneyChanged(player.Money);
        }

        public void Close()
        {
            if (buyPopup != null) buyPopup.Close();
            Unsubscribe();
            if (transition != null) transition.Hide();
            else gameObject.SetActive(false);
        }

        public void SetMode(Mode newMode)
        {
            if (service == null) return;
            if (buyPopup != null) buyPopup.Close();
            mode = newMode;
            Rebuild();
        }

        private void OnBuyTab() => SetMode(Mode.Buy);

        private void OnSellTab() => SetMode(Mode.Sell);

        private void Rebuild()
        {
            BuildList();
            RefreshTabs();
        }

        private void BuildList()
        {
            foreach (ShopItemView view in views)
            {
                if (view == null) continue;
                view.Clicked -= OnItemClicked;
                // Destroy는 프레임 끝에 처리되므로, 그 전에 새 목록과 섞여 보이거나 눌리지 않도록 먼저 끈다.
                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }
            views.Clear();

            bool hasItems = false;
            if (itemViewPrefab != null && listContent != null)
            {
                if (mode == Mode.Buy)
                {
                    foreach (ItemData item in service.GetStock())
                    {
                        ShopItemView view = CreateView();
                        view.Bind(item);
                        hasItems = true;
                    }
                }
                else
                {
                    foreach (ShopService.SellEntry entry in service.GetSellList())
                    {
                        ShopItemView view = CreateView();
                        view.Bind(entry.Item, entry.Variant, entry.Count);
                        hasItems = true;
                    }
                }
            }

            if (emptyLabel != null)
            {
                emptyLabel.gameObject.SetActive(!hasItems);
                emptyLabel.text = mode == Mode.Buy ? "판매 중인 아이템이 없습니다" : "판매할 아이템이 없습니다";
            }
        }

        private ShopItemView CreateView()
        {
            ShopItemView view = Instantiate(itemViewPrefab, listContent);
            view.Clicked += OnItemClicked;
            views.Add(view);
            return view;
        }

        private void RefreshTabs()
        {
            SetTabSprite(buyTabImage, mode == Mode.Buy);
            SetTabSprite(sellTabImage, mode == Mode.Sell);
        }

        private void SetTabSprite(Image image, bool selected)
        {
            if (image == null) return;
            Sprite sprite = selected ? tabSelectedSprite : tabNormalSprite;
            if (sprite != null) image.sprite = sprite;
        }

        private void OnItemClicked(ItemData item, ItemVariant variant)
        {
            if (buyPopup == null) return;
            if (mode == Mode.Buy) buyPopup.OpenBuy(service, item);
            else buyPopup.OpenSell(service, item, variant);
        }

        private void OnMoneyChanged(int money)
        {
            if (moneyLabel != null) moneyLabel.text = $"{money:N0} G";
            if (buyPopup != null && buyPopup.gameObject.activeSelf) buyPopup.Refresh();
        }

        private void OnInventoryChanged()
        {
            // 판매 탭에서는 판매 후 보유 수량이 바뀌므로 목록을 다시 만든다.
            // 구매 탭의 목록은 보유 수량과 무관하다.
            if (mode == Mode.Sell && service != null) BuildList();
            if (buyPopup != null && buyPopup.gameObject.activeSelf) buyPopup.Refresh();
        }

        private void Unsubscribe()
        {
            if (subscribedPlayer != null)
            {
                subscribedPlayer.OnMoneyChanged -= OnMoneyChanged;
                subscribedPlayer.OnInventoryChanged -= OnInventoryChanged;
            }
            subscribedPlayer = null;
        }

        private void ApplyFont()
        {
            // OS 폰트는 씬에 저장되지 않으므로 시작할 때 글자에 적용한다. (SaveLoadPanel과 동일한 방식)
            Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                text.font = font;
            }
            if (itemViewPrefab != null)
            {
                foreach (Text text in itemViewPrefab.GetComponentsInChildren<Text>(true))
                {
                    text.font = font;
                }
            }
        }
    }
}
