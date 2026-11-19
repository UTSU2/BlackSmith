using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Crafting;
using BlackSmith.Data;
using BlackSmith.Items;
using BlackSmith.Proficiency;

namespace BlackSmith.UI
{
    /// <summary>
    /// 레시피 도감 패널. 7×7 격자(도감 번호 순)와 오른쪽 상세, 해금 진행도를 보여 준다. (docs/V2.md §3)
    /// 모르는 칸은 번호와 "???"만 보이고, 해금한 칸은 완성품·등급·재료 구성·숙련도를 보여 준다.
    /// 열 때마다 GameSession.Current 기준으로 다시 그린다. 배치·디자인은 임시값이다.
    /// </summary>
    public class CodexPanel : MonoBehaviour
    {
        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        [Tooltip("격자 칸. 도감 번호 순서대로 채운다. 레시피보다 많으면 남는 칸은 숨긴다.")]
        [SerializeField] private CodexSlotView[] slots;

        [Header("진행도")]
        [SerializeField] private Text progressLabel;
        [Tooltip("Filled 타입 Image.")]
        [SerializeField] private Image progressFill;

        [Header("상세")]
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private Image detailIcon;
        [SerializeField] private Text detailNameLabel;
        [SerializeField] private Text detailGradeLabel;
        [SerializeField] private Text detailIngredientsLabel;
        [SerializeField] private Text detailProficiencyLabel;
        [Tooltip("진행도 막대와 숫자를 묶은 오브젝트. Master면 숨긴다.")]
        [SerializeField] private GameObject detailProgressRoot;
        [SerializeField] private Image detailProgressFill;
        [SerializeField] private Text detailProgressLabel;
        [SerializeField] private Text detailBonusLabel;
        [Tooltip("모르는 칸을 골랐을 때 상세 자리에 보여 줄 글자.")]
        [SerializeField] private Text detailUnknownLabel;

        [Header("닫기")]
        [SerializeField] private Button closeButton;
        [Tooltip("보이고 숨기는 연출. 비워 두면 즉시 켜고 끈다.")]
        [SerializeField] private SaveLoadPanelTransition transition;

        private ProficiencyRules proficiencyRules;
        private bool proficiencyLoaded;
        private CodexSlotView selected;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (slots != null)
            {
                foreach (CodexSlotView slot in slots)
                {
                    if (slot != null) slot.Clicked += Select;
                }
            }
            Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (slots != null)
            {
                foreach (CodexSlotView slot in slots)
                {
                    if (slot != null) slot.Clicked -= Select;
                }
            }
        }

        public void Open()
        {
            if (transition != null && transition.IsPlaying) return;
            if (transition != null) transition.Show();
            else gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (transition != null) transition.Hide();
            else gameObject.SetActive(false);
        }

        /// <summary>격자·진행도·상세를 현재 플레이어 데이터로 다시 그린다.</summary>
        public void Refresh()
        {
            PlayerData player = GameSession.Current;
            List<CraftRecipeData> recipes = RecipeCodex.GetOrdered();
            int slotCount = slots != null ? slots.Length : 0;
            if (recipes.Count > slotCount)
            {
                Debug.LogWarning($"[Codex] 레시피({recipes.Count})가 도감 칸({slotCount})보다 많아 {recipes.Count - slotCount}개는 표시하지 않습니다.");
            }

            int unlockedCount = 0;
            CodexSlotView firstUnlocked = null;
            for (int i = 0; i < slotCount; i++)
            {
                CodexSlotView slot = slots[i];
                if (slot == null) continue;
                bool used = i < recipes.Count;
                slot.gameObject.SetActive(used);
                slot.SetSelected(false);
                if (!used) continue;

                bool unlocked = player.IsRecipeUnlocked(recipes[i].Id);
                slot.Bind(recipes[i], unlocked);
                if (unlocked && firstUnlocked == null) firstUnlocked = slot;
            }
            foreach (CraftRecipeData recipe in recipes)
            {
                if (player.IsRecipeUnlocked(recipe.Id)) unlockedCount++;
            }

            if (progressLabel != null) progressLabel.text = $"해금 {unlockedCount} / {recipes.Count}";
            if (progressFill != null) progressFill.fillAmount = recipes.Count > 0 ? (float)unlockedCount / recipes.Count : 0f;

            CodexSlotView initial = firstUnlocked;
            if (initial == null && slotCount > 0 && recipes.Count > 0) initial = slots[0];
            Select(initial);
        }

        private void Select(CodexSlotView slot)
        {
            if (selected != null) selected.SetSelected(false);
            selected = slot;
            if (selected != null) selected.SetSelected(true);
            ShowDetail(selected);
        }

        private void ShowDetail(CodexSlotView slot)
        {
            bool unlocked = slot != null && slot.IsUnlocked;
            if (detailRoot != null) detailRoot.SetActive(unlocked);
            if (detailUnknownLabel != null)
            {
                detailUnknownLabel.gameObject.SetActive(!unlocked);
                detailUnknownLabel.text = slot != null && slot.Recipe != null ? $"No.{slot.Recipe.CodexNumber}\n???" : "???";
            }
            if (!unlocked) return;

            CraftRecipeData recipe = slot.Recipe;
            CraftedItemData product = recipe.Result;

            if (detailIcon != null)
            {
                detailIcon.sprite = product.Icon;
                detailIcon.enabled = product.Icon != null;
            }
            if (detailNameLabel != null) detailNameLabel.text = $"No.{recipe.CodexNumber} {product.DisplayName}";
            if (detailGradeLabel != null) detailGradeLabel.text = product.DefaultGrade.ToString();
            if (detailIngredientsLabel != null) detailIngredientsLabel.text = DescribeIngredients(recipe);

            ShowProficiency(recipe, product.DefaultGrade);
        }

        private void ShowProficiency(CraftRecipeData recipe, ItemGrade grade)
        {
            ProficiencyRules rules = GetProficiencyRules();
            // 숙련도 기록이 없으면(20점 미만으로만 만든 경우) 0점 상태로 보여 준다.
            ProficiencyState state = rules != null ? rules.Evaluate(GameSession.Current.GetProficiencyPoints(recipe.Id), grade) : null;

            if (detailProficiencyLabel != null)
            {
                detailProficiencyLabel.gameObject.SetActive(state != null);
                if (state != null) detailProficiencyLabel.text = state.Label;
            }

            bool showProgress = state != null && !state.IsMaster;
            if (detailProgressRoot != null) detailProgressRoot.SetActive(showProgress);
            if (showProgress)
            {
                if (detailProgressFill != null) detailProgressFill.fillAmount = state.Required > 0 ? (float)state.Progress / state.Required : 0f;
                if (detailProgressLabel != null) detailProgressLabel.text = $"{state.Progress} / {state.Required}";
            }

            if (detailBonusLabel != null)
            {
                detailBonusLabel.gameObject.SetActive(state != null);
                if (state != null) detailBonusLabel.text = "품질 보너스 +" + state.Bonus.ToString("0.#", CultureInfo.InvariantCulture);
            }
        }

        private static string DescribeIngredients(CraftRecipeData recipe)
        {
            StringBuilder text = new StringBuilder();
            foreach (KeyValuePair<MineralItemData, int> pair in CraftScoring.SumRecipeIngredients(recipe))
            {
                if (text.Length > 0) text.Append('\n');
                text.Append(pair.Key.DisplayName).Append(" × ").Append(pair.Value);
            }
            return text.ToString();
        }

        private ProficiencyRules GetProficiencyRules()
        {
            if (proficiencyLoaded) return proficiencyRules;
            proficiencyLoaded = true;
            ProficiencySettings settings = ProficiencySettings.Load();
            if (settings != null) proficiencyRules = new ProficiencyRules(settings);
            return proficiencyRules;
        }
    }
}
