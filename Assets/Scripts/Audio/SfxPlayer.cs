using System.Collections.Generic;
using UnityEngine;

namespace BlackSmith.Audio
{
    /// <summary>
    /// 효과음 재생기. 한 번 울리는 소리와 루프 소리를 재생하고, 볼륨은 SoundSettings.SfxVolume을 따른다.
    /// 루프는 클립마다 전용 AudioSource를 하나씩 만든다.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private SfxClips clips;

        private AudioSource oneShotSource;
        private readonly Dictionary<AudioClip, AudioSource> loopSources = new Dictionary<AudioClip, AudioSource>();

        public SfxClips Clips => clips;

        private void Awake()
        {
            oneShotSource = CreateSource(false);
        }

        private void OnEnable()
        {
            SoundSettings.Changed += OnVolumeChanged;
        }

        private void OnDisable()
        {
            SoundSettings.Changed -= OnVolumeChanged;
            StopAllLoops();
        }

        /// <summary>한 번 재생한다. 클립이 없으면(미연결) 아무것도 하지 않는다.</summary>
        public void Play(AudioClip clip)
        {
            if (clip == null || oneShotSource == null) return;
            oneShotSource.PlayOneShot(clip, SoundSettings.SfxVolume);
        }

        /// <summary>루프를 시작한다. 이미 재생 중이면 그대로 둔다.</summary>
        public void StartLoop(AudioClip clip)
        {
            if (clip == null) return;

            if (!loopSources.TryGetValue(clip, out AudioSource source))
            {
                source = CreateSource(true);
                source.clip = clip;
                loopSources.Add(clip, source);
            }
            if (source.isPlaying) return;

            source.volume = SoundSettings.SfxVolume;
            source.Play();
        }

        public void StopLoop(AudioClip clip)
        {
            if (clip == null) return;
            if (loopSources.TryGetValue(clip, out AudioSource source) && source.isPlaying) source.Stop();
        }

        public void StopAllLoops()
        {
            foreach (AudioSource source in loopSources.Values)
            {
                if (source != null && source.isPlaying) source.Stop();
            }
        }

        private AudioSource CreateSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private void OnVolumeChanged()
        {
            foreach (AudioSource source in loopSources.Values)
            {
                if (source != null) source.volume = SoundSettings.SfxVolume;
            }
        }
    }
}
