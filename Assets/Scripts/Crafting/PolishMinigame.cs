using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 연마 미니게임 설정. 기획에서 판정·난이도별 수치가 아직 정해지지 않아 모두 동작 확인용 임시값이다.
    /// 길이와 좌표는 미니게임 공간 단위(PolishMinigame.AreaSize 기준)다.
    /// </summary>
    [Serializable]
    public class PolishSettings
    {
        [Min(1)] public int flawCount = 4;
        [Tooltip("흠집 하나의 반지름.")]
        [Min(0.1f)] public float flawRadius = 0.9f;
        [Tooltip("흠집 안에서 문지른 거리 1당 줄어드는 양. 흠집 하나는 1에서 시작해 0이 되면 지워진다.")]
        [Min(0.01f)] public float sandPerUnit = 0.5f;
        [Tooltip("이 시간(초) 안에 못 지우면 거기서 끝난다.")]
        [Min(1f)] public float timeLimit = 10f;
        [Tooltip("흠집이 화면 가장자리에 붙지 않게 하는 여백.")]
        [Min(0f)] public float margin = 2f;
        [Tooltip("끝난 뒤(전부 지웠거나 시간 초과) 결과를 보여주고 끝나기까지 기다리는 시간(초).")]
        [Min(0f)] public float endDelay = 0.5f;
    }

    /// <summary>
    /// 연마: 사포질하듯 흠집을 클릭한 뒤 드래그해서 갈아낸다.
    /// 순수 로직이라 시간(Tick)과 포인터(PointerDown/Move/Up)만 받고, 그리는 건 PolishMinigameView가 한다.
    /// 판정 기준·점수식·실패 처리는 미정이라 지금은 "흠집을 지운 비율"만 본다.
    /// </summary>
    public class PolishMinigame : ICraftStageGame
    {
        /// <summary>미니게임 공간 크기. 흠집의 위치와 반지름은 이 공간의 단위다. 왼쪽 아래가 (0,0).</summary>
        public static readonly Vector2 AreaSize = new Vector2(16f, 9f);

        public class Flaw
        {
            public Vector2 Position { get; internal set; }

            /// <summary>남은 양. 1에서 시작해 0이 되면 지워진 것이다.</summary>
            public float Amount { get; internal set; }

            public bool IsCleared => Amount <= 0f;
        }

        private readonly PolishSettings settings;
        private readonly System.Random random;
        private readonly List<Flaw> flaws = new List<Flaw>();
        private Flaw activeFlaw;
        private Vector2 lastPosition;
        private float time;
        private float endTimer;
        private bool finished;

        public event Action<float> Completed;

        public PolishMinigame(PolishSettings settings, int? seed = null)
        {
            this.settings = settings ?? new PolishSettings();
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public bool IsRunning { get; private set; }
        public PolishSettings Settings => settings;
        public IReadOnlyList<Flaw> Flaws => flaws;
        public float Time => time;

        /// <summary>지금 문지르고 있는 흠집. 없으면 null.</summary>
        public Flaw ActiveFlaw => activeFlaw;

        /// <summary>전부 지웠거나 시간이 다 돼서 끝난 상태인가. (결과를 보여주는 중)</summary>
        public bool IsFinished => finished;

        public int ClearedCount
        {
            get
            {
                int cleared = 0;
                foreach (Flaw flaw in flaws)
                {
                    if (flaw.IsCleared) cleared++;
                }
                return cleared;
            }
        }

        /// <summary>
        /// 완성도 0~1. 점수식이 미정이라 임시로 "흠집별로 지운 비율의 평균"이다.
        /// TODO: 판정 기준과 점수식이 정해지면 교체한다.
        /// </summary>
        public float Completeness
        {
            get
            {
                if (flaws.Count == 0) return 0f;
                float sum = 0f;
                foreach (Flaw flaw in flaws) sum += 1f - Mathf.Clamp01(flaw.Amount);
                return sum / flaws.Count;
            }
        }

        public void Begin(int difficulty)
        {
            // TODO: 난이도(1~10)에 따른 흠집 개수·크기·단단함은 미정이라 아직 반영하지 않는다.
            flaws.Clear();
            activeFlaw = null;
            time = 0f;
            endTimer = 0f;
            finished = false;

            for (int i = 0; i < settings.flawCount; i++)
            {
                flaws.Add(new Flaw { Position = PickPosition(), Amount = 1f });
            }
            IsRunning = true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;

            if (!finished)
            {
                time += deltaTime;
                if (ClearedCount == flaws.Count || time >= settings.timeLimit)
                {
                    finished = true;
                    activeFlaw = null;
                }
                return;
            }

            endTimer += deltaTime;
            if (endTimer >= settings.endDelay)
            {
                IsRunning = false;
                Completed?.Invoke(Completeness);
            }
        }

        /// <summary>
        /// 눌렀다. position은 미니게임 공간 좌표. 흠집 위에서 눌러야 문지를 수 있고, 아니면 아무 일도 없다.
        /// 흠집이 겹쳐 있으면 먼저 만들어진 것부터 고른다.
        /// </summary>
        public void PointerDown(Vector2 position)
        {
            if (!IsRunning || finished) return;

            activeFlaw = null;
            lastPosition = position;
            foreach (Flaw flaw in flaws)
            {
                if (flaw.IsCleared) continue;
                if ((position - flaw.Position).sqrMagnitude <= settings.flawRadius * settings.flawRadius)
                {
                    activeFlaw = flaw;
                    return;
                }
            }
        }

        /// <summary>
        /// 누른 채로 움직였다. 고른 흠집 안에서 움직인 거리만큼만 흠집이 줄어든다.
        /// 흠집 밖에서 움직인 거리와 가만히 누르고 있는 것은 아무 효과가 없다.
        /// </summary>
        public void PointerMove(Vector2 position)
        {
            if (!IsRunning || finished || activeFlaw == null) return;

            float inside = LengthInsideCircle(lastPosition, position, activeFlaw.Position, settings.flawRadius);
            lastPosition = position;
            if (inside <= 0f) return;

            activeFlaw.Amount = Mathf.Max(0f, activeFlaw.Amount - inside * settings.sandPerUnit);
            if (activeFlaw.IsCleared) activeFlaw = null;
        }

        /// <summary>뗐다. 고른 흠집을 놓는다. (다시 눌러서 이어서 문지를 수 있다)</summary>
        public void PointerUp()
        {
            activeFlaw = null;
        }

        public void Cancel()
        {
            IsRunning = false;
            activeFlaw = null;
        }

        /// <summary>선분 a→b 중 원(중심 c, 반지름 r) 안에 있는 부분의 길이.</summary>
        private static float LengthInsideCircle(Vector2 a, Vector2 b, Vector2 c, float r)
        {
            Vector2 d = b - a;
            float length = d.magnitude;
            if (length <= 0f) return 0f;

            // a + t*d (t: 0~1) 와 원의 교점을 푼다.
            Vector2 f = a - c;
            float qa = Vector2.Dot(d, d);
            float qb = 2f * Vector2.Dot(f, d);
            float qc = Vector2.Dot(f, f) - r * r;
            float discriminant = qb * qb - 4f * qa * qc;
            if (discriminant <= 0f) return 0f;

            float root = Mathf.Sqrt(discriminant);
            float t0 = Mathf.Clamp01((-qb - root) / (2f * qa));
            float t1 = Mathf.Clamp01((-qb + root) / (2f * qa));
            return (t1 - t0) * length;
        }

        private Vector2 PickPosition()
        {
            float minX = settings.margin, maxX = AreaSize.x - settings.margin;
            float minY = settings.margin, maxY = AreaSize.y - settings.margin;
            if (maxX <= minX || maxY <= minY) return AreaSize * 0.5f;

            // 이미 만든 흠집과 겹치지 않게 몇 번 다시 뽑는다.
            float minGap = settings.flawRadius * 2.2f;
            Vector2 best = Vector2.zero;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                best = new Vector2(
                    minX + (float)random.NextDouble() * (maxX - minX),
                    minY + (float)random.NextDouble() * (maxY - minY));

                bool overlaps = false;
                foreach (Flaw other in flaws)
                {
                    if ((best - other.Position).magnitude < minGap)
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (!overlaps) break;
            }
            return best;
        }
    }
}
