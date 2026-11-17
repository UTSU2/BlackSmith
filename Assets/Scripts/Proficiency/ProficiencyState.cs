namespace BlackSmith.Proficiency
{
    /// <summary>누적 숙련도를 단계·레벨로 풀어 놓은 값(런타임 데이터). ProficiencyRules.Evaluate가 만든다.</summary>
    public class ProficiencyState
    {
        /// <summary>누적 숙련도. Master 총량을 넘지 않는다.</summary>
        public int Points { get; }

        /// <summary>단계 번호(0부터). Master면 마지막 단계 다음 값이다.</summary>
        public int StageIndex { get; }

        /// <summary>단계 안의 레벨(1부터). Master면 0.</summary>
        public int Level { get; }

        public bool IsMaster { get; }

        /// <summary>현재 레벨에서 쌓인 양. Master면 0.</summary>
        public int Progress { get; }

        /// <summary>현재 레벨의 필요 수치. Master면 0.</summary>
        public int Required { get; }

        /// <summary>이 상태에서 총점에 더하는 품질 보너스.</summary>
        public decimal Bonus { get; }

        /// <summary>"초급 Lv.3" 또는 "Master".</summary>
        public string Label { get; }

        public ProficiencyState(int points, int stageIndex, int level, bool isMaster, int progress, int required, decimal bonus, string label)
        {
            Points = points;
            StageIndex = stageIndex;
            Level = level;
            IsMaster = isMaster;
            Progress = progress;
            Required = required;
            Bonus = bonus;
            Label = label;
        }
    }
}
