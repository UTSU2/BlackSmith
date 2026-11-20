using System;
using BlackSmith.Crafting;
using BlackSmith.Data;

namespace BlackSmith.Rank
{
    /// <summary>스킵 장비의 지급·강화·제작용 점수 조회(순수 로직, PlayerData 상태를 바꾼다).</summary>
    public static class SkipGearService
    {
        /// <summary>
        /// 등급이 해금 등급 이상인데 아직 없는 장비만 준다(Lv1, 켜짐). 이미 가진 장비는 건드리지 않아 여러 번 불러도 안전하다.
        /// </summary>
        public static void SyncGrants(PlayerData player, SkipGearRules rules)
        {
            if (player == null || rules == null) return;
            foreach (SkipGearKind kind in Enum.GetValues(typeof(SkipGearKind)))
            {
                if (player.Rank >= rules.GetUnlockRank(kind) && player.GetSkipGear(kind) == null)
                {
                    player.GrantSkipGear(kind);
                }
            }
        }

        /// <summary>재화로 한 단계 강화한다. 비용이 미정·최대 레벨·돈 부족이면 false이고 아무것도 바뀌지 않는다.</summary>
        public static bool TryUpgrade(PlayerData player, SkipGearKind kind, SkipGearRules rules)
        {
            if (player == null || rules == null) return false;
            SkipGearEntry entry = player.GetSkipGear(kind);
            if (entry == null) return false;

            int cost = rules.GetUpgradeCost(entry.Level);
            if (cost < 0 || !player.CanAfford(cost)) return false;
            if (!player.TrySpendMoney(cost)) return false;
            return player.SetSkipGearLevel(kind, entry.Level + 1);
        }

        /// <summary>그 단계를 건너뛰는 켜진 장비가 있으면 그 고정 점수를 돌려준다.</summary>
        public static bool TryGetSkipScore(PlayerData player, CraftStage stage, SkipGearRules rules, out int score)
        {
            score = 0;
            if (player == null || rules == null) return false;
            foreach (SkipGearKind kind in Enum.GetValues(typeof(SkipGearKind)))
            {
                if (kind.ToStage() != stage) continue;
                SkipGearEntry entry = player.GetSkipGear(kind);
                if (entry == null || !entry.Enabled) continue;
                score = rules.GetScore(entry.Level);
                return true;
            }
            return false;
        }
    }
}
