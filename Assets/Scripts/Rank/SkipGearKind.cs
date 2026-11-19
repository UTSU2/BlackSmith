using BlackSmith.Crafting;

namespace BlackSmith.Rank
{
    /// <summary>스킵 장비 종류. 값은 세이브에 정수로 저장되므로 순서를 바꾸지 않는다.</summary>
    public enum SkipGearKind
    {
        Heat = 0,
        HeatTreat = 1
    }

    public static class SkipGearKinds
    {
        /// <summary>이 장비가 건너뛰는 제작 단계.</summary>
        public static CraftStage ToStage(this SkipGearKind kind)
        {
            return kind == SkipGearKind.Heat ? CraftStage.Heat : CraftStage.HeatTreat;
        }
    }
}
