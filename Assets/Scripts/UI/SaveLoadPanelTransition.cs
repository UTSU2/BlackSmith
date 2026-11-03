using System;
using UnityEngine;

/// <summary>
/// 저장/불러오기 패널을 보이고 숨기는 연출의 공통 베이스.
/// 씬마다 연출이 다르므로 상속해서 구현한다. (예: TitleLoadPanelTransition)
/// Show는 호출 즉시 패널 오브젝트를 활성화해야 한다. 패널이 활성화된 뒤에 슬롯 갱신이 이루어진다.
/// </summary>
public abstract class SaveLoadPanelTransition : MonoBehaviour
{
    public abstract bool IsPlaying { get; }

    public abstract void Show(Action onDone = null);

    public abstract void Hide(Action onDone = null);
}
