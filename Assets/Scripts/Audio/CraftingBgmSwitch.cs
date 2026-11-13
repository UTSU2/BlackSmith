using UnityEngine;
using BlackSmith.Crafting;

namespace BlackSmith.Audio
{
    /// <summary>
    /// 제작 BGM 전환. 제작이 시작되면 제작용 곡으로, 모든 단계가 끝나면 기본 곡으로 되돌린다.
    /// 단계 사이에서는 곡을 바꾸지 않는다. CraftingController는 BgmPlayer를 모르고 이벤트만 알린다.
    /// </summary>
    public class CraftingBgmSwitch : MonoBehaviour
    {
        [SerializeField] private CraftingController controller;
        [SerializeField] private BgmPlayer bgm;
        [SerializeField] private AudioClip defaultClip;
        [SerializeField] private AudioClip craftingClip;

        private void OnEnable()
        {
            if (controller == null) return;
            controller.CraftStarted += OnCraftStarted;
            controller.CraftFinished += OnCraftFinished;
        }

        private void OnDisable()
        {
            if (controller == null) return;
            controller.CraftStarted -= OnCraftStarted;
            controller.CraftFinished -= OnCraftFinished;
        }

        private void OnCraftStarted()
        {
            if (bgm != null) bgm.Play(craftingClip);
        }

        private void OnCraftFinished(CraftResult result)
        {
            if (bgm != null) bgm.Play(defaultClip);
        }
    }
}
