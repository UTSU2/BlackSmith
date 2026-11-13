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

        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        private void OnEnable()
        {
            if (Application.isPlaying && loadPanel != null) loadPanel.Loaded += OnPanelLoaded;

            // OS 폰트는 씬에 저장되지 않으므로 활성화될 때마다 버튼 글자에 적용한다.
            Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            foreach (Text label in GetComponentsInChildren<Text>(true))
            {
                if (label == titleLabel) continue;
                label.font = font;
            }
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
