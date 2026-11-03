using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 저장/불러오기 패널. 저장 버튼 또는 로드 버튼으로 열면 모드가 정해지고,
/// 슬롯을 누르면 모드에 따라 저장 또는 로드가 된다.
/// 이미 찬 슬롯에 저장할 때는 덮어쓰기 확인 팝업을 거친다.
/// </summary>
public class SaveLoadPanel : MonoBehaviour
{
    public enum Mode { Save, Load }

    [SerializeField] private SaveSlotView[] slots = new SaveSlotView[SaveSlotManager.SlotCount];
    [SerializeField] private Text titleLabel;
    [Tooltip("덮어쓰기 확인 팝업. 예 버튼은 OnConfirmOverwrite, 아니오 버튼은 OnCancelOverwrite에 연결한다.")]
    [SerializeField] private GameObject confirmPopup;
    [Tooltip("보이고 숨기는 연출. 비워 두면 그냥 켜고 끈다.")]
    [SerializeField] private SaveLoadPanelTransition transition;
    [Tooltip("슬롯 페이지 넘기기. 없으면 페이징 없이 동작한다.")]
    [SerializeField] private SaveSlotPager pager;

    private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

    private Mode mode = Mode.Save;
    private int pendingSlot = -1;

    public Mode CurrentMode => mode;

    /// <summary>로드에 성공했을 때 호출. GameSession.Current에 이미 반영된 상태다.</summary>
    public event Action<PlayerData> Loaded;

    private void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) slots[i].Clicked += OnSlotClicked;
        }
        ApplyFont();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) slots[i].Clicked -= OnSlotClicked;
        }
    }

    public void OpenSave() => Open(Mode.Save);

    public void OpenLoad() => Open(Mode.Load);

    public void Close()
    {
        HideConfirm();
        if (transition != null) transition.Hide();
        else gameObject.SetActive(false);
    }

    public void OnSlotClicked(int slot)
    {
        if (!SaveSlotManager.IsValidSlot(slot)) return;

        if (mode == Mode.Save)
        {
            if (SaveSlotManager.IsEmpty(slot))
            {
                SaveTo(slot);
            }
            else
            {
                pendingSlot = slot;
                if (confirmPopup != null) confirmPopup.SetActive(true);
                else SaveTo(slot); // 팝업이 연결되지 않았으면 바로 덮어쓴다.
            }
        }
        else
        {
            if (SaveSlotManager.IsEmpty(slot)) return;
            if (SaveSlotManager.TryLoad(slot, out PlayerData data))
            {
                GameSession.Set(data);
                Loaded?.Invoke(data);
            }
        }
    }

    public void OnConfirmOverwrite()
    {
        int slot = pendingSlot;
        HideConfirm();
        if (SaveSlotManager.IsValidSlot(slot)) SaveTo(slot);
    }

    public void OnCancelOverwrite() => HideConfirm();

    private void Open(Mode newMode)
    {
        if (transition != null && transition.IsPlaying) return;

        mode = newMode;
        // 슬롯 갱신 전에 패널이 먼저 활성화되어 있어야 한다. (transition.Show도 즉시 활성화함)
        if (transition != null) transition.Show();
        else gameObject.SetActive(true);

        HideConfirm();
        if (pager != null) pager.ResetToFirstPage();
        Refresh();
    }

    private void SaveTo(int slot)
    {
        SaveSlotManager.Save(slot, GameSession.Current);
        Refresh();
    }

    private void Refresh()
    {
        if (titleLabel != null) titleLabel.text = mode == Mode.Save ? "저장" : "불러오기";
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) slots[i].Refresh(i);
        }
    }

    private void HideConfirm()
    {
        pendingSlot = -1;
        if (confirmPopup != null) confirmPopup.SetActive(false);
    }

    private void ApplyFont()
    {
        // OS 폰트는 씬에 저장되지 않으므로 시작할 때 글자에 적용한다. (MainMenu와 동일한 방식)
        Font font = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
        foreach (Text text in GetComponentsInChildren<Text>(true))
        {
            text.font = font;
        }
    }
}
