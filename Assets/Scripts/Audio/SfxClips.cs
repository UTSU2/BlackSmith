using UnityEngine;

namespace BlackSmith.Audio
{
    /// <summary>
    /// 미니게임 효과음 클립 모음. 음색은 미정이라 클립은 이 에셋에서만 연결하고, 교체해도 코드는 그대로다.
    /// 파일은 Assets/Audio/Sfx에 있다. docs/SoundPlan.md 참고.
    /// </summary>
    [CreateAssetMenu(fileName = "SfxClips", menuName = "BlackSmith/Sfx Clips")]
    public class SfxClips : ScriptableObject
    {
        [Header("가열")]
        [Tooltip("루프. 누르고 있는 동안 재생.")]
        public AudioClip heatHold;

        [Header("단조")]
        public AudioClip forgePerfect;
        public AudioClip forgeGood;
        public AudioClip forgeMiss;

        [Header("열처리")]
        public AudioClip quenchFull;
        public AudioClip quenchHalf;
        public AudioClip quenchFail;

        [Header("연마")]
        [Tooltip("루프. 흠집 안에서 실제로 문지르는 동안 재생.")]
        public AudioClip sandLoop;
        public AudioClip flawCleared;
    }
}
