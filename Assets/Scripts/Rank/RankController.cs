using System;
using UnityEngine;
using BlackSmith.Crafting;
using BlackSmith.Data;
using BlackSmith.Proficiency;

namespace BlackSmith.Rank
{
    /// <summary>
    /// 등급 진행을 게임 이벤트에 연결하는 씬 오브젝트. 규칙은 BlacksmithRankRules가 갖는다.
    /// 승급 퀘스트가 생기기 전까지(출하 3단계) allowDirectPromotion이 켜져 있으면 조건을 채우는 즉시 승급한다(개발용 임시).
    /// 튜토리얼 중에는 승급하지 않는다.
    /// </summary>
    public class RankController : MonoBehaviour
    {
        [SerializeField] private CraftingController crafting;
        [Tooltip("개발용 임시: 조건을 채우면 퀘스트 없이 바로 승급. 승급 퀘스트를 붙일 때 제거한다.")]
        [SerializeField] private bool allowDirectPromotion = true;

        private BlacksmithRankRules rules;
        private ProficiencyRules proficiencyRules;
        private SkipGearRules gearRules;
        private PlayerData subscribed;

        /// <summary>진행도나 등급이 바뀌었을 때(제작 종료, 승급).</summary>
        public event Action Changed;

        private void OnEnable()
        {
            if (crafting != null) crafting.CraftFinished += OnCraftFinished;
        }

        private void Start()
        {
            BlacksmithRankSettings settings = BlacksmithRankSettings.Load();
            if (settings == null) Debug.LogWarning($"[Rank] 등급 설정({BlacksmithRankSettings.ResourcePath})이 없어 승급 없이 진행합니다.");
            else rules = new BlacksmithRankRules(settings);

            ProficiencySettings proficiency = ProficiencySettings.Load();
            if (proficiency != null) proficiencyRules = new ProficiencyRules(proficiency);

            SkipGearSettings gear = SkipGearSettings.Load();
            if (gear == null) Debug.LogWarning($"[Rank] 스킵 장비 설정({SkipGearSettings.ResourcePath})이 없어 장비를 지급하지 않습니다.");
            else gearRules = new SkipGearRules(gear);

            Subscribe(GameSession.Current);
            SkipGearService.SyncGrants(GameSession.Current, gearRules);
            Evaluate();
        }

        private void OnDisable()
        {
            if (crafting != null) crafting.CraftFinished -= OnCraftFinished;
            Subscribe(null);
        }

        /// <summary>현재 플레이어의 다음 등급까지 진행도. 설정이 없으면 다음 등급 없음으로 돌려준다.</summary>
        public RankProgress GetProgress()
        {
            PlayerData player = GameSession.Current;
            if (player == null || rules == null) return new RankProgress(false, BlacksmithRank.Apprentice, 0, -1, 0, -1);
            return rules.Evaluate(player.Rank, player.UnlockedRecipeCount, RankCounter.CountMasters(player, proficiencyRules));
        }

        private void OnCraftFinished(CraftResult result)
        {
            Evaluate();
        }

        private void OnRankChanged(BlacksmithRank rank)
        {
            SkipGearService.SyncGrants(subscribed, gearRules);
            Changed?.Invoke();
        }

        private void Subscribe(PlayerData player)
        {
            if (subscribed == player) return;
            if (subscribed != null) subscribed.OnRankChanged -= OnRankChanged;
            subscribed = player;
            if (subscribed != null) subscribed.OnRankChanged += OnRankChanged;
        }

        private void Evaluate()
        {
            PlayerData player = GameSession.Current;
            Subscribe(player);
            if (player != null && allowDirectPromotion && !player.IsTutorialPending)
            {
                // 한 번에 여러 등급의 조건을 채웠으면 이어서 올린다.
                while (true)
                {
                    RankProgress progress = GetProgress();
                    if (!progress.HasNext || !progress.IsMet) break;
                    if (!player.SetRank(progress.Next)) break;
                }
            }
            Changed?.Invoke();
        }
    }
}
