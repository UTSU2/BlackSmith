using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BlackSmith.Audio;
using BlackSmith.Data;

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

        [Header("타이틀로 돌아가기 (Game 씬에서만 연결. 비워 두면 동작하지 않는다)")]
        [SerializeField] private Button titleButton;
        [Tooltip("저장하지 않은 변경이 있을 때만 뜨는 확인 창. 기본은 꺼 둔다.")]
        [SerializeField] private GameObject confirmPopup;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        [SerializeField] private string titleSceneName = "TItle";

        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (titleButton != null) titleButton.onClick.AddListener(OnTitleClicked);
            if (confirmYesButton != null) confirmYesButton.onClick.AddListener(GoToTitle);
            if (confirmNoButton != null) confirmNoButton.onClick.AddListener(CloseConfirm);

            Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = font;
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            if (titleButton != null) titleButton.onClick.RemoveListener(OnTitleClicked);
            if (confirmYesButton != null) confirmYesButton.onClick.RemoveListener(GoToTitle);
            if (confirmNoButton != null) confirmNoButton.onClick.RemoveListener(CloseConfirm);
        }

        public void Open()
        {
            // 처음 켜질 때 Awake가 불리므로 켠 뒤에 값을 채운다.
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            CloseConfirm();

            Show(bgmSlider, bgmValueLabel, SoundSettings.BgmVolume);
            Show(sfxSlider, sfxValueLabel, SoundSettings.SfxVolume);
        }

        public void Close()
        {
            CloseConfirm();
            gameObject.SetActive(false);
        }

        // 저장 이후 금액·소지품이 바뀌지 않았으면 바로 이동하고, 바뀌었으면 확인 창을 띄운다.
        private void OnTitleClicked()
        {
            if (GameSession.Current.HasUnsavedChanges && confirmPopup != null) confirmPopup.SetActive(true);
            else GoToTitle();
        }

        private void GoToTitle()
        {
            SceneManager.LoadScene(titleSceneName);
        }

        private void CloseConfirm()
        {
            if (confirmPopup != null) confirmPopup.SetActive(false);
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
