using System;
using UnityEngine;
using UnityEngine.EventSystems;
using BlackSmith.Data;

namespace BlackSmith.Rank
{
    /// <summary>
    /// 대장간에 놓인 스킵 장비 소품(가열 = 자동 가열, 열처리 = 서리 수조). 해당 장비를 얻기 전에는 보이지도 눌리지도 않고,
    /// 얻은 뒤에 나타나 클릭할 수 있다. 무엇을 할지(팝업)는 이 클래스가 정하지 않고 Clicked를 구독한 쪽이 정한다.
    /// Collider2D가 있는 오브젝트에 붙이고 카메라에는 Physics2DRaycaster가 필요하다(모루 AnvilInteractable과 같은 방식).
    /// 소유 여부는 매 프레임 살펴서(Update) 이벤트 연결 없이 세이브 불러오기·승급을 모두 따라간다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SkipGearProp : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color HoverTint = new Color(1.18f, 1.18f, 1.18f, 1f);

        [SerializeField] private SkipGearKind kind;
        [Tooltip("보이고 숨길 그림. 비워 두면 자식의 SpriteRenderer 전부.")]
        [SerializeField] private SpriteRenderer[] renderers;

        private Collider2D hitArea;
        private bool shown;
        private bool hasShownState;

        public SkipGearKind Kind => kind;

        /// <summary>소품이 눌렸을 때(보이는 상태에서 왼쪽 클릭).</summary>
        public event Action<SkipGearKind> Clicked;

        private void Awake()
        {
            hitArea = GetComponent<Collider2D>();
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void Update()
        {
            PlayerData player = GameSession.Current;
            bool owned = player != null && player.GetSkipGear(kind) != null;
            if (!hasShownState || owned != shown) Apply(owned);
        }

        private void Apply(bool owned)
        {
            hasShownState = true;
            shown = owned;
            foreach (SpriteRenderer sprite in renderers)
            {
                if (sprite != null) sprite.enabled = owned;
            }
            if (hitArea != null) hitArea.enabled = owned;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!shown || eventData.button != PointerEventData.InputButton.Left) return;
            Clicked?.Invoke(kind);
        }

        public void OnPointerEnter(PointerEventData eventData) => SetTint(shown ? HoverTint : Color.white);

        public void OnPointerExit(PointerEventData eventData) => SetTint(Color.white);

        private void OnDisable() => SetTint(Color.white);

        private void SetTint(Color color)
        {
            if (renderers == null) return;
            foreach (SpriteRenderer sprite in renderers)
            {
                if (sprite != null) sprite.color = color;
            }
        }
    }
}
