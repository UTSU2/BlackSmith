using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BlackSmith.Crafting;

namespace BlackSmith.UI
{
    /// <summary>
    /// 연마 미니게임 화면. PolishMinigame의 흠집을 읽어 그리고, 누름/드래그/뗌을 미니게임 공간 좌표로 바꿔 전달한다.
    /// 판정은 하지 않는다. 이 오브젝트(PlayArea)가 입력을 받으므로 Image(raycastTarget)가 있어야 한다.
    /// </summary>
    public class PolishMinigameView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        [Tooltip("흠집 하나의 모양. 비활성 상태로 둔다. 자식 없이 Image 하나면 된다.")]
        [SerializeField] private RectTransform flawTemplate;
        [Tooltip("흠집이 그려지는 영역. 비워 두면 이 오브젝트를 쓴다.")]
        [SerializeField] private RectTransform playArea;
        [Tooltip("남은 시간 표시. 비워 두면 표시하지 않는다.")]
        [SerializeField] private RectTransform timeFill;
        [Tooltip("Show/Hide로 켜고 끌 오브젝트(배경 포함 패널). 비워 두면 이 오브젝트를 쓴다.")]
        [SerializeField] private GameObject panelRoot;

        [Header("색")]
        [SerializeField] private Color flawColor = new Color(0.55f, 0.25f, 0.1f, 1f);
        [Tooltip("지금 문지르고 있는 흠집 색.")]
        [SerializeField] private Color activeFlawColor = new Color(0.9f, 0.45f, 0.15f, 1f);
        [Tooltip("남은 양이 적어도 이 비율 아래로는 흐려지지 않는다. (0이면 거의 사라질 때까지 흐려진다)")]
        [Range(0f, 1f)]
        [SerializeField] private float minVisibleAlpha = 0.25f;

        private readonly List<Image> flawViews = new List<Image>();
        private PolishMinigame game;

        private GameObject Root => panelRoot != null ? panelRoot : gameObject;

        private void Awake()
        {
            if (playArea == null) playArea = (RectTransform)transform;
            if (flawTemplate != null) flawTemplate.gameObject.SetActive(false);

            Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;
        }

        /// <summary>미니게임을 보여주기 시작한다. 게임 시작(Begin)은 호출한 쪽이 한다.</summary>
        public void Show(PolishMinigame polish)
        {
            ClearFlaws();
            game = polish;
            Root.SetActive(true);
            // Awake가 아직 안 불렸을 수 있으므로(처음 활성화) 활성화한 뒤에 확인한다.
            if (playArea == null) playArea = (RectTransform)transform;
        }

        public void Hide()
        {
            if (game != null) game.PointerUp();
            game = null;
            ClearFlaws();
            Root.SetActive(false);
        }

        private void Update()
        {
            if (game == null || flawTemplate == null) return;

            // 흠집 목록은 Begin 이후에 채워지므로 부족한 만큼 만든다.
            while (flawViews.Count < game.Flaws.Count) flawViews.Add(CreateFlawView());

            float scale = playArea.rect.width / PolishMinigame.AreaSize.x;
            Rect rect = playArea.rect;
            float diameter = game.Settings.flawRadius * 2f * scale;

            for (int i = 0; i < game.Flaws.Count; i++)
            {
                PolishMinigame.Flaw flaw = game.Flaws[i];
                Image view = flawViews[i];
                view.gameObject.SetActive(!flaw.IsCleared);
                if (flaw.IsCleared) continue;

                Vector2 local = new Vector2(rect.xMin + flaw.Position.x * scale, rect.yMin + flaw.Position.y * scale);
                view.rectTransform.anchoredPosition = local - rect.center;
                view.rectTransform.sizeDelta = new Vector2(diameter, diameter);

                Color color = flaw == game.ActiveFlaw ? activeFlawColor : flawColor;
                color.a *= Mathf.Lerp(minVisibleAlpha, 1f, Mathf.Clamp01(flaw.Amount));
                view.color = color;
            }

            if (timeFill != null)
            {
                float remaining = game.IsFinished ? 0f : Mathf.Clamp01(1f - game.Time / game.Settings.timeLimit);
                timeFill.anchorMin = new Vector2(0f, 0f);
                timeFill.anchorMax = new Vector2(remaining, 1f);
                timeFill.offsetMin = Vector2.zero;
                timeFill.offsetMax = Vector2.zero;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (game == null || eventData.button != PointerEventData.InputButton.Left) return;
            if (TryToGamePosition(eventData, out Vector2 position)) game.PointerDown(position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (game == null || eventData.button != PointerEventData.InputButton.Left) return;
            if (TryToGamePosition(eventData, out Vector2 position)) game.PointerMove(position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (game == null || eventData.button != PointerEventData.InputButton.Left) return;
            game.PointerUp();
        }

        /// <summary>화면 좌표를 미니게임 공간 좌표(왼쪽 아래 기준)로 바꾼다.</summary>
        private bool TryToGamePosition(PointerEventData eventData, out Vector2 position)
        {
            position = Vector2.zero;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    playArea, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                return false;
            }

            Rect rect = playArea.rect;
            float scale = rect.width / PolishMinigame.AreaSize.x;
            position = new Vector2((local.x - rect.xMin) / scale, (local.y - rect.yMin) / scale);
            return true;
        }

        private Image CreateFlawView()
        {
            RectTransform root = Instantiate(flawTemplate, playArea);
            root.gameObject.SetActive(false);
            Image image = root.GetComponent<Image>();
            // 입력은 PlayArea가 받는다. 흠집 이미지가 가로채지 않게 한다.
            image.raycastTarget = false;
            return image;
        }

        private void ClearFlaws()
        {
            foreach (Image view in flawViews)
            {
                if (view != null)
                {
                    view.gameObject.SetActive(false);
                    Destroy(view.gameObject);
                }
            }
            flawViews.Clear();
        }
    }
}
