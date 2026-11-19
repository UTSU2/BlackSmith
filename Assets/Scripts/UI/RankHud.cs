using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Data;
using BlackSmith.Rank;

namespace BlackSmith.UI
{
    /// <summary>
    /// 대장장이 등급과 다음 등급까지의 진행도를 보여 주는 HUD 글자. 튜토리얼 중에는 숨긴다(견습은 튜토리얼 클리어가 조건).
    /// 필요 개수가 미정(-1)이면 '-'로 보인다.
    /// </summary>
    public class RankHud : MonoBehaviour
    {
        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        [SerializeField] private Text label;
        [SerializeField] private RankController controller;

        private BlacksmithRankSettings settings;
        private bool lastTutorialPending;

        private void Awake()
        {
            if (label != null) label.font = Font.CreateDynamicFontFromOSFont(KoreanFonts, label.fontSize);
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
            if (!visible) return;

            string rankName = settings != null ? settings.GetName(player.Rank) : player.Rank.ToString();
            string line2 = "최고 등급";
            if (controller != null)
            {
                RankProgress progress = controller.GetProgress();
                if (progress.HasNext)
                {
                    line2 = $"도감 {progress.Codex}/{Format(progress.RequiredCodex)} · master {progress.Masters}/{Format(progress.RequiredMasters)}";
                }
            }
            label.text = $"대장장이 등급: {rankName}\n{line2}";
        }

        private static string Format(int required) => required >= 0 ? required.ToString() : "-";
    }
}
