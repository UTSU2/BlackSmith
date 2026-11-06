using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BlackSmith.Crafting;

namespace BlackSmith.UI
{
    /// <summary>
    /// 열처리 미니게임 화면. HeatTreatMinigame의 위치를 읽어 가로 게이지 위의 오브젝트와 목표 위치를 그리고,
    /// 클릭을 전달한다. 판정은 하지 않는다. 이 오브젝트(PlayArea)가 클릭을 받으므로 Image(raycastTarget)가 있어야 한다.
    /// </summary>
    public class HeatTreatMinigameView : MonoBehaviour, IPointerDownHandler
    {
        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        [Header("구성")]
        [Tooltip("게이지가 그려지는 가로 영역. 앵커 기준으로 목표 위치와 오브젝트를 배치한다.")]
        [SerializeField] private RectTransform gaugeArea;
        [Tooltip("목표 위치(완성도 최대 구간). gaugeArea의 자식이어야 한다.")]
        [SerializeField] private RectTransform targetZone;
        [Tooltip("왕복하는 오브젝트. gaugeArea의 자식이어야 한다. 앵커 x를 코드로 움직인다.")]
        [SerializeField] private RectTransform marker;
        [Tooltip("남은 시간 표시. 비워 두면 표시하지 않는다.")]
        [SerializeField] private RectTransform timeFill;
        [Tooltip("Show/Hide로 켜고 끌 오브젝트(배경 포함 패널). 비워 두면 이 오브젝트를 쓴다.")]
        [SerializeField] private GameObject panelRoot;

        [Header("색")]
        [SerializeField] private Color zoneColor = new Color(0.3f, 0.9f, 0.4f, 0.4f);
        [SerializeField] private Color markerColor = new Color(1f, 0.6f, 0.15f, 1f);
        [Tooltip("판정 뒤 오브젝트 색. 완성도 0일 때(나쁨)와 1일 때(좋음)를 오가며 섞는다.")]
        [SerializeField] private Color badResultColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color goodResultColor = new Color(0.4f, 1f, 0.4f, 1f);

        private HeatTreatMinigame game;
        private Image markerImage;

        private GameObject Root => panelRoot != null ? panelRoot : gameObject;

        private void Awake()
        {
            Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;
        }

        /// <summary>미니게임을 보여주기 시작한다. 게임 시작(Begin)은 호출한 쪽이 한다.</summary>
        public void Show(HeatTreatMinigame heatTreat)
        {
            game = heatTreat;
            Root.SetActive(true);
            if (marker != null && markerImage == null) markerImage = marker.GetComponent<Image>();
            ApplyZone();
            Draw();
        }

        public void Hide()
        {
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
            float c = game.TargetPosition;
            float h = game.PerfectHalfWidth;
            targetZone.anchorMin = new Vector2(Mathf.Clamp01(c - h), 0f);
            targetZone.anchorMax = new Vector2(Mathf.Clamp01(c + h), 1f);
            targetZone.offsetMin = Vector2.zero;
            targetZone.offsetMax = Vector2.zero;
            Image zoneImage = targetZone.GetComponent<Image>();
            if (zoneImage != null) zoneImage.color = zoneColor;
        }

        private void Draw()
        {
            if (marker != null)
            {
                float p = game.Position;
                marker.anchorMin = new Vector2(p, 0.5f);
                marker.anchorMax = new Vector2(p, 0.5f);
                marker.anchoredPosition = Vector2.zero;
                if (markerImage != null)
                {
                    markerImage.color = game.IsJudged
                        ? Color.Lerp(badResultColor, goodResultColor, game.Completeness)
                        : markerColor;
                }
            }

            if (timeFill != null)
            {
                float remaining = game.IsJudged ? 0f : Mathf.Clamp01(1f - game.Time / game.Settings.timeLimit);
                timeFill.anchorMin = new Vector2(0f, 0f);
                timeFill.anchorMax = new Vector2(remaining, 1f);
                timeFill.offsetMin = Vector2.zero;
                timeFill.offsetMax = Vector2.zero;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (game == null || eventData.button != PointerEventData.InputButton.Left) return;
            game.HandleClick();
        }
    }
}
