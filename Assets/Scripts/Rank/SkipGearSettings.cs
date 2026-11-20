using System.Collections.Generic;
using UnityEngine;

namespace BlackSmith.Rank
{
    /// <summary>
    /// 스킵 장비 설정(정적 데이터): 레벨별 고정 점수, 강화 비용, 지급 등급.
    /// 강화 비용 -1은 미정이며 이때는 강화할 수 없다. 값은 사용자가 에셋에서 채운다.
    /// </summary>
    [CreateAssetMenu(fileName = "SkipGearSettings", menuName = "BlackSmith/Skip Gear Settings")]
    public class SkipGearSettings : ScriptableObject
    {
        public const string ResourcePath = "Rank/SkipGearSettings";

        [Tooltip("레벨 1~5의 스킵 단계 고정 점수. 최대 9로 두어 스킵만으로는 최상품(40)이 나오지 않게 한다.")]
        [SerializeField] private List<int> scores = new List<int> { 5, 6, 7, 8, 9 };
        [Tooltip("레벨 n → n+1 강화 비용(Lv1→2, Lv2→3, Lv3→4, Lv4→5). -1 = 미정(강화 불가).")]
        [SerializeField] private List<int> upgradeCosts = new List<int> { -1, -1, -1, -1 };
        [Tooltip("가열 스킵 장비를 주는 등급.")]
        [SerializeField] private BlacksmithRank heatUnlockRank = BlacksmithRank.Skilled;
        [Tooltip("열처리 스킵 장비를 주는 등급.")]
        [SerializeField] private BlacksmithRank heatTreatUnlockRank = BlacksmithRank.Grandmaster;

        public IReadOnlyList<int> Scores => scores;
        public IReadOnlyList<int> UpgradeCosts => upgradeCosts;
        public BlacksmithRank HeatUnlockRank => heatUnlockRank;
        public BlacksmithRank HeatTreatUnlockRank => heatTreatUnlockRank;

        /// <summary>설정 에셋을 불러온다. 없으면 null(스킵 없이 동작).</summary>
        public static SkipGearSettings Load()
        {
            return Resources.Load<SkipGearSettings>(ResourcePath);
        }
    }
}
