using System;
using UnityEngine;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 열처리 미니게임 설정. 난이도 1 값은 철검 기준이고, 난이도별 변화는 docs/GameDesign.md §4를 따른다.
    /// 위치는 0~1(게이지 전체 길이 대비), 속도는 초당 게이지 길이 비율이다.
    /// </summary>
    [Serializable]
    public class HeatTreatSettings
    {
        [Tooltip("난이도 1에서 오브젝트가 게이지를 가로지르는 속도. 1이면 1초에 한 번 끝에서 끝까지 간다.")]
        [Min(0.05f)] public float speed = 1.2f;
        [Tooltip("눌러야 하는 목표 위치.")]
        [Range(0f, 1f)] public float targetPosition = 0.7f;
        [Tooltip("난이도 1에서 목표 범위(초록색)의 절반 폭. 이 범위에 마커 원이 온전히 들어오면 완성도 1.")]
        [Range(0.005f, 0.5f)] public float perfectHalfWidth = 0.06f;
        [Tooltip("난이도 10에서 목표 범위의 절반 폭. 곡선(지수 2)으로 줄어든다.")]
        [Range(0.005f, 0.5f)] public float perfectHalfWidthAtMax = 0.03f;
        [Tooltip("이 시간(초) 안에 누르지 못하면 완성도 0. 한 번 왕복하는 동안 누를 기회가 적은 것이 의도다.")]
        [Min(0.5f)] public float timeLimit = 2f;
        [Tooltip("클릭(또는 시간 초과) 뒤 결과를 보여주고 끝나기까지 기다리는 시간(초).")]
        [Min(0f)] public float endDelay = 0.6f;
        [Tooltip("난이도 10에서 오브젝트 속도의 배율(난이도 1은 1). 곡선(지수 2)으로 빨라진다.")]
        [Min(1f)] public float speedMultiplierAtMax = 2f;
        [Tooltip("켜면 시작할 때마다 목표 위치가 랜덤이다. 끄면 targetPosition 고정.")]
        public bool randomizeTarget = true;
    }

    /// <summary>
    /// 열처리: 게이지 바 안에서 오브젝트가 좌우로 왕복하고, 특정 위치에 도달할 때 한 번 클릭한다. (단일 타이밍)
    /// 순수 로직이라 시간(Tick)과 클릭(HandleClick)만 받고, 그리는 건 HeatTreatMinigameView가 한다.
    /// 판정은 클릭한 순간 마커 원이 목표 범위에 온전히 들어오면 1, 일부만 걸치면 0.5, 전혀 안 겹치면 0이다.
    /// </summary>
    public class HeatTreatMinigame : ICraftStageGame
    {
        /// <summary>일부만 걸쳤을 때의 완성도.</summary>
        public const float PartialScore = 0.5f;

        private readonly HeatTreatSettings settings;
        private readonly System.Random random;
        private float speed;
        private float elapsed;
        private float judgedTimer;

        public event Action<float> Completed;

        public HeatTreatMinigame(HeatTreatSettings settings, int? seed = null)
        {
            this.settings = settings ?? new HeatTreatSettings();
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            speed = this.settings.speed;
            PerfectHalfWidth = this.settings.perfectHalfWidth;
            TargetPosition = this.settings.targetPosition;
        }

        public bool IsRunning { get; private set; }
        public HeatTreatSettings Settings => settings;

        /// <summary>이번 판 목표 위치. Begin에서 (랜덤으로) 정해진다.</summary>
        public float TargetPosition { get; private set; }

        /// <summary>이번 판 목표 범위의 절반 폭(목표 앞뒤). 난이도가 오르면 줄어든다.</summary>
        public float PerfectHalfWidth { get; private set; }

        /// <summary>
        /// 마커 원의 반지름(게이지 전체 길이 대비). 화면에 그려지는 크기라 뷰가 Show에서 알려 준다.
        /// 목표 범위의 절반 폭보다 크면 마커가 범위에 온전히 들어올 수 없어 완성도 1이 나오지 않는다.
        /// </summary>
        public float MarkerHalfSize { get; set; }

        /// <summary>눌러서 판정됐거나 시간이 다 돼서 끝난 상태인가.</summary>
        public bool IsJudged { get; private set; }

        /// <summary>시간 초과로 끝났는가. (누르지 못했다)</summary>
        public bool TimedOut { get; private set; }

        /// <summary>누른 순간의 오브젝트 위치. 누르기 전에는 현재 위치와 같다.</summary>
        public float ClickedPosition { get; private set; }

        /// <summary>누른 위치 - 목표 위치. 음수는 목표보다 앞(왼쪽)에서 누른 것.</summary>
        public float Error { get; private set; }

        public float Time => elapsed;

        /// <summary>오브젝트의 현재 위치 0~1. 왼쪽 끝에서 시작해 오른쪽으로 먼저 움직인다.</summary>
        public float Position => IsJudged ? ClickedPosition : PositionAt(elapsed);

        /// <summary>
        /// 완성도 0 / 0.5 / 1. 마커가 목표 범위에 온전히 들어오면 1, 일부만 걸치면 0.5, 전혀 안 겹치면 0이다.
        /// 시간 초과로 누르지 못해도 0이다.
        /// </summary>
        public float Completeness { get; private set; }

        public void Begin(int difficulty)
        {
            // 속도는 빨라지고 목표 범위는 줄어든다(곡선, 지수 2).
            speed = settings.speed * CraftDifficulty.CurveLerp(1f, settings.speedMultiplierAtMax, difficulty);
            PerfectHalfWidth = CraftDifficulty.CurveLerp(settings.perfectHalfWidth, settings.perfectHalfWidthAtMax, difficulty);
            TargetPosition = settings.randomizeTarget
                ? Mathf.Lerp(PerfectHalfWidth, 1f - PerfectHalfWidth, (float)random.NextDouble())
                : settings.targetPosition;

            elapsed = 0f;
            judgedTimer = 0f;
            IsJudged = false;
            TimedOut = false;
            ClickedPosition = 0f;
            Error = 0f;
            Completeness = 0f;
            IsRunning = true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;

            if (!IsJudged)
            {
                elapsed += deltaTime;
                if (elapsed >= settings.timeLimit)
                {
                    // 못 누르고 시간이 다 됐다.
                    ClickedPosition = PositionAt(settings.timeLimit);
                    Error = ClickedPosition - TargetPosition;
                    Completeness = 0f;
                    TimedOut = true;
                    IsJudged = true;
                }
                return;
            }

            judgedTimer += deltaTime;
            if (judgedTimer >= settings.endDelay)
            {
                IsRunning = false;
                Completed?.Invoke(Completeness);
            }
        }

        /// <summary>클릭을 처리한다. 이미 판정됐으면 무시한다.</summary>
        public void HandleClick()
        {
            if (!IsRunning || IsJudged) return;

            ClickedPosition = PositionAt(elapsed);
            Error = ClickedPosition - TargetPosition;
            Completeness = Score(Mathf.Abs(Error));
            IsJudged = true;
        }

        public void Cancel()
        {
            IsRunning = false;
        }

        private float PositionAt(float time)
        {
            return Mathf.PingPong(time * speed, 1f);
        }

        /// <summary>마커 중심과 목표 중심의 거리로 판정한다. 마커 반지름을 더해 온전히 들어오는지, 겹치는지 본다.</summary>
        public float Score(float distance)
        {
            if (distance + MarkerHalfSize <= PerfectHalfWidth) return 1f;
            if (distance < PerfectHalfWidth + MarkerHalfSize) return PartialScore;
            return 0f;
        }
    }
}
