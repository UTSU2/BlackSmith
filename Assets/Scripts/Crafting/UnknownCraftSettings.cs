using System.Collections.Generic;
using UnityEngine;
using BlackSmith.Items;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 레시피와 일치하지 않는 조합으로 제작할 때의 설정(정적 데이터). "알 수 없는 제작품"은 레시피가 아니므로
    /// 레시피 목록·도감·숙련도에 들어가지 않는다. (docs/V2.md §3) 에셋이 없으면 일치하는 조합만 제작할 수 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "UnknownCraftSettings", menuName = "BlackSmith/Crafting/Unknown Craft Settings")]
    public class UnknownCraftSettings : ScriptableObject
    {
        public const string ResourcePath = "Crafting/UnknownCraftSettings";

        [Tooltip("일치하는 레시피가 없을 때 만들어지는 제작품 (ID 999).")]
        [SerializeField] private CraftedItemData result;

        [Tooltip("거칠 제작 단계를 순서대로.")]
        [SerializeField] private List<CraftStage> stages = new List<CraftStage>
        {
            CraftStage.Heat, CraftStage.Forge, CraftStage.HeatTreat, CraftStage.Polish
        };

        [Tooltip("미니게임 난이도(1~10).")]
        [SerializeField, Range(1, 10)] private int baseDifficulty = 2;

        public CraftedItemData Result => result;
        public IReadOnlyList<CraftStage> Stages => stages;
        public int BaseDifficulty => baseDifficulty;

        public static UnknownCraftSettings Load()
        {
            return Resources.Load<UnknownCraftSettings>(ResourcePath);
        }
    }
}
