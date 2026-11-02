using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 아이템(ScriptableObject)을 쉽게 만드는 에디터 창. 메뉴: Tools > BlackSmith > Item Creator
/// 만든 에셋은 Assets/Resources/Items/ 아래에 저장되어 ItemDatabase가 자동으로 읽는다.
/// </summary>
public class ItemCreatorWindow : EditorWindow
{
    private const string RootFolder = "Assets/Resources/Items";
    private const string MineralFolder = RootFolder + "/Minerals";
    private const string CraftedFolder = RootFolder + "/Crafted";
    private static readonly Regex IdPattern = new Regex("^[a-z0-9_]+$");

    private ItemType type = ItemType.Mineral;
    private ItemType previousType = ItemType.Mineral;
    private string id = "";
    private string displayName = "";
    private Sprite icon;
    private string description = "";
    private int maxStack = 99;
    private ItemGrade defaultGrade = ItemGrade.Common;
    private int sellPrice;
    private int buyPrice;
    private float[] multipliers = NewMultipliers();
    private bool showMultipliers;

    private Vector2 scroll;
    private readonly List<ItemData> existing = new List<ItemData>();
    private string message;
    private MessageType messageType = MessageType.None;

    [MenuItem("Tools/BlackSmith/Item Creator")]
    public static void Open()
    {
        ItemCreatorWindow window = GetWindow<ItemCreatorWindow>("Item Creator");
        window.minSize = new Vector2(380, 520);
        window.RefreshList();
    }

    private void OnEnable() { RefreshList(); }
    private void OnFocus() { RefreshList(); }
    private void OnProjectChange() { RefreshList(); Repaint(); }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("새 아이템 만들기", EditorStyles.boldLabel);

        type = (ItemType)EditorGUILayout.EnumPopup(new GUIContent("종류", "광물: 구매+판매 가능 / 제작품: 판매만 가능"), type);
        if (type != previousType)
        {
            maxStack = DefaultMaxStack(type);
            previousType = type;
        }
        EditorGUILayout.HelpBox(type == ItemType.Mineral ? "광물: 구매와 판매가 모두 가능합니다." : "제작품: 판매만 가능합니다 (구매 불가).", MessageType.None);

        id = EditorGUILayout.TextField(new GUIContent("ID", "영소문자, 숫자, 밑줄만 사용. 예: iron_ore"), id);
        displayName = EditorGUILayout.TextField("이름", displayName);
        icon = (Sprite)EditorGUILayout.ObjectField("아이콘", icon, typeof(Sprite), false);
        EditorGUILayout.LabelField("설명");
        description = EditorGUILayout.TextArea(description, GUILayout.MinHeight(48));

        EditorGUILayout.Space();
        maxStack = Mathf.Max(1, EditorGUILayout.IntField("최대 스택 수", maxStack));
        defaultGrade = (ItemGrade)EditorGUILayout.EnumPopup("기본 등급", defaultGrade);

        EditorGUILayout.Space();
        if (type == ItemType.Mineral)
        {
            buyPrice = Mathf.Max(0, EditorGUILayout.IntField("구매가", buyPrice));
        }
        sellPrice = Mathf.Max(0, EditorGUILayout.IntField("판매가", sellPrice));

        showMultipliers = EditorGUILayout.Foldout(showMultipliers, "등급별 판매 배율 (임시값 1)", true);
        if (showMultipliers)
        {
            EditorGUI.indentLevel++;
            if (multipliers == null || multipliers.Length != ItemGradeInfo.Count) multipliers = NewMultipliers();
            for (int i = 0; i < multipliers.Length; i++)
            {
                multipliers[i] = Mathf.Max(0f, EditorGUILayout.FloatField(((ItemGrade)i).ToString(), multipliers[i]));
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("만들기", GUILayout.Height(28)))
        {
            string error;
            ItemData created = CreateItem(type, id, displayName, icon, description, maxStack, defaultGrade, sellPrice, buyPrice, multipliers, out error);
            if (created == null)
            {
                message = error;
                messageType = MessageType.Error;
            }
            else
            {
                message = "생성 완료: " + AssetDatabase.GetAssetPath(created);
                messageType = MessageType.Info;
                ResetForm();
                Selection.activeObject = created;
                EditorGUIUtility.PingObject(created);
            }
            RefreshList();
        }

        if (!string.IsNullOrEmpty(message))
        {
            EditorGUILayout.HelpBox(message, messageType);
        }

        DrawExistingList();

        EditorGUILayout.EndScrollView();
    }

    private void DrawExistingList()
    {
        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField($"등록된 아이템 ({existing.Count})", EditorStyles.boldLabel);

        List<string> problems = FindProblems();
        foreach (string problem in problems)
        {
            EditorGUILayout.HelpBox(problem, MessageType.Warning);
        }

        foreach (ItemData item in existing)
        {
            if (item == null) continue;
            using (new EditorGUILayout.HorizontalScope())
            {
                string kind = item.Type == ItemType.Mineral ? "[광물]" : "[제작품]";
                string price = item.CanBuy ? $"구매 {item.BuyPrice} / 판매 {item.SellPrice}" : $"판매 {item.SellPrice}";
                EditorGUILayout.LabelField($"{kind} {item.Id}  ({item.DisplayName})  {price}");
                if (GUILayout.Button("선택", GUILayout.Width(48)))
                {
                    Selection.activeObject = item;
                    EditorGUIUtility.PingObject(item);
                }
            }
        }
    }

    private List<string> FindProblems()
    {
        List<string> problems = new List<string>();
        HashSet<string> seen = new HashSet<string>();
        HashSet<string> reported = new HashSet<string>();
        foreach (ItemData item in existing)
        {
            if (item == null) continue;
            string path = AssetDatabase.GetAssetPath(item);
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                problems.Add($"ID가 비어 있는 아이템: {path}");
                continue;
            }
            if (!seen.Add(item.Id) && reported.Add(item.Id))
            {
                problems.Add($"중복 ID '{item.Id}' 가 있습니다. 게임에서는 하나만 등록됩니다.");
            }
            if (!path.StartsWith(RootFolder + "/", StringComparison.Ordinal))
            {
                problems.Add($"'{item.Id}'은(는) {RootFolder} 밖에 있어서 게임에서 자동으로 읽히지 않습니다: {path}");
            }
        }
        return problems;
    }

    private void RefreshList()
    {
        existing.Clear();
        existing.AddRange(FindAllItems());
        existing.Sort((a, b) =>
        {
            int byType = a.Type.CompareTo(b.Type);
            return byType != 0 ? byType : string.CompareOrdinal(a.Id, b.Id);
        });
    }

    private void ResetForm()
    {
        id = "";
        displayName = "";
        icon = null;
        description = "";
        maxStack = DefaultMaxStack(type);
        defaultGrade = ItemGrade.Common;
        sellPrice = 0;
        buyPrice = 0;
        multipliers = NewMultipliers();
    }

    private static int DefaultMaxStack(ItemType type)
    {
        return type == ItemType.Mineral ? 99 : 1;
    }

    private static float[] NewMultipliers()
    {
        float[] values = new float[ItemGradeInfo.Count];
        for (int i = 0; i < values.Length; i++) values[i] = 1f;
        return values;
    }

    /// <summary>프로젝트 안의 모든 ItemData 에셋.</summary>
    private static List<ItemData> FindAllItems()
    {
        List<ItemData> items = new List<ItemData>();
        foreach (string guid in AssetDatabase.FindAssets("t:ItemData"))
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null) items.Add(item);
        }
        return items;
    }

    /// <summary>
    /// 아이템 에셋을 검증하고 만든다. 실패하면 null을 반환하고 error에 이유를 담는다.
    /// 창의 만들기 버튼과 테스트 코드가 같이 쓴다.
    /// </summary>
    public static ItemData CreateItem(ItemType type, string id, string displayName, Sprite icon, string description,
        int maxStack, ItemGrade defaultGrade, int sellPrice, int buyPrice, float[] multipliers, out string error)
    {
        id = id == null ? "" : id.Trim();
        displayName = displayName == null ? "" : displayName.Trim();

        if (id.Length == 0) { error = "ID를 입력하세요."; return null; }
        if (!IdPattern.IsMatch(id)) { error = "ID는 영소문자, 숫자, 밑줄(_)만 사용할 수 있습니다."; return null; }
        if (displayName.Length == 0) { error = "이름을 입력하세요."; return null; }
        if (maxStack < 1) { error = "최대 스택 수는 1 이상이어야 합니다."; return null; }
        if (sellPrice < 0 || buyPrice < 0) { error = "가격은 0 이상이어야 합니다."; return null; }

        foreach (ItemData item in FindAllItems())
        {
            if (item.Id == id)
            {
                error = $"이미 같은 ID '{id}' 의 아이템이 있습니다: {AssetDatabase.GetAssetPath(item)}";
                return null;
            }
        }

        string folder = type == ItemType.Mineral ? MineralFolder : CraftedFolder;
        EnsureFolder(folder);

        ItemData asset = type == ItemType.Mineral
            ? (ItemData)CreateInstance<MineralItemData>()
            : CreateInstance<CraftedItemData>();
        string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + id + ".asset");
        AssetDatabase.CreateAsset(asset, path);

        SerializedObject so = new SerializedObject(asset);
        so.FindProperty("id").stringValue = id;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("description").stringValue = description ?? "";
        so.FindProperty("maxStack").intValue = maxStack;
        so.FindProperty("defaultGrade").enumValueIndex = (int)defaultGrade;
        so.FindProperty("sellPrice").intValue = sellPrice;

        SerializedProperty multiplierProperty = so.FindProperty("sellPriceMultipliers");
        multiplierProperty.arraySize = ItemGradeInfo.Count;
        for (int i = 0; i < ItemGradeInfo.Count; i++)
        {
            float value = (multipliers != null && i < multipliers.Length) ? multipliers[i] : 1f;
            multiplierProperty.GetArrayElementAtIndex(i).floatValue = value;
        }

        if (type == ItemType.Mineral)
        {
            so.FindProperty("buyPrice").intValue = buyPrice;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        error = null;
        return asset;
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
