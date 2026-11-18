using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using BlackSmith.Data;
using BlackSmith.Items;
using BlackSmith.Proficiency;
using BlackSmith.UI;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 모루 클릭 → 재료 투입 패널 → 제작 시작 → 단계별 미니게임 진행의 흐름을 잇는다.
    /// 제작 중에는 모루 클릭을 무시한다.
    /// 단계는 제작 요청의 단계(레시피 또는 알 수 없는 제작 설정) 순서대로 진행하며, 뷰가 연결되지 않았거나 아직 구현되지 않은 단계는
    /// 건너뛰고 결과에 Skipped로 기록한다.
    /// 마지막에 단계 점수 → 품질을 계산해 완성품을 소지품에 지급하고 CraftResult를 알린다.
    /// </summary>
    public class CraftingController : MonoBehaviour
    {
        [SerializeField] private AnvilInteractable anvil;
        [SerializeField] private CraftingPanel craftingPanel;

        [Header("미니게임 화면")]
        [SerializeField] private HeatMinigameView heatView;
        [SerializeField] private ForgeMinigameView forgeView;
        [SerializeField] private HeatTreatMinigameView heatTreatView;
        [SerializeField] private PolishMinigameView polishView;

        [Header("효과음 (비워 두면 소리 없이 진행)")]
        [SerializeField] private CraftingSfx sfx;

        [Header("미니게임 설정 (난이도 1 기준값. 난이도별 변화는 docs/GameDesign.md §4)")]
        [SerializeField] private HeatSettings heatSettings = new HeatSettings();
        [SerializeField] private ForgeSettings forgeSettings = new ForgeSettings();
        [SerializeField] private HeatTreatSettings heatTreatSettings = new HeatTreatSettings();
        [SerializeField] private PolishSettings polishSettings = new PolishSettings();

        private IReadOnlyList<CraftStage> stages;
        private int difficulty;
        private int stageIndex;
        private CraftResult result;
        private ICraftStageGame activeGame;
        private Action hideActiveView;
        private CraftStage activeStage;
        private ProficiencyRules proficiencyRules;
        private bool proficiencyLoaded;

        public bool IsCrafting { get; private set; }

        /// <summary>제작이 시작됐을 때(재료가 소모되고 첫 단계로 들어가기 직전) 호출. BGM 전환용이다.</summary>
        public event Action CraftStarted;

        /// <summary>모든 단계가 끝났을 때 호출.</summary>
        public event Action<CraftResult> CraftFinished;

        private void Awake()
        {
            HideAllViews();
        }

        private void OnEnable()
        {
            if (anvil != null) anvil.Clicked += OnAnvilClicked;
            if (craftingPanel != null) craftingPanel.StartRequested += OnStartRequested;
        }

        private void OnDisable()
        {
            if (anvil != null) anvil.Clicked -= OnAnvilClicked;
            if (craftingPanel != null) craftingPanel.StartRequested -= OnStartRequested;
            AbortCrafting();
        }

        private void Update()
        {
            // Tick 안에서 단계가 끝나 다음 게임으로 바뀔 수 있으므로 지역 변수로 한 번만 부른다.
            ICraftStageGame game = activeGame;
            if (game != null) game.Tick(Time.deltaTime);
        }

        private void OnAnvilClicked()
        {
            if (IsCrafting || craftingPanel == null || craftingPanel.IsOpen) return;
            craftingPanel.Open();
        }

        private void OnStartRequested(CraftRequest request)
        {
            if (IsCrafting) return;

            IsCrafting = true;
            stages = request.Stages;
            difficulty = request.Difficulty;
            stageIndex = -1;
            result = new CraftResult(request);
            CraftStarted?.Invoke();
            AdvanceStage();
        }

        private void AdvanceStage()
        {
            stageIndex++;
            if (stageIndex >= stages.Count)
            {
                FinishCrafting();
                return;
            }

            activeStage = stages[stageIndex];
            if (!TryStartStage(activeStage))
            {
                result.AddStage(new CraftStageResult(activeStage, 0f, true));
                AdvanceStage();
            }
        }

        /// <summary>단계의 미니게임을 시작한다. 시작할 수 없으면(미구현, 뷰 없음) false.</summary>
        private bool TryStartStage(CraftStage stage)
        {
            switch (stage)
            {
                case CraftStage.Heat:
                    if (HasNoView(heatView, "heatView")) return false;
                    HeatMinigame heat = new HeatMinigame(heatSettings);
                    // View가 Begin 이후의 값(랜덤 목표, 난이도 반영 값)을 읽도록 Run을 먼저 한다.
                    hideActiveView = heatView.Hide;
                    Run(heat);
                    if (sfx != null) sfx.Bind(heat);
                    heatView.Show(heat);
                    return true;

                case CraftStage.Forge:
                    if (HasNoView(forgeView, "forgeView")) return false;
                    ForgeMinigame forge = new ForgeMinigame(forgeSettings);
                    hideActiveView = forgeView.Hide;
                    Run(forge);
                    if (sfx != null) sfx.Bind(forge);
                    forgeView.Show(forge);
                    return true;

                case CraftStage.HeatTreat:
                    if (HasNoView(heatTreatView, "heatTreatView")) return false;
                    HeatTreatMinigame heatTreat = new HeatTreatMinigame(heatTreatSettings);
                    hideActiveView = heatTreatView.Hide;
                    Run(heatTreat);
                    if (sfx != null) sfx.Bind(heatTreat);
                    heatTreatView.Show(heatTreat);
                    return true;

                case CraftStage.Polish:
                    if (HasNoView(polishView, "polishView")) return false;
                    PolishMinigame polish = new PolishMinigame(polishSettings);
                    hideActiveView = polishView.Hide;
                    Run(polish);
                    if (sfx != null) sfx.Bind(polish);
                    polishView.Show(polish);
                    return true;

                default:
                    // 4개 단계가 모두 구현됐지만, enum에 새 단계가 추가되면 여기로 온다. 건너뛰고 다음 단계로 간다.
                    Debug.Log($"[Crafting] 아직 구현되지 않은 단계라 건너뜁니다: {stage}");
                    return false;
            }
        }

        private static bool HasNoView(UnityEngine.Object view, string fieldName)
        {
            if (view != null) return false;
            Debug.LogWarning($"[Crafting] {fieldName}가 연결되지 않아 이 단계를 건너뜁니다.");
            return true;
        }

        private void Run(ICraftStageGame game)
        {
            activeGame = game;
            game.Completed += OnStageCompleted;
            game.Begin(difficulty);
        }

        private void OnStageCompleted(float completeness)
        {
            if (activeGame != null) activeGame.Completed -= OnStageCompleted;
            activeGame = null;
            if (sfx != null) sfx.Unbind();
            HideActiveView();

            result.AddStage(new CraftStageResult(activeStage, completeness, false));
            AdvanceStage();
        }

        private void FinishCrafting()
        {
            CraftResult finished = result;
            IsCrafting = false;
            stages = null;
            result = null;

            GrantResult(finished);
            CraftFinished?.Invoke(finished);
        }

        /// <summary>
        /// 완성품을 소지품에 지급한다. 등급은 레시피 결과물의 등급이고, 품질은 단계 점수 합계에 숙련도 보너스를 더해 정한다.
        /// 재료 합산 가격은 제작을 시작할 때 계산해 둔 값을 쓴다 (재료는 이미 소모됐기 때문).
        /// 숙련도는 보너스 적용 전 총점으로 쌓는다. 결과는 CraftResult에 기록해 결과 팝업이 읽는다. (docs/V2.md §2)
        /// </summary>
        private void GrantResult(CraftResult finished)
        {
            CraftRequest request = finished.Request;
            CraftedItemData product = request.Product;
            ItemGrade grade = product.DefaultGrade;
            PlayerData player = GameSession.Current;
            // 알 수 없는 제작은 레시피가 아니므로 숙련도·도감 해금이 없다.
            string recipeId = request.IsUnknown ? null : finished.Recipe.Id;

            ProficiencyRules rules = request.IsUnknown ? null : GetProficiencyRules();
            ProficiencyState before = rules != null && player != null
                ? rules.Evaluate(player.GetProficiencyPoints(recipeId), grade) : null;
            decimal bonus = before != null ? before.Bonus : 0m;

            decimal rawScore = CraftScoring.CalculateScore(finished.Stages);
            decimal score = rules != null ? rules.ApplyBonus(rawScore, bonus) : rawScore;
            ItemQuality quality = ItemQualityInfo.FromScore(score);
            ItemVariant variant = new ItemVariant(grade, quality, request.IngredientValue);
            finished.SetOutcome(product, grade, quality, rawScore, score, bonus);

            bool granted = player != null && player.AddItem(product.Id, 1, variant);

            // 제작을 마치면 품질과 관계없이 도감에 해금한다(레시피가 있을 때만).
            if (!request.IsUnknown && player != null) finished.SetUnlocked(player.UnlockRecipe(recipeId));

            if (before != null)
            {
                int gain = player.AddProficiencyPoints(recipeId, rules.CalculateGain(rawScore), rules.GetMaxPoints(grade));
                ProficiencyState after = rules.Evaluate(player.GetProficiencyPoints(recipeId), grade);
                finished.SetProficiency(before, after, gain);
            }

            ProficiencyState now = finished.ProficiencyAfter;
            Debug.Log($"[Crafting] 제작 종료: {(request.IsUnknown ? "알 수 없는 제작" : "'" + recipeId + "'")} / 난이도 {finished.Difficulty} / {Describe(finished)}"
                + $" / 점수 {rawScore:0.0} + 보너스 {bonus:0.0} → {score:0.0} → {grade} {ItemQualityInfo.DisplayName(quality)}"
                + $" / 재료 합산 {request.IngredientValue}G / 지급 {(granted ? "완료" : "실패")}" + (finished.NewlyUnlocked ? " / 도감 해금" : "")
                + (now != null ? $" / 숙련도 +{finished.ProficiencyGain} → {now.Label} ({now.Points})" : (request.IsUnknown ? " / 숙련도 없음(레시피 아님)" : " / 숙련도 설정 없음")));
        }

        // 설정 에셋은 처음 필요할 때 한 번 불러온다. 없으면 숙련도 없이 진행한다.
        private ProficiencyRules GetProficiencyRules()
        {
            if (proficiencyLoaded) return proficiencyRules;
            proficiencyLoaded = true;
            ProficiencySettings settings = ProficiencySettings.Load();
            if (settings == null) Debug.LogWarning($"[Crafting] 숙련도 설정({ProficiencySettings.ResourcePath})이 없어 숙련도 없이 진행합니다.");
            else proficiencyRules = new ProficiencyRules(settings);
            return proficiencyRules;
        }

        private void AbortCrafting()
        {
            if (activeGame != null)
            {
                activeGame.Cancel();
                activeGame.Completed -= OnStageCompleted;
                activeGame = null;
            }
            if (sfx != null) sfx.Unbind();
            HideAllViews();
            IsCrafting = false;
        }

        private void HideActiveView()
        {
            Action hide = hideActiveView;
            hideActiveView = null;
            if (hide != null) hide();
        }

        private void HideAllViews()
        {
            hideActiveView = null;
            if (heatView != null) heatView.Hide();
            if (forgeView != null) forgeView.Hide();
            if (heatTreatView != null) heatTreatView.Hide();
            if (polishView != null) polishView.Hide();
        }

        private static string Describe(CraftResult finished)
        {
            StringBuilder text = new StringBuilder();
            foreach (CraftStageResult stage in finished.Stages)
            {
                if (text.Length > 0) text.Append(", ");
                text.Append(stage.Stage).Append(' ');
                text.Append(stage.Skipped ? "건너뜀" : stage.Completeness.ToString("P0"));
            }
            return text.Length == 0 ? "단계 없음" : text.ToString();
        }
    }
}
