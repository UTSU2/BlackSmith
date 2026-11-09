namespace BlackSmith.Crafting
{
    /// <summary>
    /// 제작 단계. 순서는 GameDesign.md의 확정 순서(가열 → 단조 → 열처리 → 연마)를 따른다.
    /// 주의: 값이 에셋에 정수로 저장되므로 기존 항목의 순서를 바꾸거나 중간에 끼워 넣지 않는다. 새 단계는 끝에 추가한다.
    /// </summary>
    public enum CraftStage
    {
        Heat,
        Forge,
        HeatTreat,
        Polish,
    }
}
