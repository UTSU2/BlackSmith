using System;
using UnityEngine;

namespace BlackSmith.Rank
{
    /// <summary>플레이어가 가진 스킵 장비 하나의 상태(런타임, 저장됨). 점수·비용은 SkipGearRules가 계산한다.</summary>
    [Serializable]
    public class SkipGearEntry
    {
        [SerializeField] private int kind;
        [SerializeField] private int level = 1;
        [SerializeField] private bool enabled = true;

        public SkipGearKind Kind => (SkipGearKind)kind;
        public int Level => level;
        public bool Enabled => enabled;

        public SkipGearEntry(SkipGearKind kind)
        {
            this.kind = (int)kind;
        }

        public void SetEnabled(bool value) { enabled = value; }
        public void SetLevel(int value) { level = value; }
    }
}
