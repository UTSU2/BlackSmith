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
        [Tooltip("흠집 하나의 모양. 비활성 상태로 둔다. 자식 없이 Image 하나면 된다.")]
        [SerializeField] private RectTransform flawTemplate;
        [Tooltip("흠집이 그려지는 영역. 비워 두면 이 오브젝트를 쓴다.")]
        [SerializeField] private RectTransform playArea;
        [Tooltip("남은 시간 표시. 비워 두면 표시하지 않는다.")]
        [SerializeField] private RectTransform timeFill;
        [Tooltip("Show/Hide로 켜고 끌 오브젝트(배경 포함 패널). 비워 두면 이 오브젝트를 쓴다.")]
        [SerializeField] private GameObject panelRoot;

        [Header("연출 (비워 두면 생략)")]
        [Tooltip("흠집 모양 변형. 흠집 번호 i에 flawSprites[i % 개수]를 쓴다. 비워 두면 flawTemplate의 스프라이트를 그대로 쓴다.")]
        [SerializeField] private Sprite[] flawSprites;
        [Tooltip("누르고 있는 동안 포인터를 따라다니는 숫돌. 비활성 상태로 두고, playArea의 자식이며 앵커는 가운데로 한다. 접촉면이 포인터에 오도록 피벗을 아래 중앙에 둔다.")]
        [SerializeField] private RectTransform whetstone;

        [Header("색")]
        [SerializeField] private Color flawColor = new Color(0.55f, 0.25f, 0.1f, 1f);
        [Tooltip("지금 문지르고 있는 흠집 색.")]
        [SerializeField] private Color activeFlawColor = new Color(0.9f, 0.45f, 0.15f, 1f);
        [Tooltip("남은 양이 적어도 이 비율 아래로는 흐려지지 않는다. (0이면 거의 사라질 때까지 흐려진다)")]
        [Range(0f, 1f)]
        [SerializeField] private float minVisibleAlpha = 0.25f;

        [Header("적중 이펙트 (비워 두면 이펙트 없이 진행)")]
        [Tooltip("흠집 하나를 다 지울 때마다 그 자리에 띄울 반짝임.")]
        [SerializeField] private HitEffectSpawner clearEffects;
        [Tooltip("이펙트 프리팹이 맞춰 만든 흠집 지름(px). 실제 흠집 지름 ÷ 이 값으로 키우거나 줄인다.")]
        [SerializeField] private float effectReferenceDiameter = 180f;

        private readonly List<Image> flawViews = new List<Image>();
        private PolishMinigame game;

        private GameObject Root => panelRoot != null ? panelRoot : gameObject;

        private void Awake()
        {
            if (playArea == null) playArea = (RectTransform)transform;
            if (flawTemplate != null) flawTemplate.gameObject.SetActive(false);
            if (whetstone != null)
            {
                // 입력은 PlayArea가 받는다. 숫돌 이미지가 가로채지 않게 한다.
                Image whetstoneImage = whetstone.GetComponent<Image>();
                if (whetstoneImage != null) whetstoneImage.raycastTarget = false;
            }
            HideWhetstone();

            UIFonts.ApplyAll(this);
        }

        /// <summary>미니게임을 보여주기 시작한다. 게임 시작(Begin)은 호출한 쪽이 한다.</summary>
        public void Show(PolishMinigame polish)
        {
            Unsubscribe();
            ClearFlaws();
            HideWhetstone();
            if (clearEffects != null) clearEffects.Clear();
            game = polish;
            if (game != null) game.FlawClearedAt += OnFlawCleared;
            Root.SetActive(true);
            // Awake가 아직 안 불렸을 수 있으므로(처음 활성화) 활성화한 뒤에 확인한다.
            if (playArea == null) playArea = (RectTransform)transform;
        }

        public void Hide()
        {
            Unsubscribe();
            if (game != null) game.PointerUp();
            game = null;
            ClearFlaws();
            HideWhetstone();
            if (clearEffects != null) clearEffects.Clear();
            Root.SetActive(false);
        }

        private void Unsubscribe()
        {
            if (game != null) game.FlawClearedAt -= OnFlawCleared;
        }

        private void OnFlawCleared(PolishMinigame.Flaw flaw)
        {
            if (clearEffects == null || game == null || effectReferenceDiameter <= 0f) return;
            float flawDiameter = game.Settings.flawRadius * 2f * playArea.rect.width / PolishMinigame.AreaSize.x;
            clearEffects.Play(ToPlayAreaPoint(flaw.Position), flawDiameter / effectReferenceDiameter);
        }

        /// <summary>미니게임 공간(왼쪽 아래 기준) 좌표 → PlayArea rect 좌표(피벗 기준 로컬).</summary>
        private Vector2 ToPlayAreaPoint(Vector2 gamePosition)
        {
            Rect rect = playArea.rect;
            float scale = rect.width / PolishMinigame.AreaSize.x;
            return new Vector2(rect.xMin + gamePosition.x * scale, rect.yMin + gamePosition.y * scale);
        }

        private void Update()
        {
            if (game == null || flawTemplate == null) return;

            // 흠집 목록은 Begin 이후에 채워지므로 부족한 만큼 만든다.
            while (flawViews.Count < game.Flaws.Count)
            {
                Image created = CreateFlawView();
                // 흠집 번호로 모양을 고른다. (로직·난수와 무관하게 같은 번호는 늘 같은 모양)
                if (flawSprites != null && flawSprites.Length > 0) created.sprite = flawSprites[flawViews.Count % flawSprites.Length];
                flawViews.Add(created);
            }

            // 끝난 뒤(전부 지웠거나 시간 초과)에는 숫돌을 치운다.
            if (game.IsFinished) HideWhetstone();

            float scale = playArea.rect.width / PolishMinigame.AreaSize.x;
            Rect rect = playArea.rect;
            float diameter = game.Settings.flawRadius * 2f * scale;

            for (int i = 0; i < game.Flaws.Count; i++)
            {
                PolishMinigame.Flaw flaw = game.Flaws[i];
                Image view = flawViews[i];
                view.gameObject.SetActive(!flaw.IsCleared);
                if (flaw.IsCleared) continue;

                view.rectTransform.anchoredPosition = ToPlayAreaPoint(flaw.Position) - rect.center;
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
            MoveWhetstone(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (game == null || eventData.button != PointerEventData.InputButton.Left) return;
            if (TryToGamePosition(eventData, out Vector2 position)) game.PointerMove(position);
            MoveWhetstone(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (game == null || eventData.button != PointerEventData.InputButton.Left) return;
            game.PointerUp();
            HideWhetstone();
        }

        /// <summary>숫돌을 포인터 위치로 옮기고 보이게 한다. 끝난 뒤나 숫돌이 없으면 아무것도 하지 않는다.</summary>
        private void MoveWhetstone(PointerEventData eventData)
        {
            if (whetstone == null || game == null || game.IsFinished) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    playArea, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                return;
            }

            whetstone.anchoredPosition = local - playArea.rect.center;
            if (!whetstone.gameObject.activeSelf)
            {
                whetstone.gameObject.SetActive(true);
                whetstone.SetAsLastSibling(); // 흠집 위에 그린다.
            }
        }

        private void HideWhetstone()
        {
            if (whetstone != null && whetstone.gameObject.activeSelf) whetstone.gameObject.SetActive(false);
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
