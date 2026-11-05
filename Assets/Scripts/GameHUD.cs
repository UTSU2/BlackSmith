using UnityEngine;
using BlackSmith.UI;

namespace BlackSmith
{
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private ShopPanel shopPanel;

        public void OnShop()
        {
            if (shopPanel != null) shopPanel.Open();
        }

        public void OnBook()
        {
            // TODO: 책 버튼의 용도(레시피북/도감 등)가 정해지면 구현
        }
    }
}
