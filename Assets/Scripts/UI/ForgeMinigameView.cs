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

        [Header("적중 이펙트 (비워 두면 이펙트 없이 진행)")]
        [SerializeField] private HitEffectSpawner hitEffects;
        [Tooltip("판정별 도넛형 이펙트. 기준 지름(effectReferenceDiameter) 원의 바깥으로만 퍼지게 만든 프리팹이다.")]
        [SerializeField] private ParticleSystem perfectEffect;
        [SerializeField] private ParticleSystem goodEffect;
        [Tooltip("눌렀지만 Miss일 때. 누르지 못하고 놓친 Miss는 이펙트가 없다.")]
        [SerializeField] private ParticleSystem missEffect;
        [Tooltip("이펙트 프리팹이 맞춰 만든 판정 원 지름(px). 실제 원 지름 ÷ 이 값으로 키우거나 줄여 구멍이 원에 맞게 한다.")]
        [SerializeField] private float effectReferenceDiameter = 160f;

        [Header("망치·주괴 연출 (비워 두면 생략)")]
        [Tooltip("눌러서 판정된 원마다 그 자리를 내리찍는 망치. 비활성 상태로 두고, playArea의 자식이며 앵커는 가운데로 한다. 피벗은 머리의 타격면에 맞춘다.")]
        [SerializeField] private RectTransform hammer;
        [Tooltip("망치로 칠 때 살짝 눌렸다 돌아오는 주괴. 비워 두면 반응이 없다.")]
        [SerializeField] private RectTransform ingot;
        [Tooltip("들어올린 망치의 각도(도). 내리치면 0도가 된다. 양수는 반시계 방향.")]
        [SerializeField] private float hammerRaisedAngle = 35f;
        [Tooltip("들어올린 망치의 타격 지점 기준 위치(px). 내리치면 (0,0)이 된다.")]
        [SerializeField] private Vector2 hammerRaisedOffset = new Vector2(50f, 80f);
        [Tooltip("내리치는 데 걸리는 시간(초).")]
        [SerializeField] private float hammerSwingTime = 0.05f;
        [Tooltip("내리친 뒤 멈춰 있는 시간(초).")]
        [SerializeField] private float hammerHoldTime = 0.06f;
        [Tooltip("멈춘 뒤 사라지는 시간(초).")]
        [SerializeField] private float hammerFadeTime = 0.09f;
        [Tooltip("내리치는 순간 주괴가 눌리는 배율(1이면 반응 없음).")]
        [SerializeField] private float ingotPressScale = 0.97f;
        [Tooltip("주괴가 눌렸다 돌아오는 시간(초).")]
        [SerializeField] private float ingotPressTime = 0.08f;

        private class CircleView
        {
            public RectTransform Root;
            public Image Target;
            public Image Shrinking;
        }

        private readonly List<CircleView> circleViews = new List<CircleView>();
        private ForgeMinigame game;

        // 망치 애니메이션: 시작한 뒤 지난 시간(초). 음수면 쉬는 중.
        private float hammerElapsed = -1f;
        private Vector2 hammerPoint;
        private Vector3 ingotBaseScale = Vector3.one;

        private void Awake()
        {
            if (playArea == null) playArea = (RectTransform)transform;
            if (circleTemplate != null) circleTemplate.gameObject.SetActive(false);
            // 클릭은 PlayArea가 받는다. 망치·주괴 이미지가 가로채지 않게 한다.
            DisableRaycast(hammer);
            DisableRaycast(ingot);
            if (ingot != null) ingotBaseScale = ingot.localScale;
            ResetHammer();

            UIFonts.ApplyAll(this);
        }

        private static void DisableRaycast(RectTransform target)
        {
            if (target == null) return;
            Image image = target.GetComponent<Image>();
            if (image != null) image.raycastTarget = false;
        }

        /// <summary>미니게임을 보여주기 시작한다. 게임 시작(Begin)은 호출한 쪽이 한다.</summary>
        public void Show(ForgeMinigame forge)
        {
            Unsubscribe();
            ClearCircles();
            ResetHammer();
            if (hitEffects != null) hitEffects.Clear();
            game = forge;
            if (game != null) game.CircleJudged += OnCircleJudged;
            Root.SetActive(true);
            // Awake가 아직 안 불렸을 수 있으므로(처음 활성화) 활성화한 뒤에 확인한다.
            if (playArea == null) playArea = (RectTransform)transform;
        }

        public void Hide()
        {
            Unsubscribe();
            game = null;
            ClearCircles();
            ResetHammer();
            if (hitEffects != null) hitEffects.Clear();
            Root.SetActive(false);
        }

        private void Unsubscribe()
        {
            if (game != null) game.CircleJudged -= OnCircleJudged;
        }

        // 눌러서 판정된 원 자리를 망치가 내리치고 빛이 터진다. 놓친 Miss(누르지 않음)는 아무것도 하지 않는다.
        private void OnCircleJudged(ForgeMinigame.Circle circle)
        {
            if (game == null || !circle.Clicked) return;

            StartHammer(circle.Position);
            PlayHitEffect(circle);
        }

        // 판정된 원 자리에 판정별 도넛형 빛을 띄운다. 크기는 판정 원 지름에 맞춘다.
        private void PlayHitEffect(ForgeMinigame.Circle circle)
        {
            if (hitEffects == null || effectReferenceDiameter <= 0f) return;

            ParticleSystem effect = circle.State == ForgeMinigame.CircleState.Perfect ? perfectEffect
                : circle.State == ForgeMinigame.CircleState.Good ? goodEffect
                : missEffect;
            float circleDiameter = game.Settings.targetRadius * 2f * playArea.rect.width / ForgeMinigame.AreaSize.x;
            hitEffects.Play(effect, ToPlayAreaPoint(circle.Position), circleDiameter / effectReferenceDiameter);
        }

        /// <summary>미니게임 공간(왼쪽 아래 기준) 좌표 → PlayArea rect 좌표(피벗 기준 로컬).</summary>
        private Vector2 ToPlayAreaPoint(Vector2 gamePosition)
        {
            Rect rect = playArea.rect;
            float scale = rect.width / ForgeMinigame.AreaSize.x;
            return new Vector2(rect.xMin + gamePosition.x * scale, rect.yMin + gamePosition.y * scale);
        }

        private GameObject Root => panelRoot != null ? panelRoot : gameObject;

        private void Update()
        {
            AdvanceHammer(Time.deltaTime);

            if (game == null || circleTemplate == null) return;

            // 원 목록은 Begin 이후에 채워지므로 부족한 만큼 만든다.
            while (circleViews.Count < game.Circles.Count) circleViews.Add(CreateCircleView());

            float scale = playArea.rect.width / ForgeMinigame.AreaSize.x;
            for (int i = 0; i < game.Circles.Count; i++)
            {
                Draw(circleViews[i], game.Circles[i], scale);
            }
        }

        /// <summary>망치를 원 위치(미니게임 좌표)로 옮겨 내리찍기를 처음부터 다시 시작한다. 망치가 없으면 아무것도 하지 않는다.</summary>
        private void StartHammer(Vector2 gamePosition)
        {
            if (hammer == null) return;

            hammerPoint = ToPlayAreaPoint(gamePosition) - playArea.rect.center;
            hammerElapsed = 0f;
            hammer.gameObject.SetActive(true);
            hammer.SetAsLastSibling(); // 원 위에 그린다.
            ApplyHammerPose(0f);
        }

        private void AdvanceHammer(float deltaTime)
        {
            if (hammerElapsed < 0f) return;

            hammerElapsed += deltaTime;
            float hammerTotal = hammerSwingTime + hammerHoldTime + hammerFadeTime;
            float ingotTotal = hammerSwingTime + ingotPressTime;
            if (hammerElapsed >= Mathf.Max(hammerTotal, ingotTotal))
            {
                ResetHammer();
                return;
            }
            ApplyHammerPose(hammerElapsed);
        }

        // 경과 시간 t에 맞춰 망치(들어올림 → 내리침 → 정지 → 사라짐)와 주괴(눌림 → 복원)를 놓는다.
        private void ApplyHammerPose(float t)
        {
            if (hammer != null)
            {
                float swing = Mathf.Max(hammerSwingTime, 0.001f);
                float raised = 1f - Mathf.Clamp01(t / swing);
                raised *= raised; // 내려칠수록 빨라지게 한다.

                hammer.anchoredPosition = hammerPoint + hammerRaisedOffset * raised;
                hammer.localRotation = Quaternion.Euler(0f, 0f, hammerRaisedAngle * raised);

                float fadeStart = hammerSwingTime + hammerHoldTime;
                float alpha = t <= fadeStart ? 1f : 1f - Mathf.Clamp01((t - fadeStart) / Mathf.Max(hammerFadeTime, 0.001f));
                Image image = hammer.GetComponent<Image>();
                if (image != null)
                {
                    Color color = image.color;
                    color.a = alpha;
                    image.color = color;
                }
            }

            if (ingot != null)
            {
                float since = t - hammerSwingTime; // 내리친 순간부터
                float press = since < 0f ? 1f : Mathf.Lerp(ingotPressScale, 1f, Mathf.Clamp01(since / Mathf.Max(ingotPressTime, 0.001f)));
                ingot.localScale = ingotBaseScale * press;
            }
        }

        /// <summary>망치를 숨기고 주괴를 원래 크기로 돌린다. (끝났을 때, Show/Hide 때)</summary>
        private void ResetHammer()
        {
            hammerElapsed = -1f;
            if (hammer != null)
            {
                Image image = hammer.GetComponent<Image>();
                if (image != null)
                {
                    Color color = image.color;
                    color.a = 1f;
                    image.color = color;
                }
                hammer.gameObject.SetActive(false);
            }
            if (ingot != null) ingot.localScale = ingotBaseScale;
        }

        private void Draw(CircleView view, ForgeMinigame.Circle circle, float scale)
        {
            bool judged = circle.State != ForgeMinigame.CircleState.Pending;
            bool started = game.Time >= circle.SpawnTime;
            bool lingering = judged && game.Time - circle.JudgedTime < resultLingerTime;
            bool visible = started && (!judged || lingering);

            view.Root.gameObject.SetActive(visible);
            if (!visible) return;

            view.Root.anchoredPosition = ToPlayAreaPoint(circle.Position) - playArea.rect.center;

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
