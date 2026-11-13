using UnityEngine;
using UnityEngine.UI;

namespace BlackSmith.UI
{
    /// <summary>
    /// 감시 대상 패널이 하나라도 켜져 있는 동안 화면 전체의 클릭을 막는다.
    /// 전체 화면을 덮는 투명한 Image(Raycast Target)에 붙이고, 패널들 바로 앞(HUD 버튼 위, 패널 아래)에 둔다.
    /// 패널은 열릴 때 맨 앞으로 오므로 패널 안쪽 조작은 막히지 않는다.
    /// 월드의 클릭 대상(모루)은 UI가 먼저 클릭을 가져가므로 함께 막힌다. 클릭해도 패널을 닫지는 않는다.
    /// 패널 스크립트를 건드리지 않도록 패널의 활성 상태만 본다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class PanelInputBlocker : MonoBehaviour
    {
        [Tooltip("하나라도 켜져 있으면 클릭을 막을 패널들.")]
        [SerializeField] private GameObject[] watchedPanels;

        private Image blocker;

        private void Awake()
        {
            blocker = GetComponent<Image>();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            bool anyOpen = false;
            if (watchedPanels != null)
            {
                foreach (GameObject panel in watchedPanels)
                {
                    if (panel != null && panel.activeInHierarchy)
                    {
                        anyOpen = true;
                        break;
                    }
                }
            }

            // Image를 끄면 레이캐스트도 사라진다. 오브젝트는 꺼 두지 않아 Update가 계속 돈다.
            if (blocker.enabled != anyOpen) blocker.enabled = anyOpen;
        }
    }
}
