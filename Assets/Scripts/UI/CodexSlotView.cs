using System;
using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Crafting;

namespace BlackSmith.UI
{
    /// <summary>
    /// 도감 격자의 칸 하나. 해금한 레시피는 번호와 완성품 아이콘, 모르는 레시피는 번호와 "???"만 보여 준다.
    /// 누르면 Clicked로 알리고, 상세 표시는 CodexPanel이 맡는다.
    /// </summary>
    public class CodexSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text numberLabel;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text unknownLabel;
        [Tooltip("선택된 칸 표시. 선택되지 않으면 숨긴다.")]
        [SerializeField] private GameObject selectedFrame;

        [Header("색 (임시값)")]
        [SerializeField] private Color unlockedColor = new Color(0.94f, 0.88f, 0.75f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.66f, 0.56f, 0.42f, 1f);

        public event Action<CodexSlotView> Clicked;

        public CraftRecipeData Recipe { get; private set; }
        public bool IsUnlocked { get; private set; }

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
        }

        public void Bind(CraftRecipeData recipe, bool unlocked)
        {
            Recipe = recipe;
            IsUnlocked = unlocked && recipe != null;

            if (numberLabel != null) numberLabel.text = recipe != null ? recipe.CodexNumber.ToString() : "";
            if (background != null) background.color = IsUnlocked ? unlockedColor : lockedColor;

            Sprite icon = IsUnlocked && recipe.Result != null ? recipe.Result.Icon : null;
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
            // 아이콘이 없는 장비도 해금 여부가 보이도록, 해금했는데 아이콘이 없으면 이름 첫 글자를 대신 보여 준다.
            if (unknownLabel != null)
            {
                unknownLabel.gameObject.SetActive(!IsUnlocked || icon == null);
                unknownLabel.text = IsUnlocked ? FirstLetter(recipe) : "???";
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedFrame != null) selectedFrame.SetActive(selected);
        }

        private static string FirstLetter(CraftRecipeData recipe)
        {
            string name = recipe != null && recipe.Result != null ? recipe.Result.DisplayName : "";
            return string.IsNullOrEmpty(name) ? "" : name.Substring(0, 1);
        }

        private void OnClicked()
        {
            Clicked?.Invoke(this);
        }
    }
}
