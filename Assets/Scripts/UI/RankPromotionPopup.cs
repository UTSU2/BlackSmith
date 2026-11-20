using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Rank;

namespace BlackSmith.UI
{
    /// <summary>
    /// 승급 팝업. 승급하면 `대장장이 등급: 초급 → 숙련`과 혜택 문구를 보여 주고 확인 버튼으로 닫는다.
    /// 제작 결과 팝업이 떠 있는 동안에는 기다렸다가 그것이 닫힌 뒤에 보여 준다. 승급이 겹치면 순서대로 하나씩 보여 준다.
    /// 오브젝트는 씬에서 활성으로 두고(Update가 돌아야 한다) 자체 Canvas(순서 22)를 끄고 켜서 보인다. 배치·디자인은 임시값이며 미정이다.
    /// </summary>
    public class RankPromotionPopup : MonoBehaviour
    {
        [SerializeField] private RankController controller;
        [Tooltip("결과 팝업. 이것이 열려 있는 동안은 기다린다. 비워 두면 바로 보여 준다.")]
        [SerializeField] private CraftResultPopup resultPopup;
        [SerializeField] private Text titleLabel;
        [Tooltip("혜택 문구. 비어 있으면 숨긴다.")]
        [SerializeField] private Text benefitLabel;
        [SerializeField] private Button confirmButton;

        private readonly Queue<KeyValuePair<BlacksmithRank, BlacksmithRank>> pending = new Queue<KeyValuePair<BlacksmithRank, BlacksmithRank>>();
        private BlacksmithRankSettings settings;
        private Canvas canvas;

        public bool IsOpen { get; private set; }

        /// <summary>승급 안내 첫 줄.</summary>
        public static string BuildTitle(string fromName, string toName)
        {
            return $"대장장이 등급: {fromName} → {toName}";
        }

        private void Awake()
        {
            // 글꼴은 크기 기준(UIFontSettings)으로 시작할 때 적용한다.
            UIFonts.ApplyAll(this);
            canvas = GetComponent<Canvas>();
        }

        private void Start()
        {
            settings = BlacksmithRankSettings.Load();
            if (controller != null) controller.Promoted += Enqueue;
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (controller != null) controller.Promoted -= Enqueue;
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
        }

        // 승급 이벤트는 제작 결과 팝업이 열리기 전에 올 수 있으므로, 결과 팝업이 있으면 한 프레임 뒤(Update)에 보여 줄지 정한다.
        public void Enqueue(BlacksmithRank from, BlacksmithRank to)
        {
            pending.Enqueue(new KeyValuePair<BlacksmithRank, BlacksmithRank>(from, to));
            if (resultPopup == null) TryShowNext();
        }

        private void Update()
        {
            if (pending.Count > 0) TryShowNext();
        }

        private void TryShowNext()
        {
            if (IsOpen || pending.Count == 0) return;
            if (resultPopup != null && resultPopup.IsOpen) return;

            KeyValuePair<BlacksmithRank, BlacksmithRank> next = pending.Dequeue();
            string fromName = settings != null ? settings.GetName(next.Key) : next.Key.ToString();
            string toName = settings != null ? settings.GetName(next.Value) : next.Value.ToString();
            if (titleLabel != null) titleLabel.text = BuildTitle(fromName, toName);

            string benefit = settings != null ? settings.GetBenefit(next.Value) : string.Empty;
            if (benefitLabel != null)
            {
                benefitLabel.gameObject.SetActive(!string.IsNullOrEmpty(benefit));
                benefitLabel.text = benefit;
            }

            SetVisible(true);
        }

        private void OnConfirmClicked()
        {
            SetVisible(false);
            TryShowNext();
        }

        private void SetVisible(bool visible)
        {
            IsOpen = visible;
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = visible;
            if (visible) transform.SetAsLastSibling();
        }
    }
}
