namespace BlackSmith.Data
{
    /// <summary>
    /// 현재 진행 중인 게임의 런타임 데이터 보관소. 씬이 바뀌어도 유지된다.
    /// 타이틀에서 슬롯을 불러오면 Set으로 넣고, 게임 씬은 Current를 사용한다.
    /// </summary>
    public static class GameSession
    {
        private static PlayerData current;

        public static PlayerData Current
        {
            get
            {
                if (current == null) current = new PlayerData();
                return current;
            }
        }

        /// <summary>새 게임: 빈 플레이어 데이터로 시작.</summary>
        public static void StartNew()
        {
            current = new PlayerData();
        }

        public static void Set(PlayerData data)
        {
            current = data ?? new PlayerData();
        }
    }
}
