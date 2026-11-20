  using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BlackSmith.Data;
using BlackSmith.UI;

namespace BlackSmith
{
    [ExecuteAlways]
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "Game";
        [SerializeField] private SaveLoadPanel loadPanel;
        [SerializeField] private SettingsPanel settingsPanel;
        [Tooltip("제목 글자. 자체 폰트를 쓰므로 OS 한글 폰트를 덮어쓰지 않는다.")]
        [SerializeField] private Text titleLabel;

        private void OnEnable()
        {
            if (Application.isPlaying && loadPanel != null) loadPanel.Loaded += OnPanelLoaded;

            // 글꼴은 크기 기준(UIFontSettings)으로 활성화될 때마다 적용한다.
            UIFonts.ApplyAll(this, titleLabel);
        }

        private void OnDisable()
        {
            if (loadPanel != null) loadPanel.Loaded -= OnPanelLoaded;
        }

        public void OnNewGame()
        {
            GameSession.StartNew();
            SceneManager.LoadScene(gameSceneName);
        }

        public void OnLoadGame()
        {
            // 설정 패널 연출이 끝나기 전에 누르면 두 연출이 같은 버튼을 동시에 움직이므로 무시한다.
            if (settingsPanel != null && settingsPanel.IsTransitionPlaying) return;
            if (loadPanel != null) loadPanel.OpenLoad();
        }

        public void OnSettings()
        {
            // 로드 패널 연출이 끝나기 전에 누르면 두 연출이 같은 버튼을 동시에 움직이므로 무시한다.
            if (loadPanel != null && loadPanel.IsTransitionPlaying) return;
            if (settingsPanel != null) settingsPanel.Open();
        }

        private void OnPanelLoaded(PlayerData data)
        {
            // GameSession에는 SaveLoadPanel이 이미 반영해 두었다.
            SceneManager.LoadScene(gameSceneName);
        }

        public void OnQuit()
        {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
        }
    }
}
