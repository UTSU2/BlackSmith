using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 단조 미니게임 설정. 난이도 1 값은 철검 기준이고, 난이도별 변화는 docs/GameDesign.md §4를 따른다.
    /// 길이는 초, 반지름과 좌표는 미니게임 공간 단위(ForgeMinigame.AreaSize 기준)다.
    /// </summary>
    [Serializable]
    public class ForgeSettings
    {
        [Tooltip("난이도 1의 원 개수.")]
        [Min(1)] public int circleCount = 6;
        [Tooltip("시작 후 첫 원이 나타나기까지.")]
        [Min(0f)] public float startDelay = 0.5f;
        [Tooltip("난이도 1에서 원이 나타나는 간격.")]
        [Min(0.05f)] public float spawnInterval = 0.7f;
        [Tooltip("난이도 10에서 원이 나타나는 간격. 곡선(지수 2)으로 줄어든다.")]
        [Min(0.05f)] public float spawnIntervalAtMax = 0.3f;
        [Tooltip("난이도 1에서 원이 나타나서 안쪽 원과 겹치기까지 걸리는 시간.")]
        [Min(0.1f)] public float shrinkDuration = 1.2f;
        [Tooltip("난이도 10에서 원이 나타나서 안쪽 원과 겹치기까지 걸리는 시간. 곡선(지수 2)으로 줄어든다.")]
        [Min(0.1f)] public float shrinkDurationAtMax = 0.5f;
        [Tooltip("겹치는 순간 앞뒤로 이 시간 안에 누르면 Perfect(1점).")]
        [FormerlySerializedAs("hitWindow")]
        [Min(0.01f)] public float perfectWindow = 0.15f;
        [Tooltip("Perfect 폭보다 이만큼 더 넓은 범위에서 누르면 Good(0.5점). 그 밖은 Miss(0점).")]
        [Min(0f)] public float goodExtraWindow = 0.2f;
        [Tooltip("안쪽(목표) 원 반지름.")]
        [Min(0.1f)] public float targetRadius = 0.8f;
        [Tooltip("줄어드는 원의 처음 반지름.")]
        [Min(0.2f)] public float startRadius = 2f;
        [Tooltip("원이 화면 가장자리에 붙지 않게 하는 여백.")]
        [Min(0f)] public float margin = 2f;
        [Tooltip("마지막 원 판정 뒤 끝나기까지 기다리는 시간.")]
        [Min(0f)] public float endDelay = 0.5f;
        [Tooltip("난이도 10에서 원 개수의 배율(난이도 1은 1). 곡선(지수 2)으로 늘어난다.")]
        [Min(1f)] public float countMultiplierAtMax = 3f;

        /// <summary>Good으로 치는 오차의 한계(초). Perfect 폭 + 추가 폭.</summary>
        public float GoodWindow => perfectWindow + goodExtraWindow;
    }

    /// <summary>
    /// 단조: 여러 위치에 원이 나타나 줄어들고, 안쪽 원과 겹칠 때 클릭한다. (리듬게임식, 마우스만)
    /// 순수 로직이라 시간(Tick)과 클릭(HandleClick)만 받고, 그리는 건 ForgeMinigameView가 한다.
    /// 판정은 겹치는 시각과 누른 시각의 오차로 한다: Perfect(1점) / Good(0.5점) / Miss(0점).
    /// </summary>
    public class ForgeMinigame : ICraftStageGame
    {
        /// <summary>미니게임 공간 크기. 원의 위치와 반지름은 이 공간의 단위다. 왼쪽 아래가 (0,0).</summary>
        public static readonly Vector2 AreaSize = new Vector2(16f, 9f);

        /// <summary>Good 판정이 주는 점수(Perfect는 1, Miss는 0).</summary>
        public const float GoodScore = 0.5f;

        public enum CircleState { Pending, Perfect, Good, Miss }

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
        private float spawnInterval;

        public event Action<float> Completed;

        public ForgeMinigame(ForgeSettings settings, int? seed = null)
        {
            this.settings = settings ?? new ForgeSettings();
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            shrinkDuration = this.settings.shrinkDuration;
            spawnInterval = this.settings.spawnInterval;
        }

        /// <summary>이번 판 원이 나타나서 안쪽 원과 겹치기까지 걸리는 시간(초). 난이도가 오르면 짧아진다.</summary>
        public float ShrinkDuration => shrinkDuration;

        /// <summary>이번 판 원이 나타나는 간격(초). 난이도가 오르면 짧아진다.</summary>
        public float SpawnInterval => spawnInterval;

        /// <summary>
        /// 마지막 원까지 모두 판정되고 끝 대기가 지나는 시각(초). 난이도별 소요 시간을 점검할 때 쓴다.
        /// 마지막 원이 판정되는 시점은 Good 폭이 끝나는 때(놓친 경우)로 계산한다.
        /// </summary>
        public float ExpectedEndTime
        {
            get
            {
                if (circles.Count == 0) return 0f;
                return circles[circles.Count - 1].HitTime + settings.GoodWindow + settings.endDelay;
            }
        }

        public bool IsRunning { get; private set; }
        public float Time => time;
        public ForgeSettings Settings => settings;
        public IReadOnlyList<Circle> Circles => circles;

        public int PerfectCount => CountState(CircleState.Perfect);
        public int GoodCount => CountState(CircleState.Good);

        /// <summary>
        /// 완성도 0~1 = (Perfect x 1 + Good x 0.5) / 전체 원 수.
        /// </summary>
        public float Completeness => circles.Count == 0 ? 0f : (PerfectCount + GoodCount * GoodScore) / circles.Count;

        public void Begin(int difficulty)
        {
            // 개수, 나타나는 간격, 줄어드는 시간이 곡선(지수 2)으로 변한다. (원 크기·판정 폭은 변하지 않는다)
            int count = Mathf.Max(1, Mathf.RoundToInt(settings.circleCount * CraftDifficulty.CurveLerp(1f, settings.countMultiplierAtMax, difficulty)));
            spawnInterval = CraftDifficulty.CurveLerp(settings.spawnInterval, settings.spawnIntervalAtMax, difficulty);
            shrinkDuration = CraftDifficulty.CurveLerp(settings.shrinkDuration, settings.shrinkDurationAtMax, difficulty);

            circles.Clear();
            time = 0f;
            lastJudgedTime = 0f;

            for (int i = 0; i < count; i++)
            {
                float spawn = settings.startDelay + i * spawnInterval;
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

                // 겹치는 순간을 지나 Good 폭까지 못 눌렀으면 놓친 것이다.
                if (time > circle.HitTime + settings.GoodWindow)
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
                circle.State = Judge(Mathf.Abs(error));
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

        /// <summary>타이밍 오차(초, 절댓값)로 판정한다. Perfect 폭 이내 / Good 폭 이내 / 그 밖은 Miss.</summary>
        public CircleState Judge(float absError)
        {
            if (absError <= settings.perfectWindow) return CircleState.Perfect;
            if (absError <= settings.GoodWindow) return CircleState.Good;
            return CircleState.Miss;
        }

        private int CountState(CircleState state)
        {
            int count = 0;
            foreach (Circle circle in circles)
            {
                if (circle.State == state) count++;
            }
            return count;
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
