using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Crafting;
using BlackSmith.Items;
using BlackSmith.Proficiency;

namespace BlackSmith.UI
{
    /// <summary>
    /// 제작 결과 팝업. 제작이 끝나면 완성품·품질·숙련도 변화를 보여 주고 확인 버튼으로 닫는다. (docs/V2.md §2)
    /// 숙련도가 쌓이지 않았거나 이미 Master면 현재 단계·레벨만 보여 준다. 배치·디자인은 임시값이다.
    /// </summary>
    public class CraftResultPopup : MonoBehaviour
    {
        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        [SerializeField] private CraftingController crafting;

        [Header("표시")]
        [Tooltip("완성품 아이콘. 아이콘이 없는 아이템이면 숨긴다.")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Text itemLabel;
        [SerializeField] private Text proficiencyLabel;
        [Tooltip("진행도 게이지(막대와 숫자)를 묶은 오브젝트. 변화가 없거나 Master면 숨긴다.")]
        [SerializeField] private GameObject progressRoot;
        [Tooltip("Filled 타입 Image. fillAmount로 진행도를 보여 준다.")]
        [SerializeField] private Image progressFill;
        [SerializeField] private Text progressLabel;
        [SerializeField] private Text bonusLabel;

        [Header("닫기")]
        [SerializeField] private Button confirmButton;

        /// <summary>확인 버튼으로 닫혔을 때.</summary>
        public event Action Closed;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            // OS 폰트는 씬에 저장되지 않으므로 시작할 때 글자에 적용한다.
            Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;
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

            if (itemLabel != null)
            {
                itemLabel.text = $"{result.Product.DisplayName} ({result.Grade}) {ItemQualityInfo.DisplayName(result.Quality)}";
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

            if (bonusLabel != null)
            {
                bonusLabel.gameObject.SetActive(after != null);
                bonusLabel.text = "품질 보너스 +" + result.Bonus.ToString("0.#", CultureInfo.InvariantCulture);
            }
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
