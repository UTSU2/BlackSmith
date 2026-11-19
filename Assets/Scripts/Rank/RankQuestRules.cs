using BlackSmith.Items;

namespace BlackSmith.Rank
{
    /// <summary>승급 퀘스트 규칙(순수 C#). 클리어 판정만 한다.</summary>
    public class RankQuestRules
    {
        private readonly RankQuestSettings settings;

        public RankQuestRules(RankQuestSettings settings)
        {
            this.settings = settings;
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
