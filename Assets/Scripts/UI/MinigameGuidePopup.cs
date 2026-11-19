using UnityEngine;
using UnityEngine.UI;

namespace BlackSmith.UI
{
    /// <summary>
    /// 튜토리얼 제작 중 화면 왼쪽 상단에 미니게임 조작 안내를 보여 주는 반투명 상자. (docs/Tutorial.md)
    /// 미니게임 영역 위에 겹치므로 클릭을 막지 않는다(글자·배경 모두 레이캐스트 대상이 아니다).
    /// 언제 무엇을 보여 줄지는 TutorialController가 정한다. 배치·디자인은 임시값이다.
    /// </summary>
    public class MinigameGuidePopup : MonoBehaviour
    {
        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        [SerializeField] private Text messageLabel;

        public bool IsShown => gameObject.activeSelf;

        private void Awake()
        {
            // OS 폰트는 씬에 저장되지 않으므로 시작할 때 글자에 적용한다.
            Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;
            // 아래 미니게임 입력을 막지 않는다.
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }

        /// <summary>문구를 보여 준다. 빈 문자열이면 숨긴다.</summary>
        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                Hide();
                return;
            }
            gameObject.SetActive(true);
            if (messageLabel != null) messageLabel.text = message;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
