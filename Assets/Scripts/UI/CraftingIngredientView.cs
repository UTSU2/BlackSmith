using System;
using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Items;

namespace BlackSmith.UI
{
    /// <summary>
    /// 재료 투입 목록의 한 줄. 아이콘, 이름(등급), 투입 수량/보유 수량을 보여주고
    /// + / - 버튼으로 투입 수량을 바꿔 달라고 알린다. 수량 규칙은 CraftingSession이 맡는다.
    /// </summary>
    public class CraftingIngredientView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text countLabel;
        [SerializeField] private Button addButton;
        [SerializeField] private Button removeButton;

        private ItemData item;
        private int owned;

        public ItemData Item => item;

        public event Action<ItemData> AddClicked;
        public event Action<ItemData> RemoveClicked;

        private void Awake()
        {
            if (addButton != null) addButton.onClick.AddListener(OnAdd);
            if (removeButton != null) removeButton.onClick.AddListener(OnRemove);
        }

        private void OnDestroy()
        {
            if (addButton != null) addButton.onClick.RemoveListener(OnAdd);
            if (removeButton != null) removeButton.onClick.RemoveListener(OnRemove);
        }

        public void Bind(ItemData data, int ownedCount, int insertedCount)
        {
            item = data;
            owned = ownedCount;

            if (nameLabel != null) nameLabel.text = $"{data.DisplayName} ({data.DefaultGrade})";
            if (iconImage != null)
            {
                iconImage.sprite = data.Icon;
                iconImage.enabled = data.Icon != null;
            }
            SetInserted(insertedCount);
        }

        public void SetInserted(int insertedCount)
        {
            if (countLabel != null) countLabel.text = $"{insertedCount} / {owned}";
        }

        private void OnAdd()
        {
            if (item != null) AddClicked?.Invoke(item);
        }

        private void OnRemove()
        {
            if (item != null) RemoveClicked?.Invoke(item);
        }
    }
}
