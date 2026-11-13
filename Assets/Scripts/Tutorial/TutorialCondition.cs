namespace BlackSmith.Tutorial
{
    /// <summary>튜토리얼 단계의 완료 조건. 정수로 저장되므로 순서를 바꾸지 않는다.</summary>
    public enum TutorialCondition
    {
        Confirm = 0,
        HaveItem = 1,
        CraftStarted = 2,
        CraftFinished = 3,
        ItemGone = 4,
    }
}
