using UnityEngine;

namespace BlackSmith.Tutorial
{
    /// <summary>튜토리얼 한 단계의 정적 데이터.</summary>
    [CreateAssetMenu(fileName = "TutorialStep", menuName = "BlackSmith/Tutorial Step")]
    public class TutorialStepData : ScriptableObject
    {
        [SerializeField, TextArea] private string message;
        [SerializeField] private TutorialCondition condition;
        [SerializeField] private string itemId;

        /// <summary>빈 문자열이면 팝업을 숨긴다.</summary>
        public string Message => message;
        public TutorialCondition Condition => condition;
        /// <summary>HaveItem / ItemGone 조건에서 쓰는 아이템 ID.</summary>
        public string ItemId => itemId;
    }
}
