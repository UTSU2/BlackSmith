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
            }
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
            }
            if (popup != null) popup.Confirmed -= OnPopupConfirmed;
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

        private void OnCraftFinished(CraftResult result)
        {
            if (flow != null) flow.NotifyCraftFinished();
        }
    }
}
