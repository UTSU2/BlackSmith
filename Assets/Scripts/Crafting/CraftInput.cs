using BlackSmith.Items;

namespace BlackSmith.Crafting
{
    /// <summary>모루에 투입한 재료 한 줄: (아이템, 등급)별 수량. 제작 규칙 계산에 쓰는 값 하나짜리 스냅샷이다.</summary>
    public readonly struct CraftInput
    {
        public readonly string ItemId;
        public readonly ItemGrade Grade;
        public readonly int Count;

        public CraftInput(string itemId, ItemGrade grade, int count)
        {
            ItemId = itemId;
            Grade = grade;
            Count = count;
        }
    }
}
