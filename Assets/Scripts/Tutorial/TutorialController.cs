using UnityEngine;
using BlackSmith.Crafting;
using BlackSmith.Data;
using BlackSmith.Items;
using BlackSmith.UI;

namespace BlackSmith.Tutorial
{
    /// <summary>튜토리얼 진행을 게임 이벤트와 팝업에 연결한다. 진행 규칙은 TutorialFlow가 갖는다.</summary>
    public class TutorialController : MonoBehaviour
    {
        [SerializeField] private CraftingController crafting;
        [SerializeField] private TutorialPopup popup;
        [Tooltip("제작 결과 팝업. 닫힐 때 '결과 팝업 닫힘'(CraftResultClosed) 단계를 넘긴다. 비워 두면 그 단계는 제작 완료 직후 바로 넘어간다.")]
        [SerializeField] private CraftResultPopup resultPopup;
        [Tooltip("튜토리얼 제작 중 왼쪽 상단에 미니게임 조작 안내를 보여 줄 상자. 비워 두면 안내 없음.")]
        [SerializeField] private MinigameGuidePopup guidePopup;

        private TutorialConfig config;
        private TutorialFlow flow;
        private PlayerData player;

        private bool completePending;

        public bool IsActive { get; private set; }

        private void Start()
        {
            Begin(GameSession.Current);
        }

        private void LateUpdate()
        {
            if (!completePending) return;
            completePending = false;
            if (player != null) player.CompleteTutorial();
        }

        private void OnDisable()
        {
            // 완료 대기 중에 꺼져도 보상을 놓치지 않게 먼저 처리한다.
            LateUpdate();
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>튜토리얼을 시작한다. 설정이 없거나 이미 끝난 저장이면 아무것도 하지 않는다.</summary>
        public void Begin(PlayerData playerData)
        {
            LateUpdate();
            Unsubscribe();
            IsActive = false;

            config = TutorialConfig.Load();
            if (config == null || playerData == null || !playerData.IsTutorialPending)
            {
                if (popup != null) popup.Hide();
                return;
            }

            player = playerData;
            flow = new TutorialFlow(config.Steps, player);
            // Start()가 첫 StepChanged를 발생시키므로 구독을 먼저 한다.
            flow.StepChanged += OnStepChanged;
            flow.Finished += OnFinished;
            player.OnInventoryChanged += OnInventoryChanged;
            if (crafting != null)
            {
                crafting.CraftStarted += OnCraftStarted;
                crafting.CraftFinished += OnCraftFinished;
                crafting.StageStarted += OnStageStarted;
            }
            if (resultPopup != null) resultPopup.Closed += OnResultPopupClosed;
            if (popup != null) popup.Confirmed += OnPopupConfirmed;

            IsActive = true;
            flow.Start();
        }

        public bool IsBuyAllowed(ItemData item)
        {
            return !IsActive || config == null || config.IsBuyAllowed(item);
        }

        public bool IsSellAllowed(ItemData item)
        {
            return !IsActive || config == null || config.IsSellAllowed(item);
        }

        private void Unsubscribe()
        {
            if (flow != null)
            {
                flow.StepChanged -= OnStepChanged;
                flow.Finished -= OnFinished;
            }
            if (player != null) player.OnInventoryChanged -= OnInventoryChanged;
            if (crafting != null)
            {
                crafting.CraftStarted -= OnCraftStarted;
                crafting.CraftFinished -= OnCraftFinished;
                crafting.StageStarted -= OnStageStarted;
            }
            if (resultPopup != null) resultPopup.Closed -= OnResultPopupClosed;
            if (popup != null) popup.Confirmed -= OnPopupConfirmed;
            if (guidePopup != null) guidePopup.Hide();
            flow = null;
            player = null;
        }

        private void OnStepChanged()
        {
            // 마지막 단계 뒤의 StepChanged는 Current가 null이다. 완료는 Finished에서 처리한다.
            if (flow == null || flow.Current == null) return;
            if (popup != null) popup.Show(flow.Current.Message, flow.Current.Condition == TutorialCondition.Confirm, flow.Current.PopupPosition);
        }

        // 판매는 아이템을 먼저 빼고(이때 이 이벤트가 온다) 그 다음에 판매금을 더한다. 여기서 바로 CompleteTutorial을 부르면
        // 판매금이 보상 뒤에 더해져 1000이 넘으므로, 같은 프레임의 판매 처리가 끝난 뒤(LateUpdate)에 완료한다.
        private void OnFinished()
        {
            completePending = true;
            IsActive = false;
            if (popup != null) popup.Show(config.CompletionMessage, true);
        }

        private void OnPopupConfirmed()
        {
            // 흐름이 끝난 뒤의 확인은 완료 안내를 닫는다.
            if (flow == null || flow.IsFinished)
            {
                if (popup != null) popup.Hide();
                return;
            }
            flow.NotifyConfirm();
        }

        private void OnInventoryChanged()
        {
            if (flow != null) flow.NotifyInventoryChanged();
        }

        private void OnCraftStarted()
        {
            if (flow != null) flow.NotifyCraftStarted();
        }

        // 제작 완료 단계는 바로 넘긴다. 다음 단계(숙련도 안내)가 결과 팝업과 동시에 보이고, 결과 팝업이 닫히면 넘어간다.
        private void OnCraftFinished(CraftResult result)
        {
            if (guidePopup != null) guidePopup.Hide();
            if (flow == null) return;
            flow.NotifyCraftFinished();
            // 결과 팝업이 없으면 기다릴 대상이 없으므로 안내 단계도 바로 넘긴다.
            if (resultPopup == null) flow.NotifyCraftResultClosed();
        }

        // 튜토리얼 제작 중에만 단계마다 조작 안내를 바꿔 보여 준다. 앞 단계가 끝나면 같은 프레임에 다음 단계가 시작되므로 바로 바뀐다.
        private void OnStageStarted(CraftStage stage)
        {
            if (guidePopup == null) return;
            if (!IsActive || flow == null || config == null)
            {
                guidePopup.Hide();
                return;
            }
            guidePopup.Show(config.GetStageGuide(stage));
        }

        private void OnResultPopupClosed()
        {
            if (flow != null) flow.NotifyCraftResultClosed();
        }
    }
}
