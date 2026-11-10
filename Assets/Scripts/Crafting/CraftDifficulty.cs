namespace BlackSmith.Crafting
{
    /// <summary>
    /// 미니게임 난이도(1~10). 값은 레시피에 정의된 기본값 그대로다. (재료 등급에 따른 가중 없음)
    /// 4개 미니게임에 같은 값을 쓰고 플레이어에게는 표시하지 않는다.
    /// 단계별 미니게임 수치는 docs/GameDesign.md §4에 정해져 있고, 각 미니게임이 아래 보간 함수로 난이도 1 값과 10 값 사이를 잇는다.
    /// </summary>
    public static class CraftDifficulty
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 10;

        /// <summary>곡선 보간의 지수. 2면 낮은 난이도 사이보다 높은 난이도 사이의 변화폭이 크다.</summary>
        public const float CurvePower = 2f;

        /// <summary>난이도 1이면 0, 10이면 1인 선형 보간 계수. 선형으로 정해진 항목(가열의 100% 상한 비율)에 쓴다.</summary>
        public static float Progress(int difficulty)
        {
            return UnityEngine.Mathf.Clamp01((difficulty - MinLevel) / (float)(MaxLevel - MinLevel));
        }

        /// <summary>난이도 1이면 0, 10이면 1인 곡선 보간 계수(Progress의 CurvePower제곱).</summary>
        public static float Curve(int difficulty)
        {
            return UnityEngine.Mathf.Pow(Progress(difficulty), CurvePower);
        }

        /// <summary>난이도 1에서 atMin, 난이도 10에서 atMax가 되는 곡선 보간 값.</summary>
        public static float CurveLerp(float atMin, float atMax, int difficulty)
        {
            return UnityEngine.Mathf.Lerp(atMin, atMax, Curve(difficulty));
        }

        /// <summary>레시피에 정의된 기본 난이도를 1~10 범위로 맞춘다. 재료 등급·종류·개수는 영향을 주지 않는다.</summary>
        public static int Calculate(int baseDifficulty)
        {
            if (baseDifficulty < MinLevel) return MinLevel;
            return baseDifficulty > MaxLevel ? MaxLevel : baseDifficulty;
        }
    }
}
