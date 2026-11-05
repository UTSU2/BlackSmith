using System;
using UnityEngine;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 열처리 미니게임 설정. 기획에서 판정·난이도별 수치가 아직 정해지지 않아 모두 동작 확인용 임시값이다.
    /// 위치는 0~1(게이지 전체 길이 대비), 속도는 초당 게이지 길이 비율이다.
    /// </summary>
    [Serializable]
    public class HeatTreatSettings
    {
        [Tooltip("오브젝트가 게이지를 가로지르는 속도. 1이면 1초에 한 번 끝에서 끝까지 간다.")]
        [Min(0.05f)] public float speed = 1.2f;
        [Tooltip("눌러야 하는 목표 위치.")]
        [Range(0f, 1f)] public float targetPosition = 0.7f;
        [Tooltip("목표 위치 앞뒤로 이 폭 안이면 완성도 최대.")]
        [Range(0.005f, 0.5f)] public float perfectHalfWidth = 0.04f;
        [Tooltip("목표에서 이만큼 벗어나면 완성도 0. perfectHalfWidth보다 커야 한다.")]
        [Range(0.01f, 1f)] public float tolerance = 0.3f;
        [Tooltip("이 시간(초) 안에 누르지 못하면 완성도 0.")]
        [Min(1f)] public float timeLimit = 6f;
        [Tooltip("클릭(또는 시간 초과) 뒤 결과를 보여주고 끝나기까지 기다리는 시간(초).")]
        [Min(0f)] public float endDelay = 0.6f;
    }

    /// <summary>
    /// 열처리: 게이지 바 안에서 오브젝트가 좌우로 왕복하고, 특정 위치에 도달할 때 한 번 클릭한다. (단일 타이밍)
    /// 순수 로직이라 시간(Tick)과 클릭(HandleClick)만 받고, 그리는 건 HeatTreatMinigameView가 한다.
    /// 판정 기준·점수식·실패 처리는 미정이라 지금은 "목표 위치와의 거리"만 본다.
    /// </summary>
    public class HeatTreatMinigame : ICraftStageGame
    {
        private readonly HeatTreatSettings settings;
        private float elapsed;
        private float judgedTimer;

        public event Action<float> Completed;

        public HeatTreatMinigame(HeatTreatSettings settings)
        {
            this.settings = settings ?? new HeatTreatSettings();
        }

        public bool IsRunning { get; private set; }
        public HeatTreatSettings Settings => settings;

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
        /// 완성도 0~1. 점수식이 미정이라 임시로 "목표와의 거리"에 비례한다.
        /// 오차가 perfectHalfWidth 이내면 1, tolerance 이상이면 0, 그 사이는 직선으로 줄어든다.
        /// TODO: 판정 기준과 점수식이 정해지면 교체한다.
        /// </summary>
        public float Completeness { get; private set; }

        public void Begin(int difficulty)
        {
            // TODO: 난이도(1~10)에 따른 속도·목표 폭은 미정이라 아직 반영하지 않는다.
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
                    Error = ClickedPosition - settings.targetPosition;
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
            Error = ClickedPosition - settings.targetPosition;
            Completeness = Score(Mathf.Abs(Error));
            IsJudged = true;
        }

        public void Cancel()
        {
            IsRunning = false;
        }

        private float PositionAt(float time)
        {
            return Mathf.PingPong(time * settings.speed, 1f);
        }

        private float Score(float distance)
        {
            if (distance <= settings.perfectHalfWidth) return 1f;
            float span = Mathf.Max(settings.tolerance - settings.perfectHalfWidth, 0.0001f);
            return Mathf.Clamp01(1f - (distance - settings.perfectHalfWidth) / span);
        }
    }
}
