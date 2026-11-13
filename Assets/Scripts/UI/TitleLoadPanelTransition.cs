using System;
using System.Collections;
using UnityEngine;

namespace BlackSmith.UI
{
    /// <summary>
    /// 패널이 위에서 화면 중앙으로 내려오는 연출. Hide는 그 반대.
    /// 타이틀 씬에서는 메뉴 버튼들이 오른쪽으로 밀려 나가고 제목이 위로 올라가며(menuButtons, title),
    /// Game 씬에서는 둘 다 비워 두고 패널만 움직인다. (이름은 처음 쓰인 곳을 따른 것이다)
    /// 패널 하나를 움직이므로 패널마다 이 컴포넌트를 하나씩 둔다. (타이틀: 로드·설정, Game: 상점·저장/불러오기·설정)
    /// 패널이 비활성이 되어도 코루틴이 유지되도록 항상 활성인 오브젝트(MainMenuCanvas 또는 그 자식)에 붙인다.
    /// </summary>
    public class TitleLoadPanelTransition : SaveLoadPanelTransition
    {
        [Header("대상")]
        [Tooltip("위에서 내려올 패널 루트. 뒤로 가기 버튼도 이 안에 있어야 함께 내려온다.")]
        [SerializeField] private RectTransform panel;
        [Tooltip("오른쪽으로 밀어낼 메뉴 버튼들.")]
        [SerializeField] private RectTransform[] menuButtons;
        [Tooltip("연출 중 입력을 막는 데 쓴다. 패널에 붙은 CanvasGroup.")]
        [SerializeField] private CanvasGroup panelGroup;
        [Tooltip("패널이 열리면 위로 올라가 사라질 제목. 비워 두면 움직이지 않는다.")]
        [SerializeField] private RectTransform title;

        [Header("연출")]
        [SerializeField, Min(0.05f)] private float duration = 0.5f;
        [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("메뉴 버튼이 오른쪽으로 이동하는 거리(px, 캔버스 기준).")]
        [SerializeField] private float menuSlideDistance = 600f;
        [Tooltip("메뉴 버튼 사이의 시작 시간 차이(초).")]
        [SerializeField, Min(0f)] private float menuStagger = 0.05f;
        [Tooltip("제목이 위로 올라가는 거리(px, 캔버스 기준). 제목이 화면 밖으로 완전히 나가야 한다.")]
        [SerializeField] private float titleSlideDistance = 400f;

        private Vector2[] menuHomePositions;
        private Vector2 titleHomePosition;
        private Vector2 panelShownPosition;
        private Coroutine running;
        private bool isShown;

        public override bool IsPlaying => running != null;

        private void Awake()
        {
            CacheHomePositions();
            // 시작 상태: 패널은 숨김.
            if (panel != null) panel.gameObject.SetActive(false);
        }

        private void CacheHomePositions()
        {
            menuHomePositions = new Vector2[menuButtons.Length];
            for (int i = 0; i < menuButtons.Length; i++)
            {
                if (menuButtons[i] != null) menuHomePositions[i] = menuButtons[i].anchoredPosition;
            }
            if (panel != null) panelShownPosition = panel.anchoredPosition;
            if (title != null) titleHomePosition = title.anchoredPosition;
        }

        public override void Show(Action onDone = null)
        {
            if (IsPlaying || isShown || panel == null) return;
            isShown = true;
            // 활성화는 즉시 한다. 호출한 쪽이 곧바로 슬롯을 갱신해야 하기 때문.
            panel.gameObject.SetActive(true);
            running = StartCoroutine(Play(true, onDone));
        }

        public override void Hide(Action onDone = null)
        {
            if (IsPlaying || !isShown || panel == null) return;
            isShown = false;
            running = StartCoroutine(Play(false, onDone));
        }

        private IEnumerator Play(bool show, Action onDone)
        {
            // 패널이 화면 위로 완전히 벗어나는 거리. 화면 전체 크기의 패널은 패널 높이와 같고,
            // 중앙에 놓인 작은 패널은 (화면 높이 + 패널 높이) / 2가 필요하다.
            RectTransform parent = panel.parent as RectTransform;
            float parentHeight = parent != null && parent.rect.height > 0f ? parent.rect.height : 1080f;
            float panelHeight = panel.rect.height > 0f ? panel.rect.height : 1080f;
            Vector2 panelHidden = panelShownPosition + new Vector2(0f, (parentHeight + panelHeight) * 0.5f);

            if (panelGroup != null) panelGroup.blocksRaycasts = false;

            int count = menuButtons.Length;
            float totalTime = duration + menuStagger * Mathf.Max(0, count - 1);
            float elapsed = 0f;

            while (elapsed < totalTime)
            {
                elapsed += Time.unscaledDeltaTime;

                float panelT = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                float panelBlend = show ? panelT : 1f - panelT;
                panel.anchoredPosition = Vector2.LerpUnclamped(panelHidden, panelShownPosition, panelBlend);
                // 제목은 패널이 내려오는 동안 위로 올라가고, 닫으면 내려온다.
                if (title != null) title.anchoredPosition = titleHomePosition + new Vector2(0f, titleSlideDistance * (show ? panelT : 1f - panelT));

                for (int i = 0; i < count; i++)
                {
                    if (menuButtons[i] == null) continue;
                    // 메뉴가 나가는 순서(show)와 들어오는 순서(hide) 모두 위쪽 버튼부터.
                    float t = curve.Evaluate(Mathf.Clamp01((elapsed - menuStagger * i) / duration));
                    float blend = show ? t : 1f - t;
                    Vector2 away = menuHomePositions[i] + new Vector2(menuSlideDistance, 0f);
                    menuButtons[i].anchoredPosition = Vector2.LerpUnclamped(menuHomePositions[i], away, blend);
                }
                yield return null;
            }

            // 최종 위치를 정확히 맞춘다.
            panel.anchoredPosition = show ? panelShownPosition : panelHidden;
            if (title != null) title.anchoredPosition = show ? titleHomePosition + new Vector2(0f, titleSlideDistance) : titleHomePosition;
            for (int i = 0; i < count; i++)
            {
                if (menuButtons[i] == null) continue;
                menuButtons[i].anchoredPosition = show
                    ? menuHomePositions[i] + new Vector2(menuSlideDistance, 0f)
                    : menuHomePositions[i];
            }

            if (!show) panel.gameObject.SetActive(false);
            if (panelGroup != null) panelGroup.blocksRaycasts = true;

            running = null;
            onDone?.Invoke();
        }
    }
}
