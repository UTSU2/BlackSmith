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
        [SerializeField] private Text messageLabel;

        public bool IsShown => gameObject.activeSelf;

        private void Awake()
        {
            // 글꼴은 크기 기준(UIFontSettings)으로 시작할 때 적용한다.
            UIFonts.ApplyAll(this);
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
