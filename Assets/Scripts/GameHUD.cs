using UnityEngine;
using UnityEngine.SceneManagement;
using BlackSmith.UI;

namespace BlackSmith
{
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private ShopPanel shopPanel;
        [SerializeField] private SaveLoadPanel saveLoadPanel;
        [SerializeField] private SettingsPanel settingsPanel;

        private void Awake()
        {
            if (saveLoadPanel != null) saveLoadPanel.Loaded += OnLoaded;
        }

        private void OnDestroy()
        {
            if (saveLoadPanel != null) saveLoadPanel.Loaded -= OnLoaded;
        }

        public void OnShop()
        {
            if (shopPanel != null) shopPanel.Open();
        }

        /// <summary>책 버튼 = 저장/불러오기 패널 열기. 기본 탭은 저장.</summary>
        public void OnBook()
        {
            if (saveLoadPanel != null) saveLoadPanel.OpenSave();
        }

        /// <summary>톱니바퀴 버튼 = 설정 패널(소리 크기) 열기.</summary>
        public void OnSettings()
        {
            if (settingsPanel != null) settingsPanel.Open();
        }

        // 불러온 데이터는 GameSession에 이미 반영되어 있으므로, 열려 있는 UI가 이전 데이터를 쥐지 않도록 씬을 다시 로드한다.
        private void OnLoaded(Data.PlayerData data)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
