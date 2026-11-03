using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 슬롯 페이지 넘기기. 페이지들이 나란히 놓인 pagesRoot를 좌우로 슬라이드한다.
/// 더 갈 수 없는 방향의 화살표 버튼은 비활성화된다. 이동 중에는 두 화살표 모두 비활성.
/// pagesRoot는 pivot/anchor가 왼쪽(x=0)이고, 뷰포트(RectMask2D) 안에서 첫 페이지가 보이는 위치가 x=0이어야 한다.
/// </summary>
public class SaveSlotPager : MonoBehaviour
{
    [SerializeField] private RectTransform pagesRoot;
    [Tooltip("페이지 한 장의 폭. 0이면 pagesRoot 폭 / 페이지 수로 계산한다.")]
    [SerializeField] private float pageWidth;
    [SerializeField, Min(1)] private int pageCount = 2;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Header("연출")]
    [SerializeField, Min(0.05f)] private float duration = 0.3f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private int page;
    private Coroutine sliding;

    public int Page => page;

    private void Awake()
    {
        if (previousButton != null) previousButton.onClick.AddListener(PreviousPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
    }

    private void OnDisable()
    {
        // 비활성화되면 코루틴이 멈추므로 진행 중이던 이동은 도착 상태로 확정한다.
        sliding = null;
        SnapToPage();
    }

    public void NextPage() => GoTo(page + 1);

    public void PreviousPage() => GoTo(page - 1);

    /// <summary>애니메이션 없이 첫 페이지로 돌아간다. 패널을 열 때 호출.</summary>
    public void ResetToFirstPage()
    {
        if (sliding != null) { StopCoroutine(sliding); sliding = null; }
        page = 0;
        SnapToPage();
    }

    private void GoTo(int target)
    {
        if (sliding != null) return;
        if (target < 0 || target >= pageCount || target == page) return;
        if (!gameObject.activeInHierarchy) return;
        sliding = StartCoroutine(Slide(target));
    }

    private IEnumerator Slide(int target)
    {
        UpdateArrows(false, false);

        float from = pagesRoot.anchoredPosition.x;
        float to = -target * GetPageWidth();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
            SetX(Mathf.LerpUnclamped(from, to, t));
            yield return null;
        }

        page = target;
        sliding = null;
        SnapToPage();
    }

    private void SnapToPage()
    {
        if (pagesRoot != null) SetX(-page * GetPageWidth());
        UpdateArrows(page > 0, page < pageCount - 1);
    }

    private void UpdateArrows(bool canGoPrevious, bool canGoNext)
    {
        if (previousButton != null) previousButton.interactable = canGoPrevious;
        if (nextButton != null) nextButton.interactable = canGoNext;
    }

    private void SetX(float x)
    {
        Vector2 pos = pagesRoot.anchoredPosition;
        pos.x = x;
        pagesRoot.anchoredPosition = pos;
    }

    private float GetPageWidth()
    {
        if (pageWidth > 0f) return pageWidth;
        return pagesRoot != null ? pagesRoot.rect.width / pageCount : 0f;
    }
}
