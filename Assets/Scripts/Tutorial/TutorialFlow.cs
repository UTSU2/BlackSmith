using System;
using System.Collections.Generic;
using BlackSmith.Data;

namespace BlackSmith.Tutorial
{
    /// <summary>
    /// 튜토리얼 단계 진행(순수 C#). 현재 단계의 조건과 맞는 알림만 단계를 넘긴다.
    /// 새 단계에 들어가면 인벤토리 조건(HaveItem, ItemGone)을 즉시 평가한다.
    /// </summary>
    public class TutorialFlow
    {
        private readonly IReadOnlyList<TutorialStepData> steps;
        private readonly PlayerData player;
        private bool started;
        private bool itemSeen;

        public int Index { get; private set; }
        public bool IsFinished { get; private set; }
        public TutorialStepData Current => IsFinished || steps == null || Index >= steps.Count ? null : steps[Index];

        /// <summary>단계가 바뀔 때마다 (Start 포함).</summary>
        public event Action StepChanged;
        /// <summary>마지막 단계 완료 시 한 번.</summary>
        public event Action Finished;

        public TutorialFlow(IReadOnlyList<TutorialStepData> steps, PlayerData player)
        {
            this.steps = steps;
            this.player = player;
        }

        public void Start()
        {
            if (started) return;
            started = true;
            Index = 0;
            EnterStep();
        }

        public void NotifyConfirm() => Notify(TutorialCondition.Confirm);
        public void NotifyCraftStarted() => Notify(TutorialCondition.CraftStarted);
        public void NotifyCraftFinished() => Notify(TutorialCondition.CraftFinished);

        public void NotifyInventoryChanged()
        {
            if (!started || IsFinished) return;
            if (EvaluateInventory()) Advance();
        }

        private void Notify(TutorialCondition condition)
        {
            if (!started || IsFinished) return;
            TutorialStepData step = Current;
            if (step == null || step.Condition != condition) return;
            Advance();
        }

        private void Advance()
        {
            Index++;
            EnterStep();
        }

        // 현재 Index의 단계에 진입한다. null 단계는 건너뛰고, 인벤토리 조건이 이미 충족이면 연쇄로 넘어간다.
        private void EnterStep()
        {
            while (true)
            {
                int count = steps == null ? 0 : steps.Count;
                while (Index < count && steps[Index] == null) Index++;

                itemSeen = false;
                if (Index >= count)
                {
                    IsFinished = true;
                    StepChanged?.Invoke();
                    Finished?.Invoke();
                    return;
                }

                StepChanged?.Invoke();
                if (!EvaluateInventory()) return;
                Index++;
            }
        }

        // 현재 단계가 인벤토리 조건이고 충족되었으면 true.
        private bool EvaluateInventory()
        {
            TutorialStepData step = Current;
            if (step == null || player == null) return false;

            switch (step.Condition)
            {
                case TutorialCondition.HaveItem:
                    return player.GetTotalCount(step.ItemId) > 0;
                case TutorialCondition.ItemGone:
                    int total = player.GetTotalCount(step.ItemId);
                    if (total > 0) itemSeen = true;
                    return itemSeen && total == 0;
                default:
                    return false;
            }
        }
    }
}
