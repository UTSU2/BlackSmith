using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Crafting;
using BlackSmith.Items;
using BlackSmith.Proficiency;
using BlackSmith.Rank;
using BlackSmith.Data;

namespace BlackSmith.UI
{
    /// <summary>
    /// 제작 결과 팝업. 제작이 끝나면 완성품·품질·숙련도 변화를 보여 주고 확인 버튼으로 닫는다. (docs/V2.md §2)
    /// 처음 만든 레시피면 도감 해금 줄을, 알 수 없는 제작품이면 숙련도 대신 판매가를 보여 준다. (docs/V2.md §3)
    /// 숙련도가 쌓이지 않았거나 이미 Master면 현재 단계·레벨만 보여 준다. 배치·디자인은 임시값이다.
    /// </summary>
    public class CraftResultPopup : MonoBehaviour
    {
        [SerializeField] private CraftingController crafting;

        [Header("표시")]
        [Tooltip("완성품 아이콘. 아이콘이 없는 아이템이면 숨긴다.")]
        [SerializeField] private Image iconImage;
        [Tooltip("완성품 이름. 긴 이름도 한 줄에 들어가도록 등급·품질은 detailLabel에 따로 둔다.")]
        [SerializeField] private Text itemLabel;
        [Tooltip("등급·품질. 비워 두면 아이템명 줄에 함께 표시한다.")]
        [SerializeField] private Text detailLabel;
        [Tooltip("레시피를 처음 만들어 도감에 해금했을 때만 보인다.")]
        [SerializeField] private Text unlockLabel;
        [SerializeField] private Text proficiencyLabel;
        [Tooltip("진행도 게이지(막대와 숫자)를 묶은 오브젝트. 변화가 없거나 Master면 숨긴다.")]
        [SerializeField] private GameObject progressRoot;
        [Tooltip("Filled 타입 Image. fillAmount로 진행도를 보여 준다.")]
        [SerializeField] private Image progressFill;
        [SerializeField] private Text progressLabel;
        [SerializeField] private Text bonusLabel;
        [Tooltip("알 수 없는 제작품일 때만 판매가를 보여 준다(숙련도 줄 대신).")]
        [SerializeField] private Text priceLabel;

        [Header("닫기")]
        [SerializeField] private Button confirmButton;

        /// <summary>확인 버튼으로 닫혔을 때.</summary>
        public event Action Closed;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            // 글꼴은 크기 기준(UIFontSettings)으로 시작할 때 적용한다.
            UIFonts.ApplyAll(this);
        }

        // 팝업은 닫혀 있는(비활성) 동안에도 제작 완료를 들어야 하므로 OnEnable이 아니라 Awake/OnDestroy에서 구독한다.
        // 씬에서 비활성으로 시작하면 Awake가 불리지 않으므로, 결과 팝업 오브젝트는 활성으로 두고 Start에서 숨긴다.
        private void Start()
        {
            if (crafting != null) crafting.CraftFinished += OnCraftFinished;
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (crafting != null) crafting.CraftFinished -= OnCraftFinished;
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
        }

        private void OnCraftFinished(CraftResult result)
        {
            Show(result);
        }

        public void Show(CraftResult result)
        {
            if (result == null || result.Product == null) return;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (iconImage != null)
            {
                iconImage.sprite = result.Product.Icon;
                iconImage.enabled = result.Product.Icon != null;
            }

            string detail = $"({result.Grade}) {ItemQualityInfo.DisplayName(result.Quality)}";
            if (detailLabel != null) detailLabel.text = detail;
            if (itemLabel != null)
            {
                itemLabel.text = detailLabel != null ? result.Product.DisplayName : $"{result.Product.DisplayName} {detail}";
            }

            if (unlockLabel != null)
            {
                bool unlocked = result.NewlyUnlocked && result.Recipe != null;
                unlockLabel.gameObject.SetActive(unlocked);
                if (unlocked) unlockLabel.text = $"새 레시피 해금! (No.{result.Recipe.CodexNumber})";
            }

            ProficiencyState after = result.ProficiencyAfter;
            bool changed = after != null && result.ProficiencyGain > 0 && !after.IsMaster;
            // Master에 막 도달한 경우도 "Master"만 보여 준다(강조 없음).
            if (proficiencyLabel != null)
            {
                proficiencyLabel.gameObject.SetActive(after != null);
                if (after != null)
                {
                    proficiencyLabel.text = changed ? $"{after.Label} (+{result.ProficiencyGain})" : after.Label;
                }
            }

            if (progressRoot != null) progressRoot.SetActive(changed);
            if (changed)
            {
                if (progressFill != null) progressFill.fillAmount = after.Required > 0 ? (float)after.Progress / after.Required : 0f;
                if (progressLabel != null) progressLabel.text = $"{after.Progress} / {after.Required}";
            }

            // 품질 보너스 줄에 스킵 장비로 건너뛴 단계를 덧붙인다(줄을 새로 늘리면 상자 배치가 바뀌므로 기존 줄을 쓴다).
            // 알 수 없는 제작품(숙련도 없음)이라도 스킵을 썼으면 이 줄에 스킵만 보여 준다.
            if (bonusLabel != null)
            {
                string skipText = DescribeSkips(result);
                bool hasBonus = after != null;
                bonusLabel.gameObject.SetActive(hasBonus || skipText.Length > 0);
                string bonusText = hasBonus ? "품질 보너스 +" + result.Bonus.ToString("0.#", CultureInfo.InvariantCulture) : string.Empty;
                bonusLabel.text = hasBonus && skipText.Length > 0 ? bonusText + " · " + skipText : bonusText + skipText;
            }

            // 알 수 없는 제작품은 숙련도가 없으므로(ProficiencyAfter == null) 위 줄들이 숨고 판매가를 보여 준다.
            if (priceLabel != null)
            {
                priceLabel.gameObject.SetActive(result.IsUnknown);
                if (result.IsUnknown)
                {
                    int price = result.Product.GetSellUnitPrice(new ItemVariant(result.Grade, result.Quality, result.Request.IngredientValue));
                    BlacksmithRankSettings rankSettings = BlacksmithRankSettings.Load();
                    if (rankSettings != null) price = rankSettings.ApplySellMultiplier(price, GameSession.Current.Rank);
                    priceLabel.text = $"판매가 {price} G";
                }
            }
        }

        // 스킵 장비로 건너뛴 단계 이름. 없으면 빈 문자열. 예: "스킵 가열·열처리"
        private static string DescribeSkips(CraftResult result)
        {
            string names = string.Empty;
            foreach (CraftStageResult stage in result.Stages)
            {
                if (stage == null || !stage.IsFixed) continue;
                string name = stage.Stage == CraftStage.Heat ? "가열" : (stage.Stage == CraftStage.HeatTreat ? "열처리" : stage.Stage.ToString());
                names = names.Length > 0 ? names + "·" + name : name;
            }
            return names.Length > 0 ? "스킵 " + names : string.Empty;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnConfirmClicked()
        {
            Hide();
            Closed?.Invoke();
        }
    }
}
