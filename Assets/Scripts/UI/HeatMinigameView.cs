using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BlackSmith.Crafting;

namespace BlackSmith.UI
{
    /// <summary>
    /// 가열 미니게임 화면. HeatMinigame의 바 값을 읽어 세로 바와 목표 구간을 그리고,
    /// 마우스 누름/뗌을 전달한다. 판정은 하지 않는다. 이 오브젝트(PlayArea)가 클릭을 받으므로 Image(raycastTarget)가 있어야 한다.
    /// </summary>
    public class HeatMinigameView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        [Header("구성")]
        [Tooltip("바가 그려지는 세로 영역. 앵커 기준으로 목표 구간과 표시기를 배치한다.")]
        [SerializeField] private RectTransform barArea;
        [Tooltip("목표 구간. barArea의 자식이어야 한다.")]
        [SerializeField] private RectTransform targetZone;
        [Tooltip("현재 바 값을 보여주는 표시기(가로 막대). barArea의 자식이어야 한다.")]
        [SerializeField] private RectTransform indicator;
        [Tooltip("남은 시간 표시. 가로로 줄어드는 Image(Filled 방식이 아니어도 된다). 비워 두면 표시하지 않는다.")]
        [SerializeField] private RectTransform timeFill;
        [Tooltip("Show/Hide로 켜고 끌 오브젝트(배경 포함 패널). 비워 두면 이 오브젝트를 쓴다.")]
        [SerializeField] private GameObject panelRoot;

        [Header("색")]
        [SerializeField] private Color zoneColor = new Color(0.3f, 0.9f, 0.4f, 0.35f);
        [SerializeField] private Color indicatorInZoneColor = new Color(0.4f, 1f, 0.4f, 1f);
        [SerializeField] private Color indicatorOutColor = new Color(1f, 0.6f, 0.15f, 1f);

        [Tooltip("표시기 막대의 두께(px).")]
        [SerializeField] private float indicatorThickness = 18f;

        private HeatMinigame game;
        private Image indicatorImage;

        private GameObject Root => panelRoot != null ? panelRoot : gameObject;

        private void Awake()
        {
            Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;
        }

        /// <summary>미니게임을 보여주기 시작한다. 게임 시작(Begin)은 호출한 쪽이 한다.</summary>
        public void Show(HeatMinigame heat)
        {
            game = heat;
            Root.SetActive(true);
            if (indicator != null && indicatorImage == null) indicatorImage = indicator.GetComponent<Image>();
            ApplyZone();
            Draw();
        }

        public void Hide()
        {
            if (game != null) game.SetHolding(false);
            game = null;
            Root.SetActive(false);
        }

        private void Update()
        {
            if (game == null) return;
            Draw();
        }

        private void ApplyZone()
        {
            if (targetZone == null) return;
            float c = game.Center;
            float h = game.HalfWidth;
            targetZone.anchorMin = new Vector2(0f, Mathf.Clamp01(c - h));
            targetZone.anchorMax = new Vector2(1f, Mathf.Clamp01(c + h));
            targetZone.offsetMin = Vector2.zero;
            targetZone.offsetMax = Vector2.zero;
            Image zoneImage = targetZone.GetComponent<Image>();
            if (zoneImage != null) zoneImage.color = zoneColor;
        }

        private void Draw()
        {
            if (indicator != null)
            {
                indicator.anchorMin = new Vector2(0f, game.Value);
                indicator.anchorMax = new Vector2(1f, game.Value);
                indicator.pivot = new Vector2(0.5f, 0.5f);
                indicator.offsetMin = new Vector2(0f, -indicatorThickness * 0.5f);
                indicator.offsetMax = new Vector2(0f, indicatorThickness * 0.5f);
                if (indicatorImage != null) indicatorImage.color = game.IsInZone ? indicatorInZoneColor : indicatorOutColor;
            }

            if (timeFill != null)
            {
                float remaining = Mathf.Clamp01(1f - game.Time / game.Settings.duration);
                timeFill.anchorMin = new Vector2(0f, 0f);
                timeFill.anchorMax = new Vector2(remaining, 1f);
                timeFill.offsetMin = Vector2.zero;
                timeFill.offsetMax = Vector2.zero;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (game == null || eventData.button != PointerEventData.InputButton.Left) return;
            game.SetHolding(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (game == null || eventData.button != PointerEventData.InputButton.Left) return;
            game.SetHolding(false);
        }
    }
}
