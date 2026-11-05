using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 모루 클릭 입력. Collider2D가 있는 오브젝트에 붙이고, 카메라에는 Physics2DRaycaster가 필요하다.
    /// UI 위에서 누른 클릭은 EventSystem이 먼저 가져가므로(패널이 열려 있으면) 이 이벤트는 발생하지 않는다.
    /// 제작을 시작할지 여부는 이 클래스가 정하지 않고, Clicked를 구독한 쪽(CraftingController)이 판단한다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class AnvilInteractable : MonoBehaviour, IPointerClickHandler
    {
        public event Action Clicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            Clicked?.Invoke();
        }
    }
}
