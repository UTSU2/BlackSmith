using UnityEngine;
using BlackSmith.Audio;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 미니게임 효과음 연결. CraftingController가 단계를 시작할 때 Bind로 알려 주면,
    /// 그 미니게임의 판정 이벤트와 상태를 듣고 SfxPlayer로 소리를 낸다. 미니게임 로직은 오디오를 모른다.
    /// 단계가 끝나거나 취소되면 Unbind로 이벤트 해제와 루프 정지를 한다.
    /// 클립이나 SfxPlayer가 연결되지 않았으면 조용히 넘어간다.
    /// </summary>
    public class CraftingSfx : MonoBehaviour
    {
        [SerializeField] private SfxPlayer player;

        private HeatMinigame heat;
        private ForgeMinigame forge;
        private HeatTreatMinigame heatTreat;
        private PolishMinigame polish;

        private SfxClips Clips => player != null ? player.Clips : null;

        public void Bind(HeatMinigame game)
        {
            Unbind();
            heat = game;
        }

        public void Bind(ForgeMinigame game)
        {
            Unbind();
            forge = game;
            forge.Judged += OnForgeJudged;
            forge.EmptyClicked += OnForgeEmptyClicked;
        }

        public void Bind(HeatTreatMinigame game)
        {
            Unbind();
            heatTreat = game;
            heatTreat.Judged += OnHeatTreatJudged;
        }

        public void Bind(PolishMinigame game)
        {
            Unbind();
            polish = game;
            polish.FlawCleared += OnFlawCleared;
        }

        /// <summary>듣던 미니게임을 놓고 루프 소리를 멈춘다. 연결된 게 없어도 안전하다.</summary>
        public void Unbind()
        {
            if (forge != null)
            {
                forge.Judged -= OnForgeJudged;
                forge.EmptyClicked -= OnForgeEmptyClicked;
            }
            if (heatTreat != null) heatTreat.Judged -= OnHeatTreatJudged;
            if (polish != null) polish.FlawCleared -= OnFlawCleared;

            heat = null;
            forge = null;
            heatTreat = null;
            polish = null;
            if (player != null) player.StopAllLoops();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Update()
        {
            SfxClips clips = Clips;
            if (clips == null) return;

            // 루프는 상태를 보고 켜고 끈다. 누름이 풀리거나 시간이 끝나면 바로 멈춘다.
            if (heat != null)
            {
                SetLoop(clips.heatHold, heat.IsRunning && heat.IsHolding);
            }
            if (polish != null)
            {
                SetLoop(clips.sandLoop, polish.IsSanding);
            }
        }

        private void SetLoop(AudioClip clip, bool on)
        {
            if (on) player.StartLoop(clip);
            else player.StopLoop(clip);
        }

        private void OnForgeJudged(ForgeMinigame.CircleState state)
        {
            SfxClips clips = Clips;
            if (clips == null) return;

            switch (state)
            {
                case ForgeMinigame.CircleState.Perfect: player.Play(clips.forgePerfect); break;
                case ForgeMinigame.CircleState.Good: player.Play(clips.forgeGood); break;
                case ForgeMinigame.CircleState.Miss: player.Play(clips.forgeMiss); break;
            }
        }

        // 빈 곳을 눌렀을 때도 헛친 소리로 forge_miss를 쓴다. (별도 클립은 만들지 않기로 함)
        private void OnForgeEmptyClicked()
        {
            SfxClips clips = Clips;
            if (clips != null) player.Play(clips.forgeMiss);
        }

        private void OnHeatTreatJudged(float completeness)
        {
            SfxClips clips = Clips;
            if (clips == null) return;

            if (completeness >= 1f) player.Play(clips.quenchFull);
            else if (completeness > 0f) player.Play(clips.quenchHalf);
            else player.Play(clips.quenchFail);
        }

        private void OnFlawCleared()
        {
            SfxClips clips = Clips;
            if (clips != null) player.Play(clips.flawCleared);
        }
    }
}
