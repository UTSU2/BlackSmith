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
        [Tooltip("보이고 숨기는 연출. Title 씬에서만 연결한다. 비워 두면 즉시 켜고 끈다(Game 씬).")]
        [SerializeField] private SaveLoadPanelTransition transition;

        [Header("화면 (비워 두면 해당 항목은 동작하지 않는다)")]
        [Tooltip("전체화면 선택 버튼. 선택된 쪽은 눌림 스프라이트로 보여준다.")]
        [SerializeField] private Button fullscreenButton;
        [SerializeField] private Button windowedButton;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite buttonPressedSprite;
        [Tooltip("창 모드 해상도를 한 칸씩 바꾸는 화살표 버튼과 현재 해상도 글자.")]
        [SerializeField] private Button resolutionPrevButton;
        [SerializeField] private Button resolutionNextButton;
        [SerializeField] private Text resolutionLabel;

        [Header("타이틀로 돌아가기 (Game 씬에서만 연결. 비워 두면 동작하지 않는다)")]
        [SerializeField] private Button titleButton;
        [Tooltip("저장하지 않은 변경이 있을 때만 뜨는 확인 창. 기본은 꺼 둔다.")]
        [SerializeField] private GameObject confirmPopup;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        [SerializeField] private string titleSceneName = "TItle";

        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>열고 닫는 연출이 재생 중인가. 타이틀 메뉴가 다른 패널 연출과 겹치지 않게 확인하는 데 쓴다.</summary>
        public bool IsTransitionPlaying => transition != null && transition.IsPlaying;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (fullscreenButton != null) fullscreenButton.onClick.AddListener(OnFullscreenClicked);
            if (windowedButton != null) windowedButton.onClick.AddListener(OnWindowedClicked);
            if (resolutionPrevButton != null) resolutionPrevButton.onClick.AddListener(OnResolutionPrev);
            if (resolutionNextButton != null) resolutionNextButton.onClick.AddListener(OnResolutionNext);
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
            if (fullscreenButton != null) fullscreenButton.onClick.RemoveListener(OnFullscreenClicked);
            if (windowedButton != null) windowedButton.onClick.RemoveListener(OnWindowedClicked);
            if (resolutionPrevButton != null) resolutionPrevButton.onClick.RemoveListener(OnResolutionPrev);
            if (resolutionNextButton != null) resolutionNextButton.onClick.RemoveListener(OnResolutionNext);
            if (titleButton != null) titleButton.onClick.RemoveListener(OnTitleClicked);
            if (confirmYesButton != null) confirmYesButton.onClick.RemoveListener(GoToTitle);
            if (confirmNoButton != null) confirmNoButton.onClick.RemoveListener(CloseConfirm);
        }

        public void Open()
        {
            if (transition != null && transition.IsPlaying) return;

            // 처음 켜질 때 Awake가 불리므로 켠 뒤에 값을 채운다. (transition.Show도 즉시 활성화한다)
            if (transition != null) transition.Show();
            else gameObject.SetActive(true);
            transform.SetAsLastSibling();
            CloseConfirm();

            Show(bgmSlider, bgmValueLabel, SoundSettings.BgmVolume);
            Show(sfxSlider, sfxValueLabel, SoundSettings.SfxVolume);
            RefreshDisplay(Screen.fullScreenMode == FullScreenMode.Windowed);
        }

        public void Close()
        {
            CloseConfirm();
            if (transition != null) transition.Hide();
            else gameObject.SetActive(false);
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

        private void OnFullscreenClicked()
        {
            DisplaySettings.SetWindowed(false);
            RefreshDisplay(false);
        }

        private void OnWindowedClicked()
        {
            DisplaySettings.SetWindowed(true);
            RefreshDisplay(true);
        }

        private void OnResolutionPrev() => StepResolution(-1);

        private void OnResolutionNext() => StepResolution(1);

        // 목록 끝에서는 멈춘다. 창 모드가 아닐 때는 값만 저장하고 화면은 바꾸지 않는다.
        private void StepResolution(int step)
        {
            DisplaySettings.SetResolutionIndex(DisplaySettings.ResolutionIndex + step);
            RefreshDisplay(DisplaySettings.IsWindowed);
        }

        // 버튼 모양과 해상도 글자를 갱신한다. SetResolution은 프레임 끝에 적용되므로, 클릭 직후에는
        // Screen.fullScreenMode 대신 방금 정한 값을 넘기고, 패널을 열 때만 실제 화면 모드(Alt+Enter로 바뀐 것 포함)를 쓴다.
        private void RefreshDisplay(bool windowed)
        {
            SetSelected(fullscreenButton, !windowed);
            SetSelected(windowedButton, windowed);

            int index = DisplaySettings.ResolutionIndex;
            int count = DisplaySettings.AvailableResolutions.Count;
            if (resolutionLabel != null)
            {
                Vector2Int size = DisplaySettings.CurrentResolution;
                resolutionLabel.text = size.x + " x " + size.y;
            }
            if (resolutionPrevButton != null) resolutionPrevButton.interactable = windowed && index > 0;
            if (resolutionNextButton != null) resolutionNextButton.interactable = windowed && index < count - 1;
        }

        // 선택된 버튼은 눌림 스프라이트로 보여준다. ButtonLabelPress가 이를 보고 글자도 함께 내린다.
        private void SetSelected(Button button, bool selected)
        {
            if (button == null || button.image == null) return;
            Sprite sprite = selected ? buttonPressedSprite : buttonSprite;
            if (sprite != null) button.image.sprite = sprite;
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
