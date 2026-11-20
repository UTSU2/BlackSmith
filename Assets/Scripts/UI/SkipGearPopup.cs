using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Crafting;
using BlackSmith.Rank;

namespace BlackSmith.UI
{
    /// <summary>
    /// 스킵 장비 소품(자동 가열·서리 수조)을 눌렀을 때 뜨는 팝업. 켜기/끄기와 강화는 안에 둔 SkipGearRow가 처리하고
    /// 이 클래스는 어느 장비인지 정해 열고 닫기만 한다. 제작 중에는 열지 않는다.
    /// 이 오브젝트는 씬에서 켜 둔 채로 두고(소품 이벤트를 받아야 한다), 화면 전체를 덮는 반투명 막 + 상자인 content를 켜고 끈다.
    /// </summary>
    public class SkipGearPopup : MonoBehaviour
    {
        [Tooltip("팝업 전체(반투명 막 + 상자). 씬에서는 꺼 두고, 열 때 켠다.")]
        [SerializeField] private GameObject content;
        [SerializeField] private SkipGearProp[] props;
        [Tooltip("제작 중에는 열지 않는다. 비워 두면 항상 연다.")]
        [SerializeField] private CraftingController crafting;
        [SerializeField] private SkipGearRow row;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Button closeButton;

        public bool IsOpen => content != null && content.activeSelf;

        public static string GetTitle(SkipGearKind kind)
        {
            return kind == SkipGearKind.Heat ? "자동 가열" : "서리 수조";
        }

        private void Awake()
        {
            UIFonts.ApplyAll(this);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void Start()
        {
            if (content != null) content.SetActive(false);
            if (props == null) return;
            foreach (SkipGearProp prop in props)
            {
                if (prop != null) prop.Clicked += Open;
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (props == null) return;
            foreach (SkipGearProp prop in props)
            {
                if (prop != null) prop.Clicked -= Open;
            }
        }

        public void Open(SkipGearKind kind)
        {
            if (crafting != null && crafting.IsCrafting) return;
            if (titleLabel != null) titleLabel.text = GetTitle(kind);
            if (row != null) row.SetKind(kind);
            if (content != null) content.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (content != null) content.SetActive(false);
        }
    }
}
