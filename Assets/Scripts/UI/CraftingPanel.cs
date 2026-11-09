using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Crafting;
using BlackSmith.Data;
using BlackSmith.Items;

namespace BlackSmith.UI
{
    /// <summary>
    /// 모루 재료 투입 패널. 소지한 광물 목록에서 + / - 로 재료를 넣고, 조합이 맞으면 제작 시작 버튼이 켜진다.
    /// 레시피는 플레이어에게 보이지 않으므로 "제작 가능 / 조합이 맞지 않음"만 알려준다.
    /// 조합 판정과 수량 규칙은 CraftingSession이 맡고 이 클래스는 표시와 입력 전달만 한다.
    /// </summary>
    public class CraftingPanel : MonoBehaviour
    {
        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };
        private static readonly Color DimColor = new Color(1f, 1f, 1f, 0.5f);

        [SerializeField] private CraftingIngredientView ingredientViewPrefab;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private Text statusLabel;
        [Tooltip("소지한 광물이 없을 때 보여줄 안내 글자. 비워 두면 표시하지 않는다.")]
        [SerializeField] private Text emptyLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button closeButton;

        private readonly List<CraftingIngredientView> views = new List<CraftingIngredientView>();
        private CraftingSession session;

        /// <summary>제작 시작이 확정될 때 호출 (레시피, 난이도, 재료 합산 가격 등). 이후 진행은 CraftingController가 맡는다.</summary>
        public event Action<CraftRequest> StartRequested;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStart);
            if (clearButton != null) clearButton.onClick.AddListener(OnClear);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            ApplyFont();
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStart);
            if (clearButton != null) clearButton.onClick.RemoveListener(OnClear);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            DetachSession();
        }

        public void Open()
        {
            DetachSession();
            session = new CraftingSession(GameSession.Current);
            session.Changed += OnSessionChanged;

            gameObject.SetActive(true);
            BuildList();
            RefreshState();
        }

        public void Close()
        {
            DetachSession();
            gameObject.SetActive(false);
        }

        private void DetachSession()
        {
            if (session != null) session.Changed -= OnSessionChanged;
            session = null;
        }

        private void BuildList()
        {
            foreach (CraftingIngredientView view in views)
            {
                if (view == null) continue;
                view.AddClicked -= OnAddClicked;
                view.RemoveClicked -= OnRemoveClicked;
                // Destroy는 프레임 끝에 처리되므로, 그 전에 새 목록과 섞여 보이지 않도록 먼저 끈다.
                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }
            views.Clear();

            bool hasItems = false;
            if (ingredientViewPrefab != null && listContent != null)
            {
                foreach (InventoryEntry entry in GameSession.Current.Items)
                {
                    // 재료로 쓸 수 있는 건 광물뿐이다.
                    if (!ItemDatabase.TryGet(entry.ItemId, out ItemData data) || !(data is MineralItemData)) continue;

                    CraftingIngredientView view = Instantiate(ingredientViewPrefab, listContent);
                    view.Bind(data, entry.Grade, entry.Count, session.GetInserted(entry.ItemId, entry.Grade));
                    view.AddClicked += OnAddClicked;
                    view.RemoveClicked += OnRemoveClicked;
                    views.Add(view);
                    hasItems = true;
                }
            }

            if (emptyLabel != null)
            {
                emptyLabel.gameObject.SetActive(!hasItems);
                emptyLabel.text = "넣을 수 있는 재료가 없습니다";
            }
        }

        private void OnAddClicked(ItemData item, ItemGrade grade)
        {
            if (session != null) session.TryAdd(item.Id, grade);
        }

        private void OnRemoveClicked(ItemData item, ItemGrade grade)
        {
            if (session != null) session.TryRemove(item.Id, grade);
        }

        private void OnClear()
        {
            if (session != null) session.Clear();
        }

        private void OnStart()
        {
            if (session == null || !session.CanStart) return;
            if (!session.TryStart(out CraftRequest request)) return;

            Close();
            StartRequested?.Invoke(request);
        }

        private void OnSessionChanged()
        {
            RefreshState();
        }

        private void RefreshState()
        {
            if (session == null) return;

            foreach (CraftingIngredientView view in views)
            {
                if (view == null) continue;
                view.SetInserted(session.GetInserted(view.Item.Id, view.Grade));
            }

            if (statusLabel != null)
            {
                if (session.IsEmpty) statusLabel.text = "재료를 넣어주세요";
                else statusLabel.text = session.CanStart ? "제작할 수 있습니다" : "조합이 맞지 않습니다";
            }

            // interactable을 끄면 스프라이트 전환과 글자 위치가 어긋나므로 상점 팝업처럼 색만 어둡게 한다.
            if (startButton != null && startButton.image != null)
            {
                startButton.image.color = session.CanStart ? Color.white : DimColor;
            }
        }

        private void ApplyFont()
        {
            // OS 폰트는 씬에 저장되지 않으므로 시작할 때 글자에 적용한다. (ShopPanel과 동일한 방식)
            Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                text.font = font;
            }
            if (ingredientViewPrefab != null)
            {
                foreach (Text text in ingredientViewPrefab.GetComponentsInChildren<Text>(true))
                {
                    text.font = font;
                }
            }
        }
    }
}
