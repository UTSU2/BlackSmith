using System;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 제작 단계 하나를 맡는 미니게임. 순수 로직이라 화면은 모른다. (화면은 UI 쪽 View가 상태를 읽어 그린다)
    /// CraftingController가 Begin으로 시작하고 매 프레임 Tick을 불러 준다.
    /// </summary>
    public interface ICraftStageGame
    {
        /// <summary>끝났을 때 호출 (완성도 0~1). 완성도 계산 규칙은 미정이라 임시다.</summary>
        event Action<float> Completed;

        bool IsRunning { get; }

        void Begin(int difficulty);

        void Tick(float deltaTime);

        /// <summary>중간에 그만둔다. Completed는 호출되지 않는다.</summary>
        void Cancel();
    }
}
