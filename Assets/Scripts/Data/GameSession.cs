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

        /// <summary>
        /// 새 게임: 빈 플레이어 데이터로 시작한다. 튜토리얼이 아직 없으므로 시작하자마자 건너뛴 것으로 처리해 보상을 바로 준다.
        /// 튜토리얼이 생기면 StartTutorial까지만 하고, CompleteTutorial은 튜토리얼을 마치거나 건너뛰는 시점에서 부른다.
        /// </summary>
        public static void StartNew()
        {
            current = new PlayerData();
            current.StartTutorial();
            current.CompleteTutorial();

            // 시작 지급금은 저장하지 않은 변경으로 보지 않는다. (아무것도 안 하고 타이틀로 돌아갈 때 확인 창이 뜨지 않게)
            current.MarkSaved();
        }

        public static void Set(PlayerData data)
        {
            current = data ?? new PlayerData();
        }
    }
}
