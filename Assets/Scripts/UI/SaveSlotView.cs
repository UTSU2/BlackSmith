using System;
using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Data;

namespace BlackSmith.UI
{
    /// <summary>저장 슬롯 한 칸. 버튼과 글자를 가지며 클릭을 패널에 전달한다.</summary>
    public class SaveSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text label;

        private int index;

        public event Action<int> Clicked;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (label == null) label = GetComponentInChildren<Text>(true);
            if (button != null) button.onClick.AddListener(() => Clicked?.Invoke(index));
        }

        public void Refresh(int slotIndex)
        {
            index = slotIndex;
            if (label == null) return;

            string number = $"슬롯 {slotIndex + 1}";
            if (SaveSlotManager.TryGetSavedAt(slotIndex, out DateTime savedAt))
            {
                label.text = $"{number}  {savedAt:yyyy-MM-dd HH:mm}";
            }
            else
            {
                label.text = $"{number}  빈 슬롯";
            }
        }
    }
}
