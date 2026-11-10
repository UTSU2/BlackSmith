namespace BlackSmith.Crafting
{
    /// <summary>모루에 투입한 재료 한 줄: 아이템별 수량. 제작 규칙 계산에 쓰는 값 하나짜리 스냅샷이다.</summary>
    public readonly struct CraftInput
    {
        public readonly string ItemId;
        public readonly int Count;

        public CraftInput(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }
}
