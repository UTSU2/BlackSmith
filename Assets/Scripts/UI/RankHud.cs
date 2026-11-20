using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Data;
using BlackSmith.Items;
using BlackSmith.Rank;

namespace BlackSmith.UI
{
    /// <summary>
    /// 대장장이 등급과 다음 등급까지의 진행도를 보여 주는 HUD 글자. 튜토리얼 중에는 숨긴다(견습은 튜토리얼 클리어가 조건).
    /// 필요 개수가 미정(-1)이면 '-'로 보인다.
    /// 글자 크기는 그대로 두고 명패(이 오브젝트의 크기)를 글자 길이에 맞춰 늘린다. 튜토리얼 중에는 명패도 함께 숨긴다.
    /// </summary>
    public class RankHud : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private RankController controller;
        [Tooltip("글자 뒤의 명패 오브젝트. 글자를 숨길 때 함께 숨긴다. 비워 두면 숨기지 않는다.")]
        [SerializeField] private GameObject plaque;
        [Tooltip("명패의 최소 크기.")]
        [SerializeField] private Vector2 minSize = new Vector2(380f, 120f);
        [Tooltip("글자 둘레 여백의 합(가로, 세로). 글자 칸의 안쪽 여백(RankHud/Label 오프셋)과 맞춘다.")]
        [SerializeField] private Vector2 padding = new Vector2(70f, 30f);

        private BlacksmithRankSettings settings;
        private bool lastTutorialPending;

        private void Awake()
        {
            UIFonts.Apply(label);
        }

        private void OnEnable()
        {
            if (controller != null) controller.Changed += Refresh;
        }

        private void Start()
        {
            settings = BlacksmithRankSettings.Load();
            Refresh();
        }

        private void OnDisable()
        {
            if (controller != null) controller.Changed -= Refresh;
        }

        private void Update()
        {
            // 튜토리얼이 끝나는 순간(판매 직후)에는 별도 이벤트가 없어 상태 변화만 살핀다.
            PlayerData player = GameSession.Current;
            bool pending = player != null && player.IsTutorialPending;
            if (pending != lastTutorialPending) Refresh();
        }

        private void Refresh()
        {
            if (label == null) return;

            PlayerData player = GameSession.Current;
            lastTutorialPending = player != null && player.IsTutorialPending;
            bool visible = player != null && !lastTutorialPending;
            label.enabled = visible;
            if (plaque != null) plaque.SetActive(visible);
            if (!visible) return;

            string rankName = settings != null ? settings.GetName(player.Rank) : player.Rank.ToString();
            string line2 = "최고 등급";
            if (controller != null)
            {
                RankProgress progress = controller.GetProgress();
                string progressLine = progress.HasNext
                    ? $"도감 {progress.Codex}/{Format(progress.RequiredCodex)} · master {progress.Masters}/{Format(progress.RequiredMasters)}"
                    : string.Empty;
                RankQuestSettings.QuestEntry quest;
                controller.TryGetActiveQuest(out quest);
                line2 = DescribeQuest(controller.GetQuestState(), quest, progressLine);
            }
            label.text = $"대장장이 등급: {rankName}\n{line2}";
            FitToText();
        }

        /// <summary>현재 글자 길이에 맞게 명패 크기를 정한다(최소 크기 이상). 글자는 한 줄이 길어도 줄바꿈하지 않는다.</summary>
        public void FitToText()
        {
            if (label == null) return;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform rect = (RectTransform)transform;
            float width = Mathf.Max(minSize.x, label.preferredWidth + padding.x);
            float height = Mathf.Max(minSize.y, label.preferredHeight + padding.y);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        /// <summary>
        /// HUD 둘째 줄. 조건 미달이면 진행도, 퀘스트 진행 중이면 퀘스트 설명, 조건은 채웠는데 퀘스트가 비어 있으면 준비 중, 최고 등급이면 최고 등급.
        /// 설명이 비어 있으면 레시피 id와 품질로 임시 표기한다. 문구는 임시이며 미정이다.
        /// </summary>
        public static string DescribeQuest(RankQuestState state, RankQuestSettings.QuestEntry quest, string progressLine)
        {
            switch (state)
            {
                case RankQuestState.Locked: return progressLine;
                case RankQuestState.Preparing: return "승급 퀘스트 준비 중";
                case RankQuestState.Active:
                    if (quest == null) return "승급 퀘스트 준비 중";
                    return !string.IsNullOrEmpty(quest.description)
                        ? $"승급 퀘스트: {quest.description}"
                        : $"승급 퀘스트: {quest.recipeId} {ItemQualityInfo.DisplayName(quest.minQuality)} 이상";
                default: return "최고 등급";
            }
        }

        private static string Format(int required) => required >= 0 ? required.ToString() : "-";
    }
}
