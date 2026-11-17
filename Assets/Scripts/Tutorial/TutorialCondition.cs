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
        /// <summary>제작 결과 팝업이 닫혔을 때(확인). 결과 팝업과 함께 보이는 안내 단계에 쓴다.</summary>
        CraftResultClosed = 5,
    }
}
