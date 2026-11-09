using UnityEngine;
using UnityEngine.UI;

namespace BlackSmith
{
    /// <summary>
    /// 버튼이 지금 실제로 눌림 스프라이트를 보여 주는 동안 Label을 pressedPosition으로 내린다.
    /// 포인터 이벤트를 따로 추정하지 않고 Button의 현재 스프라이트를 그대로 따르므로
    /// 버튼 이미지와 글자 위치가 항상 일치한다.
    /// 평소 위치는 씬에 저장된 Label의 anchoredPosition을 그대로 사용한다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonLabelPress : MonoBehaviour
    {
        [SerializeField] private RectTransform label;
        [SerializeField] private Vector2 pressedPosition = Vector2.zero;

        private Button button;
        private Image image;
        private Vector2 normalPosition;

        private void Awake()
        {
            button = GetComponent<Button>();
            image = button.targetGraphic as Image;

            if (label == null)
            {
                Transform found = transform.Find("Label");
                if (found != null) label = (RectTransform)found;
            }
            if (label != null) normalPosition = label.anchoredPosition;
        }

        private void LateUpdate()
        {
            if (label == null || image == null) return;

            Sprite pressedSprite = button.spriteState.pressedSprite;
            bool pressed = pressedSprite != null && image.overrideSprite == pressedSprite;
            Vector2 target = pressed ? pressedPosition : normalPosition;
            if (label.anchoredPosition != target) label.anchoredPosition = target;
        }

        private void OnDisable()
        {
            if (label != null) label.anchoredPosition = normalPosition;
        }
    }
}
