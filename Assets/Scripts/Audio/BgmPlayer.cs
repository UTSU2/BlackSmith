using UnityEngine;

namespace BlackSmith.Audio
{
    /// <summary>
    /// BGM 재생기. AudioSource 두 개로 곡을 크로스페이드하고, 볼륨은 SoundSettings.BgmVolume을 따른다.
    /// 씬마다 하나씩 두며 씬이 바뀌면 곡이 처음부터 다시 시작된다. (DontDestroyOnLoad를 쓰지 않는다)
    /// </summary>
    public class BgmPlayer : MonoBehaviour
    {
        [Tooltip("씬이 시작될 때 재생할 곡. 비워 두면 Play가 불릴 때까지 조용하다.")]
        [SerializeField] private AudioClip startClip;
        [Tooltip("곡이 바뀔 때 섞이는 시간(초). 임시값.")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.7f;

        private AudioSource current;
        private AudioSource previous;
        private float fade = 1f;

        private void Awake()
        {
            current = CreateSource();
            previous = CreateSource();
        }

        private void OnEnable()
        {
            SoundSettings.Changed += ApplyVolume;
        }

        private void OnDisable()
        {
            SoundSettings.Changed -= ApplyVolume;
        }

        private void Start()
        {
            if (startClip != null) Play(startClip);
        }

        /// <summary>곡을 바꾼다. 이미 같은 곡이 재생 중이면 아무것도 하지 않는다. 첫 곡은 바로 시작한다.</summary>
        public void Play(AudioClip clip)
        {
            if (clip == null || current == null) return;
            if (current.clip == clip && current.isPlaying) return;

            bool hadMusic = current.isPlaying;
            AudioSource swap = previous;
            previous = current;
            current = swap;

            current.clip = clip;
            current.Play();
            fade = hadMusic && fadeDuration > 0f ? 0f : 1f;
            if (!hadMusic) previous.Stop();
            ApplyVolume();
        }

        private void Update()
        {
            if (fade >= 1f) return;

            fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / fadeDuration);
            ApplyVolume();
            if (fade >= 1f) previous.Stop();
        }

        private void ApplyVolume()
        {
            if (current == null) return;
            float volume = SoundSettings.BgmVolume;
            current.volume = volume * fade;
            previous.volume = volume * (1f - fade);
        }

        private AudioSource CreateSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
