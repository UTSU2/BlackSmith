namespace BlackSmith.Rank
{
    /// <summary>
    /// 대장장이 등급 (docs/superpowers/specs/2026-11-19-v3-rank-skip-design.md).
    /// 값은 세이브에 정수로 저장되므로 순서를 바꾸지 않는다: 견습 → 초급 → 숙련 → 장인 → 명장.
    /// </summary>
    public enum BlacksmithRank
    {
        Apprentice = 0,
        Novice = 1,
        Skilled = 2,
        Artisan = 3,
        Grandmaster = 4
    }
}
