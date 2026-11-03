using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private SaveLoadPanel loadPanel;

    private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

    private void OnEnable()
    {
        if (Application.isPlaying && loadPanel != null) loadPanel.Loaded += OnPanelLoaded;

        // OS 폰트는 씬에 저장되지 않으므로 활성화될 때마다 버튼 글자에 적용한다.
        Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
        foreach (Text label in GetComponentsInChildren<Text>(true))
        {
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
        if (loadPanel != null) loadPanel.OpenLoad();
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
