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
        [Tooltip("난이도 1에서 100%를 채우는 데 필요한 구간 안 시간 비율(전체 시간 대비). 임시값(미정).")]
        [Range(0.05f, 0.7f)] public float requiredRatioAtMinDifficulty = 0.4f;
        [Tooltip("필요 시간 비율의 상한(전체 시간 대비). 난이도가 올라도 이 값을 넘지 않는다.")]
        [Range(0.05f, 0.7f)] public float requiredRatioCap = 0.7f;
        [Tooltip("필요 시간 비율의 난이도 곡선 지수. 2면 낮은 난이도 사이보다 높은 난이도 사이의 증가폭이 크다.")]
        [Min(1f)] public float requiredRatioCurvePower = 2f;
        [Tooltip("난이도 10에서 목표 구간 절반 폭의 배율(난이도 1은 1). 선형으로 줄어든다.")]
        [Range(0.1f, 1f)] public float halfWidthScaleAtMax = 0.5f;
        [Tooltip("켜면 시작할 때마다 목표 구간 위치가 랜덤이다. 끄면 targetCenter 고정.")]
        public bool randomizeTarget = true;
    }

    /// <summary>
    /// 가열: 마우스를 누르다 떼기를 반복해 바를 일정한 위치(목표 구간)에 유지한다.
    /// 순수 로직이라 시간(Tick)과 누름 여부(SetHolding)만 받고, 그리는 건 HeatMinigameView가 한다.
    /// 판정 기준·점수식·실패 처리는 미정이라 지금은 "목표 구간 안에 있던 시간"만 본다.
    /// </summary>
    public class HeatMinigame : ICraftStageGame
    {
        private readonly HeatSettings settings;
        private readonly System.Random random;
        private float time;
        private float inZoneTime;
        private float requiredTime;
        private bool holding;

        public event Action<float> Completed;

        public HeatMinigame(HeatSettings settings, int? seed = null)
        {
            this.settings = settings ?? new HeatSettings();
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            Value = this.settings.startValue;
            Center = this.settings.targetCenter;
            HalfWidth = this.settings.targetHalfWidth;
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

        /// <summary>100%를 채우는 데 필요한 구간 안 시간(초). 난이도에 따라 늘며 전체 시간의 requiredRatioCap을 넘지 않는다.</summary>
        public float RequiredTime => requiredTime;

        /// <summary>
        /// 완성도 0~1. 점수식이 미정이라 임시로 "목표 구간 안에 있던 시간 / 필요 시간"이다.
        /// 전체 시간 중 필요 시간만큼만 구간 안에 있으면 100%가 된다.
        /// TODO: 판정 기준과 점수식이 정해지면 교체한다.
        /// </summary>
        public float Completeness => requiredTime > 0f ? Mathf.Clamp01(inZoneTime / requiredTime) : 0f;

        public void Begin(int difficulty)
        {
            // 난이도 1 = 현행 값. 필요 시간 비율은 볼록 곡선(t^power), 구간 폭은 선형으로 줄어든다.
            // TODO: 상승·하강 속도의 난이도 변화는 미정이라 반영하지 않는다.
            float t = CraftDifficulty.Progress(difficulty);
            float ratio = Mathf.Lerp(settings.requiredRatioAtMinDifficulty, settings.requiredRatioCap, Mathf.Pow(t, settings.requiredRatioCurvePower));
            requiredTime = settings.duration * Mathf.Min(ratio, settings.requiredRatioCap);

            HalfWidth = settings.targetHalfWidth * Mathf.Lerp(1f, settings.halfWidthScaleAtMax, t);
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
