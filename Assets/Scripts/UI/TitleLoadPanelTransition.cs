using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 타이틀 씬 연출: 메뉴 버튼들이 오른쪽으로 밀려 나가고,
/// 패널(뒤로 가기 버튼 포함)이 위에서 화면 중앙으로 내려온다. Hide는 그 반대.
/// 패널이 비활성이 되어도 코루틴이 유지되도록 항상 활성인 오브젝트(MainMenuCanvas)에 붙인다.
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

    [Header("연출")]
    [SerializeField, Min(0.05f)] private float duration = 0.5f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("메뉴 버튼이 오른쪽으로 이동하는 거리(px, 캔버스 기준).")]
    [SerializeField] private float menuSlideDistance = 600f;
    [Tooltip("메뉴 버튼 사이의 시작 시간 차이(초).")]
    [SerializeField, Min(0f)] private float menuStagger = 0.05f;

    private Vector2[] menuHomePositions;
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
        float panelHeight = panel.rect.height > 0f ? panel.rect.height : 1080f;
        Vector2 panelHidden = panelShownPosition + new Vector2(0f, panelHeight);

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
