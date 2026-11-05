using System;
using UnityEngine;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 가열 미니게임 설정. 기획에서 판정·난이도별 수치가 아직 정해지지 않아 모두 동작 확인용 임시값이다.
    /// 바 값과 구간은 0~1(바 전체 길이 대비), 속도는 초당 변화량이다.
    /// </summary>
    [Serializable]
    public class HeatSettings
    {
        [Tooltip("가열 시간(초).")]
        [Min(1f)] public float duration = 8f;
        [Tooltip("시작할 때 바 값.")]
        [Range(0f, 1f)] public float startValue = 0.2f;
        [Tooltip("누르고 있는 동안 바가 오르는 속도.")]
        [Min(0.01f)] public float riseSpeed = 0.6f;
        [Tooltip("떼고 있는 동안 바가 내려가는 속도.")]
        [Min(0.01f)] public float fallSpeed = 0.5f;
        [Tooltip("유지해야 하는 목표 구간의 중심.")]
        [Range(0f, 1f)] public float targetCenter = 0.6f;
        [Tooltip("목표 구간의 절반 폭.")]
        [Range(0.01f, 0.5f)] public float targetHalfWidth = 0.1f;
    }

    /// <summary>
    /// 가열: 마우스를 누르다 떼기를 반복해 바를 일정한 위치(목표 구간)에 유지한다.
    /// 순수 로직이라 시간(Tick)과 누름 여부(SetHolding)만 받고, 그리는 건 HeatMinigameView가 한다.
    /// 판정 기준·점수식·실패 처리는 미정이라 지금은 "목표 구간 안에 있던 시간"만 본다.
    /// </summary>
    public class HeatMinigame : ICraftStageGame
    {
        private readonly HeatSettings settings;
        private float time;
        private float inZoneTime;
        private bool holding;

        public event Action<float> Completed;

        public HeatMinigame(HeatSettings settings)
        {
            this.settings = settings ?? new HeatSettings();
            Value = this.settings.startValue;
        }

        public bool IsRunning { get; private set; }
        public HeatSettings Settings => settings;

        /// <summary>현재 바 값 0~1.</summary>
        public float Value { get; private set; }

        /// <summary>시작 후 흐른 시간(초).</summary>
        public float Time => time;

        public bool IsHolding => holding;

        public bool IsInZone => Mathf.Abs(Value - settings.targetCenter) <= settings.targetHalfWidth;

        /// <summary>
        /// 완성도 0~1. 점수식이 미정이라 임시로 "목표 구간 안에 있던 시간 / 전체 시간"이다.
        /// TODO: 판정 기준과 점수식이 정해지면 교체한다.
        /// </summary>
        public float Completeness => Mathf.Clamp01(inZoneTime / settings.duration);

        public void Begin(int difficulty)
        {
            // TODO: 난이도(1~10)에 따른 속도·구간 폭은 미정이라 아직 반영하지 않는다.
            time = 0f;
            inZoneTime = 0f;
            holding = false;
            Value = settings.startValue;
            IsRunning = true;
        }

        /// <summary>마우스를 누르는 중이면 true, 떼면 false.</summary>
        public void SetHolding(bool isHolding)
        {
            if (!IsRunning) return;
            holding = isHolding;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;

            // 마지막 프레임이 끝 시각을 넘지 않도록 자른다.
            float step = Mathf.Min(deltaTime, settings.duration - time);
            if (step > 0f)
            {
                float speed = holding ? settings.riseSpeed : -settings.fallSpeed;
                Value = Mathf.Clamp01(Value + speed * step);
                time += step;
                if (IsInZone) inZoneTime += step;
            }

            if (time >= settings.duration)
            {
                IsRunning = false;
                holding = false;
                Completed?.Invoke(Completeness);
            }
        }

        public void Cancel()
        {
            IsRunning = false;
            holding = false;
        }
    }
}
