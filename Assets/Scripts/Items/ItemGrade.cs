using System;

namespace BlackSmith.Items
{
    /// <summary>
    /// 아이템 등급. 등급의 이름과 개수는 아직 정해지지 않아 임시로 5개를 두었다.
    /// 확정되면 이 파일만 수정하면 된다.
    /// 주의: 값의 순서가 ItemData.sellPriceMultipliers 배열의 인덱스가 된다.
    /// 등급을 추가/삭제/순서 변경하면 기존 아이템의 배율 배열도 맞춰야 한다
    /// (개수가 달라지면 ItemData.OnValidate가 부족한 칸을 1.0으로 채운다).
    /// </summary>
    public enum ItemGrade
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
    }

    public static class ItemGradeInfo
    {
        /// <summary>등급 개수.</summary>
        public static readonly int Count = Enum.GetValues(typeof(ItemGrade)).Length;
    }
}
