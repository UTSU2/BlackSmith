using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlackSmith.UI
{
    /// <summary>튜토리얼 안내 팝업. 확인이 필요한 단계만 전체 화면을 막고 확인 버튼을 보여 준다.</summary>
    public class TutorialPopup : MonoBehaviour
    {
        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        [SerializeField] private Text messageLabel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private GameObject backdrop;

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
        public void Show(string message, bool needsConfirm)
        {
            if (string.IsNullOrEmpty(message))
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);
            if (messageLabel != null) messageLabel.text = message;
            if (confirmButton != null) confirmButton.gameObject.SetActive(needsConfirm);
            if (backdrop != null) backdrop.SetActive(needsConfirm);
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
