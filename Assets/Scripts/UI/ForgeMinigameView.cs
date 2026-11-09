using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using BlackSmith.Crafting;

namespace BlackSmith.UI
{
    /// <summary>
    /// 단조 미니게임 화면. ForgeMinigame의 상태를 읽어 원을 그리고, 누른 위치를 미니게임 공간 좌표로 바꿔 전달한다.
    /// 판정은 하지 않는다. 이 오브젝트(PlayArea)가 클릭을 받으므로 Image(raycastTarget)가 있어야 한다.
    /// </summary>
    public class ForgeMinigameView : MonoBehaviour, IPointerDownHandler
    {
        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        [Tooltip("원 하나의 모양. 'Target'(안쪽 원)과 'Shrinking'(줄어드는 원) 자식이 있어야 한다. 비활성 상태로 둔다.")]
        [SerializeField] private RectTransform circleTemplate;
        [Tooltip("원이 그려지는 영역. 비워 두면 이 오브젝트를 쓴다.")]
        [SerializeField] private RectTransform playArea;
        [Tooltip("Show/Hide로 켜고 끌 오브젝트(배경 포함 패널). 비워 두면 이 오브젝트를 쓴다.")]
        [SerializeField] private GameObject panelRoot;

        [Header("색")]
        [SerializeField] private Color targetColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color shrinkingColor = new Color(1f, 0.6f, 0.15f, 0.9f);
        [FormerlySerializedAs("hitColor")]
        [SerializeField] private Color perfectColor = new Color(0.4f, 1f, 0.4f, 0.8f);
        [SerializeField] private Color goodColor = new Color(1f, 0.9f, 0.3f, 0.8f);
        [SerializeField] private Color missColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        [Tooltip("판정 뒤 결과 색으로 남아 있는 시간(초).")]
        [SerializeField] private float resultLingerTime = 0.3f;

        private class CircleView
        {
            public RectTransform Root;
            public Image Target;
            public Image Shrinking;
        }

        private readonly List<CircleView> circleViews = new List<CircleView>();
        private ForgeMinigame game;

        private void Awake()
        {
            if (playArea == null) playArea = (RectTransform)transform;
            if (circleTemplate != null) circleTemplate.gameObject.SetActive(false);

            Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;
        }

        /// <summary>미니게임을 보여주기 시작한다. 게임 시작(Begin)은 호출한 쪽이 한다.</summary>
        public void Show(ForgeMinigame forge)
        {
            ClearCircles();
            game = forge;
            Root.SetActive(true);
            // Awake가 아직 안 불렸을 수 있으므로(처음 활성화) 활성화한 뒤에 확인한다.
            if (playArea == null) playArea = (RectTransform)transform;
        }

        public void Hide()
        {
            game = null;
            ClearCircles();
            Root.SetActive(false);
        }

        private GameObject Root => panelRoot != null ? panelRoot : gameObject;

        private void Update()
        {
            if (game == null || circleTemplate == null) return;

            // 원 목록은 Begin 이후에 채워지므로 부족한 만큼 만든다.
            while (circleViews.Count < game.Circles.Count) circleViews.Add(CreateCircleView());

            float scale = playArea.rect.width / ForgeMinigame.AreaSize.x;
            for (int i = 0; i < game.Circles.Count; i++)
            {
                Draw(circleViews[i], game.Circles[i], scale);
            }
        }

        private void Draw(CircleView view, ForgeMinigame.Circle circle, float scale)
        {
            bool judged = circle.State != ForgeMinigame.CircleState.Pending;
            bool started = game.Time >= circle.SpawnTime;
            bool lingering = judged && game.Time - circle.JudgedTime < resultLingerTime;
            bool visible = started && (!judged || lingering);

            view.Root.gameObject.SetActive(visible);
            if (!visible) return;

            // 미니게임 공간(왼쪽 아래 기준) → PlayArea 로컬 좌표
            Rect rect = playArea.rect;
            Vector2 local = new Vector2(
                rect.xMin + circle.Position.x * scale,
                rect.yMin + circle.Position.y * scale);
            view.Root.anchoredPosition = local - rect.center;

            float targetSize = game.Settings.targetRadius * 2f * scale;
            view.Target.rectTransform.sizeDelta = new Vector2(targetSize, targetSize);

            if (judged)
            {
                view.Shrinking.enabled = false;
                view.Target.color = circle.State == ForgeMinigame.CircleState.Perfect ? perfectColor
                    : circle.State == ForgeMinigame.CircleState.Good ? goodColor
                    : missColor;
            }
            else
            {
                float radius = Mathf.Max(game.GetRadius(circle), 0f);
                float size = radius * 2f * scale;
                view.Shrinking.enabled = true;
                view.Shrinking.rectTransform.sizeDelta = new Vector2(size, size);
                view.Shrinking.color = shrinkingColor;
                view.Target.color = targetColor;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (game == null || eventData.button != PointerEventData.InputButton.Left) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    playArea, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                return;
            }

            Rect rect = playArea.rect;
            float scale = rect.width / ForgeMinigame.AreaSize.x;
            Vector2 position = new Vector2((local.x - rect.xMin) / scale, (local.y - rect.yMin) / scale);
            game.HandleClick(position);
        }

        private CircleView CreateCircleView()
        {
            RectTransform root = Instantiate(circleTemplate, playArea);
            root.gameObject.SetActive(false);
            CircleView view = new CircleView
            {
                Root = root,
                Target = root.Find("Target").GetComponent<Image>(),
                Shrinking = root.Find("Shrinking").GetComponent<Image>(),
            };
            // 클릭은 PlayArea가 받는다. 원 이미지가 가로채지 않게 한다.
            view.Target.raycastTarget = false;
            view.Shrinking.raycastTarget = false;
            return view;
        }

        private void ClearCircles()
        {
            foreach (CircleView view in circleViews)
            {
                if (view != null && view.Root != null)
                {
                    view.Root.gameObject.SetActive(false);
                    Destroy(view.Root.gameObject);
                }
            }
            circleViews.Clear();
        }
    }
}
