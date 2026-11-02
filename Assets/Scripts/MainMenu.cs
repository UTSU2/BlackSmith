using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";

    private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

    private void OnEnable()
    {
        // OS 폰트는 씬에 저장되지 않으므로 활성화될 때마다 버튼 글자에 적용한다.
        Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
        foreach (Text label in GetComponentsInChildren<Text>(true))
        {
            label.font = font;
        }
    }

    public void OnNewGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnLoadGame()
    {
        // TODO: 로드 게임 기능은 아직 구현하지 않음
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
