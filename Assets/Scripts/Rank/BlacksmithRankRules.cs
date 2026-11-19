namespace BlackSmith.Rank
{
    /// <summary>다음 등급까지의 진행 상황.</summary>
    public readonly struct RankProgress
    {
        public bool HasNext { get; }
        public BlacksmithRank Next { get; }
        public int Codex { get; }
        public int RequiredCodex { get; }
        public int Masters { get; }
        public int RequiredMasters { get; }

        /// <summary>두 필요 개수가 모두 정해졌는가(0 이상). -1(미정)이면 false.</summary>
        public bool IsDecided { get; }

        /// <summary>조건을 모두 채웠는가. 미정이면 false.</summary>
        public bool IsMet { get; }

        public RankProgress(bool hasNext, BlacksmithRank next, int codex, int requiredCodex, int masters, int requiredMasters)
        {
            HasNext = hasNext;
            Next = next;
            Codex = codex;
            RequiredCodex = requiredCodex;
            Masters = masters;
            RequiredMasters = requiredMasters;
            IsDecided = hasNext && requiredCodex >= 0 && requiredMasters >= 0;
            IsMet = IsDecided && codex >= requiredCodex && masters >= requiredMasters;
        }
    }

    /// <summary>등급 규칙(순수 C#). 도감 해금 수와 master 수는 각각 따로 세고 둘 다 채워야 승급 조건을 만족한다.</summary>
    public class BlacksmithRankRules
    {
        private readonly BlacksmithRankSettings settings;

        public BlacksmithRankRules(BlacksmithRankSettings settings)
        {
            this.settings = settings;
        }

        public RankProgress Evaluate(BlacksmithRank current, int codexCount, int masterCount)
        {
            if (current >= BlacksmithRank.Grandmaster) return new RankProgress(false, current, codexCount, -1, masterCount, -1);

            BlacksmithRank next = current + 1;
            int requiredCodex = settings != null ? settings.GetRequiredCodex(next) : -1;
            int requiredMasters = settings != null ? settings.GetRequiredMasters(next) : -1;
            return new RankProgress(true, next, codexCount, requiredCodex, masterCount, requiredMasters);
        }
    }
}
