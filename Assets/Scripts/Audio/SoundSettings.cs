using System;
using UnityEngine;

namespace BlackSmith.Audio
{
    /// <summary>
    /// 소리 크기 설정(BGM / 효과음, 각 0~1). PlayerPrefs에 저장한다.
    /// 세이브 슬롯과 무관하고, Title에서 슬롯을 불러오기 전에도 적용돼야 해서 세이브 데이터에 넣지 않는다.
    /// 값이 바뀌면 Changed가 불리므로, 구독하는 쪽은 OnDisable에서 반드시 해제한다.
    /// </summary>
    public static class SoundSettings
    {
        private const string BgmKey = "Sound.BgmVolume";
        private const string SfxKey = "Sound.SfxVolume";

        /// <summary>처음 실행했을 때의 값. (임시값. 실제 밸런스는 들어 보고 조정)</summary>
        public const float DefaultBgmVolume = 0.7f;
        public const float DefaultSfxVolume = 1f;

        private static float bgmVolume = -1f;
        private static float sfxVolume = -1f;

        /// <summary>BGM이나 효과음 크기가 바뀌었을 때.</summary>
        public static event Action Changed;

        public static float BgmVolume
        {
            get
            {
                if (bgmVolume < 0f) bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmKey, DefaultBgmVolume));
                return bgmVolume;
            }
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, BgmVolume)) return;
                bgmVolume = value;
                PlayerPrefs.SetFloat(BgmKey, value);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static float SfxVolume
        {
            get
            {
                if (sfxVolume < 0f) sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, DefaultSfxVolume));
                return sfxVolume;
            }
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, SfxVolume)) return;
                sfxVolume = value;
                PlayerPrefs.SetFloat(SfxKey, value);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        // 에디터에서 Domain Reload를 끄고 플레이해도 이전 판의 캐시가 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            bgmVolume = -1f;
            sfxVolume = -1f;
            Changed = null;
        }
    }
}
