using System;
using System.Text;
using UnityEngine;
using BlackSmith.UI;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 모루 클릭 → 재료 투입 패널 → 제작 시작 → 단계별 미니게임 진행의 흐름을 잇는다.
    /// 제작 중에는 모루 클릭을 무시한다.
    /// 단계는 레시피의 stages 순서대로 진행하며, 뷰가 연결되지 않았거나 아직 구현되지 않은 단계는
    /// 건너뛰고 결과에 Skipped로 기록한다.
    /// 마지막에 CraftResult를 알릴 뿐, 완성도 → 등급 계산과 결과 지급은 미정이라 하지 않는다.
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

        [Header("미니게임 설정 (기획이 정해지기 전까지는 모두 임시값)")]
        [SerializeField] private HeatSettings heatSettings = new HeatSettings();
        [SerializeField] private ForgeSettings forgeSettings = new ForgeSettings();
        [SerializeField] private HeatTreatSettings heatTreatSettings = new HeatTreatSettings();
        [SerializeField] private PolishSettings polishSettings = new PolishSettings();

        private CraftRecipeData recipe;
        private int difficulty;
        private int stageIndex;
        private CraftResult result;
        private ICraftStageGame activeGame;
        private Action hideActiveView;
        private CraftStage activeStage;

        public bool IsCrafting { get; private set; }

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

        private void OnStartRequested(CraftRecipeData startedRecipe, int startedDifficulty)
        {
            if (IsCrafting) return;

            IsCrafting = true;
            recipe = startedRecipe;
            difficulty = startedDifficulty;
            stageIndex = -1;
            result = new CraftResult(recipe, difficulty);
            AdvanceStage();
        }

        private void AdvanceStage()
        {
            stageIndex++;
            if (stageIndex >= recipe.Stages.Count)
            {
                FinishCrafting();
                return;
            }

            activeStage = recipe.Stages[stageIndex];
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
                    heatView.Show(heat);
                    hideActiveView = heatView.Hide;
                    Run(heat);
                    return true;

                case CraftStage.Forge:
                    if (HasNoView(forgeView, "forgeView")) return false;
                    ForgeMinigame forge = new ForgeMinigame(forgeSettings);
                    forgeView.Show(forge);
                    hideActiveView = forgeView.Hide;
                    Run(forge);
                    return true;

                case CraftStage.HeatTreat:
                    if (HasNoView(heatTreatView, "heatTreatView")) return false;
                    HeatTreatMinigame heatTreat = new HeatTreatMinigame(heatTreatSettings);
                    heatTreatView.Show(heatTreat);
                    hideActiveView = heatTreatView.Hide;
                    Run(heatTreat);
                    return true;

                case CraftStage.Polish:
                    if (HasNoView(polishView, "polishView")) return false;
                    PolishMinigame polish = new PolishMinigame(polishSettings);
                    polishView.Show(polish);
                    hideActiveView = polishView.Hide;
                    Run(polish);
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
            HideActiveView();

            result.AddStage(new CraftStageResult(activeStage, completeness, false));
            AdvanceStage();
        }

        private void FinishCrafting()
        {
            CraftResult finished = result;
            IsCrafting = false;
            recipe = null;
            result = null;

            // TODO: 완성도 → 등급 계산과 완성품 지급은 미정이라 하지 않는다.
            Debug.Log($"[Crafting] 제작 종료: '{finished.Recipe.Id}' / 난이도 {finished.Difficulty} / {Describe(finished)}");
            CraftFinished?.Invoke(finished);
        }

        private void AbortCrafting()
        {
            if (activeGame != null)
            {
                activeGame.Cancel();
                activeGame.Completed -= OnStageCompleted;
                activeGame = null;
            }
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
