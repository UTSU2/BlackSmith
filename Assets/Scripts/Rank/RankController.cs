using System;
using UnityEngine;
using BlackSmith.Crafting;
using BlackSmith.Data;
using BlackSmith.Proficiency;

namespace BlackSmith.Rank
{
    /// <summary>
    /// 등급 진행을 게임 이벤트에 연결하는 씬 오브젝트. 규칙은 BlacksmithRankRules가 갖는다.
    /// 승급 조건을 채우면 퀘스트(RankQuestSettings)가 자동으로 시작되고, 제작 결과가 퀘스트를 클리어하면 한 단계 승급한다.
    /// 튜토리얼 중에는 승급하지 않는다.
    /// </summary>
    public class RankController : MonoBehaviour
    {
        [SerializeField] private CraftingController crafting;

        private BlacksmithRankRules rules;
        private ProficiencyRules proficiencyRules;
        private SkipGearRules gearRules;
        private RankQuestRules questRules;
        private PlayerData subscribed;

        /// <summary>진행도나 등급이 바뀌었을 때(제작 종료, 승급).</summary>
        public event Action Changed;

        /// <summary>승급했을 때(이전 등급, 새 등급).</summary>
        public event Action<BlacksmithRank, BlacksmithRank> Promoted;

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

            RankQuestSettings quest = RankQuestSettings.Load();
            if (quest == null) Debug.LogWarning($"[Rank] 승급 퀘스트 설정({RankQuestSettings.ResourcePath})이 없어 승급할 수 없습니다.");
            questRules = new RankQuestRules(quest);

            Subscribe(GameSession.Current);
            SkipGearService.SyncGrants(GameSession.Current, gearRules);
            Subscribe(GameSession.Current);
            Changed?.Invoke();
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

        /// <summary>현재 승급 퀘스트 상태. 설정이 없으면 다음 등급이 없는 것으로 본다.</summary>
        public RankQuestState GetQuestState()
        {
            if (questRules == null || GameSession.Current == null) return RankQuestState.None;
            return questRules.GetState(GetProgress());
        }

        /// <summary>진행 중인 퀘스트(Active)일 때만 그 내용을 돌려준다.</summary>
        public bool TryGetActiveQuest(out RankQuestSettings.QuestEntry entry)
        {
            entry = null;
            return questRules != null && GameSession.Current != null && questRules.TryGetActive(GetProgress(), out entry);
        }

        private void OnCraftFinished(CraftResult result)
        {
            HandleCraftResult(result);
        }

        /// <summary>
        /// 제작 결과가 승급 퀘스트를 클리어하면 한 단계 승급한다. 튜토리얼 중, 알 수 없는 제작품, 조건 미달, 퀘스트가 비어 있으면 승급하지 않는다.
        /// </summary>
        public void HandleCraftResult(CraftResult result)
        {
            PlayerData player = GameSession.Current;
            Subscribe(player);
            if (player != null && result != null && questRules != null && !player.IsTutorialPending)
            {
                RankProgress progress = GetProgress();
                string recipeId = result.Recipe != null ? result.Recipe.Id : null;
                if (progress.HasNext && progress.IsMet && questRules.IsCleared(progress.Next, recipeId, result.IsUnknown, result.Quality))
                {
                    BlacksmithRank from = player.Rank;
                    if (player.SetRank(progress.Next)) Promoted?.Invoke(from, progress.Next);
                }
            }
            Changed?.Invoke();
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
    }
}
