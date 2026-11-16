using System;
using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Tutorial;

namespace BlackSmith.UI
{
    /// <summary>튜토리얼 안내 팝업. 확인이 필요한 단계만 전체 화면을 막고 확인 버튼을 보여 준다.</summary>
    public class TutorialPopup : MonoBehaviour
    {
        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        [SerializeField] private Text messageLabel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private GameObject backdrop;
        [Tooltip("위치를 옮길 안내 상자. 비워 두면 위치를 바꾸지 않는다.")]
        [SerializeField] private RectTransform box;
        [Tooltip("화면 가장자리에서 띄우는 거리.")]
        [SerializeField] private float edgeMargin = 8f;

        public event Action Confirmed;

        private void Awake()
        {
            // OS 폰트는 씬에 저장되지 않으므로 시작할 때 글자에 적용한다.
            Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;
        }

        private void OnEnable()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        private void OnDisable()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
        }

        /// <summary>문구를 보여 준다. 빈 문자열이면 숨긴다.</summary>
        public void Show(string message, bool needsConfirm, TutorialPopupPosition position = TutorialPopupPosition.Top)
        {
            if (string.IsNullOrEmpty(message))
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);
            Place(position);
            if (messageLabel != null) messageLabel.text = message;
            if (confirmButton != null) confirmButton.gameObject.SetActive(needsConfirm);
            if (backdrop != null) backdrop.SetActive(needsConfirm);
        }

        // 가운데 가로 정렬은 유지하고 상단/하단 가장자리에 붙인다. 크기는 씬 값을 그대로 쓴다.
        private void Place(TutorialPopupPosition position)
        {
            if (box == null) return;
            bool top = position == TutorialPopupPosition.Top;
            float y = top ? 1f : 0f;
            box.anchorMin = new Vector2(0.5f, y);
            box.anchorMax = new Vector2(0.5f, y);
            box.pivot = new Vector2(0.5f, y);
            box.anchoredPosition = new Vector2(0f, top ? -edgeMargin : edgeMargin);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnConfirmClicked()
        {
            Confirmed?.Invoke();
        }
    }
}
