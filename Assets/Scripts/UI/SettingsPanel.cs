using UnityEngine;
using UnityEngine.UI;
using BlackSmith.Audio;

namespace BlackSmith.UI
{
    /// <summary>
    /// 설정 패널. BGM / 효과음 소리 크기를 슬라이더로 조절한다. 값은 바꾸는 즉시 적용되고 SoundSettings가 저장한다.
    /// Title과 Game 씬에 각각 하나씩 두고 같은 컴포넌트를 쓴다. 게임을 일시정지하지는 않는다.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [Tooltip("슬라이더 옆에 0~100으로 보여줄 값. 비워 두면 표시하지 않는다.")]
        [SerializeField] private Text bgmValueLabel;
        [SerializeField] private Text sfxValueLabel;
        [SerializeField] private Button closeButton;

        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);

            Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        }

        public void Open()
        {
            // 처음 켜질 때 Awake가 불리므로 켠 뒤에 값을 채운다.
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            Show(bgmSlider, bgmValueLabel, SoundSettings.BgmVolume);
            Show(sfxSlider, sfxValueLabel, SoundSettings.SfxVolume);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void OnBgmChanged(float value)
        {
            SoundSettings.BgmVolume = value;
            SetLabel(bgmValueLabel, value);
        }

        private void OnSfxChanged(float value)
        {
            SoundSettings.SfxVolume = value;
            SetLabel(sfxValueLabel, value);
        }

        // 슬라이더를 저장된 값으로 맞출 때는 다시 저장하지 않도록 알림 없이 설정한다.
        private static void Show(Slider slider, Text label, float value)
        {
            if (slider != null) slider.SetValueWithoutNotify(value);
            SetLabel(label, value);
        }

        private static void SetLabel(Text label, float value)
        {
            if (label != null) label.text = Mathf.RoundToInt(value * 100f).ToString();
        }
    }
}
