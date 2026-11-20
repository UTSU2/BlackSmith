using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Data;
using BlackSmith.Rank;

namespace BlackSmith.UI
{
    /// <summary>
    /// 스킵 장비 한 줄: 상태 글자, 켜기/끄기 버튼, 강화 버튼. 장비가 없으면 줄 전체를 숨긴다.
    /// 규칙은 SkipGearService/SkipGearRules가 갖고 이 클래스는 표시와 입력 전달만 한다. 켜질 때마다 새로 그린다.
    /// 스킵 장비 팝업(SkipGearPopup) 안에서 쓰며, 어느 장비를 보여 줄지는 SetKind로 정한다.
    /// </summary>
    public class SkipGearRow : MonoBehaviour
    {
        private static readonly Color DimColor = new Color(1f, 1f, 1f, 0.5f);

        [SerializeField] private SkipGearKind kind;
        [Tooltip("줄 안의 내용물(글자와 버튼들). 장비가 없으면 이것을 끈다. 이 컴포넌트가 붙은 오브젝트는 끄지 않는다.")]
        [SerializeField] private GameObject content;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Text toggleLabel;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Text upgradeLabel;

        private SkipGearRules rules;

        private void Awake()
        {
            SkipGearSettings settings = SkipGearSettings.Load();
            if (settings != null) rules = new SkipGearRules(settings);
            if (toggleButton != null) toggleButton.onClick.AddListener(OnToggle);
            if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgrade);
        }

        private void OnDestroy()
        {
            if (toggleButton != null) toggleButton.onClick.RemoveListener(OnToggle);
            if (upgradeButton != null) upgradeButton.onClick.RemoveListener(OnUpgrade);
        }

        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>보여 줄 장비를 바꾼다. 켜져 있으면 바로 다시 그린다.</summary>
        public void SetKind(SkipGearKind newKind)
        {
            kind = newKind;
            if (isActiveAndEnabled) Refresh();
        }

        private void OnToggle()
        {
            PlayerData player = GameSession.Current;
            SkipGearEntry entry = player != null ? player.GetSkipGear(kind) : null;
            if (entry != null) player.SetSkipGearEnabled(kind, !entry.Enabled);
            Refresh();
        }

        private void OnUpgrade()
        {
            SkipGearService.TryUpgrade(GameSession.Current, kind, rules);
            Refresh();
        }

        private void Refresh()
        {
            PlayerData player = GameSession.Current;
            SkipGearEntry entry = player != null ? player.GetSkipGear(kind) : null;
            bool owned = entry != null && rules != null;
            if (content != null) content.SetActive(owned);
            if (!owned) return;

            string name = SkipGearPopup.GetTitle(kind);
            if (statusLabel != null) statusLabel.text = $"{name} Lv.{entry.Level} ({rules.GetScore(entry.Level)}점)";
            if (toggleLabel != null) toggleLabel.text = entry.Enabled ? "켜짐" : "꺼짐";

            int cost = rules.GetUpgradeCost(entry.Level);
            bool canUpgrade = cost >= 0 && player.CanAfford(cost);
            if (upgradeLabel != null)
            {
                upgradeLabel.text = entry.Level >= SkipGearRules.MaxLevel ? "최대" : (cost < 0 ? "강화 불가" : $"강화 {cost}G");
            }
            // interactable을 끄면 스프라이트 전환과 글자 위치가 어긋나므로 색만 어둡게 한다. (CraftingPanel 시작 버튼과 같은 방식)
            if (upgradeButton != null && upgradeButton.image != null) upgradeButton.image.color = canUpgrade ? Color.white : DimColor;
        }
    }
}
