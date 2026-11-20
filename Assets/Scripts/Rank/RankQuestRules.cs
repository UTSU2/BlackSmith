using BlackSmith.Items;

namespace BlackSmith.Rank
{
    /// <summary>승급 퀘스트의 현재 상태.</summary>
    public enum RankQuestState
    {
        /// <summary>다음 등급이 없다(최고 등급).</summary>
        None,
        /// <summary>승급 조건을 아직 채우지 못했거나 필요 개수가 미정이다.</summary>
        Locked,
        /// <summary>조건은 채웠지만 퀘스트 내용이 비어 있다(미정).</summary>
        Preparing,
        /// <summary>퀘스트 진행 중.</summary>
        Active
    }

    /// <summary>승급 퀘스트 규칙(순수 C#). 상태 계산과 클리어 판정을 한다.</summary>
    public class RankQuestRules
    {
        private readonly RankQuestSettings settings;

        public RankQuestRules(RankQuestSettings settings)
        {
            this.settings = settings;
        }

        public RankQuestState GetState(RankProgress progress)
        {
            if (!progress.HasNext) return RankQuestState.None;
            if (!progress.IsMet) return RankQuestState.Locked;
            RankQuestSettings.QuestEntry entry;
            return settings != null && settings.TryGet(progress.Next, out entry) ? RankQuestState.Active : RankQuestState.Preparing;
        }

        /// <summary>퀘스트가 진행 중(Active)일 때만 그 항목을 돌려준다.</summary>
        public bool TryGetActive(RankProgress progress, out RankQuestSettings.QuestEntry entry)
        {
            entry = null;
            return GetState(progress) == RankQuestState.Active && settings.TryGet(progress.Next, out entry);
        }

        /// <summary>
        /// 제작 결과가 target 등급 퀘스트를 클리어하는가. 알 수 없는 제작품, 퀘스트가 비어 있는 등급은 false.
        /// quality는 품질 보너스 적용 후의 최종 품질이다.
        /// </summary>
        public bool IsCleared(BlacksmithRank target, string craftedRecipeId, bool isUnknown, ItemQuality quality)
        {
            if (isUnknown || settings == null) return false;
            RankQuestSettings.QuestEntry entry;
            if (!settings.TryGet(target, out entry)) return false;
            return craftedRecipeId == entry.recipeId && quality >= entry.minQuality;
        }
    }
}
