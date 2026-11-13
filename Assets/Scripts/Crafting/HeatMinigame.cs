using System;
using UnityEngine;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 가열 미니게임 설정. 난이도 1 값은 철검 기준이고, 난이도별 변화는 docs/GameDesign.md §4를 따른다.
    /// 바 값과 구간은 0~1(바 전체 길이 대비), 속도는 초당 변화량이다.
    /// </summary>
    [Serializable]
    public class HeatSettings
    {
        [Tooltip("가열 시간(초).")]
        [Min(1f)] public float duration = 5f;
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
        [Tooltip("난이도 1에서 100%로 치는 구간 안 시간 비율(전체 시간 대비).")]
        [Range(0.05f, 1f)] public float requiredRatioAtMin = 0.4f;
        [Tooltip("난이도 10에서 100%로 치는 구간 안 시간 비율(전체 시간 대비). 난이도 1~10 사이는 선형이다.")]
        [Range(0.05f, 1f)] public float requiredRatioAtMax = 0.8f;
        [Tooltip("난이도 1~10의 목표 구간 절반 폭 배율(난이도 1 = 철검 기준 폭). 5%씩 줄다가 9단계 60%, 10단계 50%.")]
        public float[] halfWidthScaleByDifficulty = { 1f, 0.95f, 0.9f, 0.85f, 0.8f, 0.75f, 0.7f, 0.65f, 0.6f, 0.5f };
        [Tooltip("난이도 10에서 바 상승·하강 속도의 배율(난이도 1은 1). 곡선(지수 2)으로 빨라진다.")]
        [Min(1f)] public float speedMultiplierAtMax = 2f;
        [Tooltip("켜면 시작할 때마다 목표 구간 위치가 랜덤이다. 끄면 targetCenter 고정.")]
        public bool randomizeTarget = true;
    }

    /// <summary>
    /// 가열: 마우스를 누르다 떼기를 반복해 바를 일정한 위치(목표 구간)에 유지한다.
    /// 순수 로직이라 시간(Tick)과 누름 여부(SetHolding)만 받고, 그리는 건 HeatMinigameView가 한다.
    /// 완성도는 "목표 구간 안에 있던 시간 / (전체 시간 x 100% 상한 비율)"이다.
    /// </summary>
    public class HeatMinigame : ICraftStageGame
    {
        private readonly HeatSettings settings;
        private readonly System.Random random;
        private float time;
        private float inZoneTime;
        private float requiredTime;
        private float riseSpeed;
        private float fallSpeed;
        private bool holding;

        public event Action<float> Completed;

        public HeatMinigame(HeatSettings settings, int? seed = null)
        {
            this.settings = settings ?? new HeatSettings();
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            Value = this.settings.startValue;
            Center = this.settings.targetCenter;
            HalfWidth = this.settings.targetHalfWidth;
            riseSpeed = this.settings.riseSpeed;
            fallSpeed = this.settings.fallSpeed;
        }

        public bool IsRunning { get; private set; }
        public HeatSettings Settings => settings;

        /// <summary>현재 바 값 0~1.</summary>
        public float Value { get; private set; }

        /// <summary>시작 후 흐른 시간(초).</summary>
        public float Time => time;

        public bool IsHolding => holding;

        /// <summary>이번 판 목표 구간의 중심. Begin에서 (랜덤으로) 정해진다.</summary>
        public float Center { get; private set; }

        /// <summary>이번 판 목표 구간의 절반 폭. 난이도가 오르면 줄어든다.</summary>
        public float HalfWidth { get; private set; }

        public bool IsInZone => Mathf.Abs(Value - Center) <= HalfWidth;

        /// <summary>100%로 치는 구간 안 시간(초). 전체 시간 x 상한 비율이며, 난이도가 오르면 늘어난다.</summary>
        public float RequiredTime => requiredTime;

        /// <summary>
        /// 완성도 0~1 = 목표 구간 안에 있던 시간 / 필요 시간(전체 시간 x 100% 상한 비율).
        /// 필요 시간만큼만 구간 안에 있으면 100%가 되고, 넘으면 1로 자른다.
        /// </summary>
        public float Completeness => requiredTime > 0f ? Mathf.Clamp01(inZoneTime / requiredTime) : 0f;

        public void Begin(int difficulty)
        {
            // 100% 상한 비율은 선형, 구간 폭은 배율 표, 바 속도는 곡선(지수 2)으로 변한다.
            float ratio = Mathf.Lerp(settings.requiredRatioAtMin, settings.requiredRatioAtMax, CraftDifficulty.Progress(difficulty));
            requiredTime = settings.duration * ratio;

            float speedScale = CraftDifficulty.CurveLerp(1f, settings.speedMultiplierAtMax, difficulty);
            riseSpeed = settings.riseSpeed * speedScale;
            fallSpeed = settings.fallSpeed * speedScale;

            HalfWidth = settings.targetHalfWidth * HalfWidthScale(difficulty);
            Center = settings.randomizeTarget
                ? Mathf.Lerp(HalfWidth, 1f - HalfWidth, (float)random.NextDouble())
                : settings.targetCenter;
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
                float speed = holding ? riseSpeed : -fallSpeed;
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

        /// <summary>난이도(1~10)에 해당하는 구간 폭 배율. 표가 비어 있으면 1.</summary>
        private float HalfWidthScale(int difficulty)
        {
            float[] table = settings.halfWidthScaleByDifficulty;
            if (table == null || table.Length == 0) return 1f;
            int index = Mathf.Clamp(difficulty - CraftDifficulty.MinLevel, 0, table.Length - 1);
            return table[index];
        }
    }
}
