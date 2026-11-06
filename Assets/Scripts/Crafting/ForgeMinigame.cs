using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 단조 미니게임 설정. 기획에서 난이도별 값, 개수, 판정 기준이 아직 정해지지 않아
    /// 아래 값은 모두 동작 확인용 임시값이다. (난이도 1~10에 따라 달라지지도 않는다)
    /// 길이는 초, 반지름과 좌표는 미니게임 공간 단위(ForgeMinigame.AreaSize 기준)다.
    /// </summary>
    [Serializable]
    public class ForgeSettings
    {
        [Min(1)] public int circleCount = 6;
        [Tooltip("시작 후 첫 원이 나타나기까지.")]
        [Min(0f)] public float startDelay = 0.5f;
        [Tooltip("원이 나타나는 간격.")]
        [Min(0.05f)] public float spawnInterval = 0.7f;
        [Tooltip("원이 나타나서 안쪽 원과 겹치기까지 걸리는 시간.")]
        [Min(0.1f)] public float shrinkDuration = 1.2f;
        [Tooltip("겹치는 순간 앞뒤로 이 시간 안에 누르면 성공.")]
        [Min(0.01f)] public float hitWindow = 0.15f;
        [Tooltip("안쪽(목표) 원 반지름.")]
        [Min(0.1f)] public float targetRadius = 0.8f;
        [Tooltip("줄어드는 원의 처음 반지름.")]
        [Min(0.2f)] public float startRadius = 2f;
        [Tooltip("원이 화면 가장자리에 붙지 않게 하는 여백.")]
        [Min(0f)] public float margin = 2f;
        [Tooltip("마지막 원 판정 뒤 끝나기까지 기다리는 시간.")]
        [Min(0f)] public float endDelay = 0.5f;
        [Tooltip("난이도 10에서 원 개수의 배율(난이도 1은 1). 선형으로 늘어난다.")]
        [Min(1f)] public float countMultiplierAtMax = 4f;
        [Tooltip("난이도 10에서 원이 줄어드는 속도의 배율(난이도 1은 1). 선형으로 빨라진다.")]
        [Min(1f)] public float shrinkSpeedMultiplierAtMax = 2f;
    }

    /// <summary>
    /// 단조: 여러 위치에 원이 나타나 줄어들고, 안쪽 원과 겹칠 때 클릭한다. (리듬게임식, 마우스만)
    /// 순수 로직이라 시간(Tick)과 클릭(HandleClick)만 받고, 그리는 건 ForgeMinigameView가 한다.
    /// 판정 기준·점수식·실패 처리는 미정이라 지금은 "겹치는 시각 ± hitWindow 안에 눌렀는가"만 본다.
    /// </summary>
    public class ForgeMinigame : ICraftStageGame
    {
        /// <summary>미니게임 공간 크기. 원의 위치와 반지름은 이 공간의 단위다. 왼쪽 아래가 (0,0).</summary>
        public static readonly Vector2 AreaSize = new Vector2(16f, 9f);

        public enum CircleState { Pending, Hit, Miss }

        public class Circle
        {
            public Vector2 Position { get; internal set; }
            public float SpawnTime { get; internal set; }

            /// <summary>줄어드는 원이 안쪽 원과 정확히 겹치는 시각.</summary>
            public float HitTime { get; internal set; }

            public CircleState State { get; internal set; }
            public float JudgedTime { get; internal set; }

            /// <summary>누른 시각 - 겹치는 시각. 음수는 일찍 누른 것. 누르지 못하고 놓쳤으면 0.</summary>
            public float TimingError { get; internal set; }
        }

        private readonly ForgeSettings settings;
        private readonly System.Random random;
        private readonly List<Circle> circles = new List<Circle>();
        private float time;
        private float lastJudgedTime;
        private float shrinkDuration;

        public event Action<float> Completed;

        public ForgeMinigame(ForgeSettings settings, int? seed = null)
        {
            this.settings = settings ?? new ForgeSettings();
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            shrinkDuration = this.settings.shrinkDuration;
        }

        /// <summary>이번 판 원이 나타나서 안쪽 원과 겹치기까지 걸리는 시간(초). 난이도가 오르면 짧아진다.</summary>
        public float ShrinkDuration => shrinkDuration;

        public bool IsRunning { get; private set; }
        public float Time => time;
        public ForgeSettings Settings => settings;
        public IReadOnlyList<Circle> Circles => circles;

        public int HitCount
        {
            get
            {
                int hits = 0;
                foreach (Circle circle in circles)
                {
                    if (circle.State == CircleState.Hit) hits++;
                }
                return hits;
            }
        }

        /// <summary>
        /// 완성도 0~1. 점수식이 미정이라 임시로 "성공한 원 / 전체 원"이다.
        /// TODO: 판정 기준과 점수식이 정해지면 교체한다.
        /// </summary>
        public float Completeness => circles.Count == 0 ? 0f : (float)HitCount / circles.Count;

        public void Begin(int difficulty)
        {
            // 난이도 1 = 현행 값. 개수와 줄어드는 속도만 선형으로 변한다. (원 크기·판정 폭은 변하지 않는다)
            float t = CraftDifficulty.Progress(difficulty);
            int count = Mathf.Max(1, Mathf.RoundToInt(settings.circleCount * Mathf.Lerp(1f, settings.countMultiplierAtMax, t)));
            shrinkDuration = settings.shrinkDuration / Mathf.Lerp(1f, settings.shrinkSpeedMultiplierAtMax, t);

            circles.Clear();
            time = 0f;
            lastJudgedTime = 0f;

            for (int i = 0; i < count; i++)
            {
                float spawn = settings.startDelay + i * settings.spawnInterval;
                circles.Add(new Circle
                {
                    Position = PickPosition(i),
                    SpawnTime = spawn,
                    HitTime = spawn + shrinkDuration,
                    State = CircleState.Pending,
                });
            }
            IsRunning = true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;
            time += deltaTime;

            bool allJudged = true;
            foreach (Circle circle in circles)
            {
                if (circle.State != CircleState.Pending) continue;

                // 겹치는 순간을 지나 허용 시간까지 못 눌렀으면 놓친 것이다.
                if (time > circle.HitTime + settings.hitWindow)
                {
                    circle.State = CircleState.Miss;
                    circle.JudgedTime = time;
                    circle.TimingError = 0f;
                    lastJudgedTime = time;
                }
                else
                {
                    allJudged = false;
                }
            }

            if (allJudged && time - lastJudgedTime >= settings.endDelay)
            {
                IsRunning = false;
                Completed?.Invoke(Completeness);
            }
        }

        /// <summary>
        /// 클릭을 처리한다. position은 미니게임 공간 좌표. 눌린 자리에 원이 없으면 아무 일도 없다.
        /// 원이 겹쳐 있으면 먼저 나타난 것부터 판정한다.
        /// </summary>
        public void HandleClick(Vector2 position)
        {
            if (!IsRunning) return;

            foreach (Circle circle in circles)
            {
                if (circle.State != CircleState.Pending || time < circle.SpawnTime) continue;

                float reach = Mathf.Max(GetRadius(circle), settings.targetRadius);
                if ((position - circle.Position).sqrMagnitude > reach * reach) continue;

                float error = time - circle.HitTime;
                circle.State = Mathf.Abs(error) <= settings.hitWindow ? CircleState.Hit : CircleState.Miss;
                circle.TimingError = error;
                circle.JudgedTime = time;
                lastJudgedTime = time;
                return;
            }
        }

        /// <summary>지금 줄어드는 원의 반지름. 안쪽 원과 겹치는 시각에 targetRadius가 된다.</summary>
        public float GetRadius(Circle circle)
        {
            float progress = (time - circle.SpawnTime) / shrinkDuration;
            return settings.startRadius - (settings.startRadius - settings.targetRadius) * progress;
        }

        public void Cancel()
        {
            IsRunning = false;
        }

        private Vector2 PickPosition(int index)
        {
            float minX = settings.margin, maxX = AreaSize.x - settings.margin;
            float minY = settings.margin, maxY = AreaSize.y - settings.margin;
            if (maxX <= minX || maxY <= minY)
            {
                return AreaSize * 0.5f;
            }

            // 바로 앞에 나온 원과 너무 가깝지 않게 몇 번 다시 뽑는다.
            float minGap = settings.targetRadius * 3f;
            Vector2 best = Vector2.zero;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                best = new Vector2(
                    minX + (float)random.NextDouble() * (maxX - minX),
                    minY + (float)random.NextDouble() * (maxY - minY));
                if (index == 0 || (best - circles[index - 1].Position).magnitude >= minGap) break;
            }
            return best;
        }
    }
}
