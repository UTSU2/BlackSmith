using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using BlackSmith.Items;

namespace BlackSmith.EditorTools
{
    /// <summary>
    /// 아이템(ScriptableObject)을 쉽게 만들고 수정하는 에디터 창. 메뉴: Tools > BlackSmith > Item Creator
    /// 왼쪽: 등록된 아이템 목록 / 오른쪽: 새로 만들기 또는 선택한 아이템 수정 폼.
    /// 만든 에셋은 Assets/Resources/Items/ 아래에 저장되어 ItemDatabase가 자동으로 읽는다.
    /// </summary>
    public class ItemCreatorWindow : EditorWindow
    {
        private const string RootFolder = "Assets/Resources/Items";
        private const string MineralFolder = RootFolder + "/Minerals";
        private const string CraftedFolder = RootFolder + "/Crafted";
        private const float ListWidth = 260f;
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

        /// <summary>null이면 새로 만들기 모드, 아니면 이 아이템을 수정 중.</summary>
        private ItemData editing;

        private Vector2 formScroll;
        private Vector2 listScroll;
        private readonly List<ItemData> existing = new List<ItemData>();
        private string message;
        private MessageType messageType = MessageType.None;

        [MenuItem("Tools/BlackSmith/Item Creator")]
        public static void Open()
        {
            ItemCreatorWindow window = GetWindow<ItemCreatorWindow>("Item Creator");
            window.minSize = new Vector2(700, 520);
            window.RefreshList();
        }

        private void OnEnable() { RefreshList(); }
        private void OnFocus() { RefreshList(); }
        private void OnProjectChange() { RefreshList(); Repaint(); }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(ListWidth)))
                {
                    DrawExistingList();
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawForm();
                }
            }
        }

        // ───────────────────────── 오른쪽: 폼 ─────────────────────────

        private void DrawForm()
        {
            formScroll = EditorGUILayout.BeginScrollView(formScroll);

            bool isEditing = editing != null;
            EditorGUILayout.LabelField(isEditing ? $"아이템 수정: {editing.Id}" : "새 아이템 만들기", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(isEditing))
            {
                type = (ItemType)EditorGUILayout.EnumPopup(new GUIContent("종류", "광물: 구매+판매 가능 / 제작품: 판매만 가능. 만든 뒤에는 바꿀 수 없습니다."), type);
            }
            if (type != previousType)
            {
                if (!isEditing) maxStack = DefaultMaxStack(type);
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
            if (isEditing)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("저장", GUILayout.Height(28))) OnSaveClicked();
                    if (GUILayout.Button("취소", GUILayout.Height(28), GUILayout.Width(80))) StartNewItem();
                }
            }
            else if (GUILayout.Button("만들기", GUILayout.Height(28)))
            {
                OnCreateClicked();
            }

            if (!string.IsNullOrEmpty(message))
            {
                EditorGUILayout.HelpBox(message, messageType);
            }

            EditorGUILayout.EndScrollView();
        }

        private void OnCreateClicked()
        {
            string error;
            ItemData created = CreateItem(type, id, displayName, icon, description, maxStack, defaultGrade, sellPrice, buyPrice, multipliers, out error);
            if (created == null)
            {
                SetMessage(error, MessageType.Error);
            }
            else
            {
                SetMessage("생성 완료: " + AssetDatabase.GetAssetPath(created), MessageType.Info);
                ResetForm();
                Selection.activeObject = created;
                EditorGUIUtility.PingObject(created);
            }
            RefreshList();
        }

        private void OnSaveClicked()
        {
            string error;
            if (UpdateItem(editing, id, displayName, icon, description, maxStack, defaultGrade, sellPrice, buyPrice, multipliers, out error))
            {
                SetMessage("저장 완료: " + AssetDatabase.GetAssetPath(editing), MessageType.Info);
            }
            else
            {
                SetMessage(error, MessageType.Error);
            }
            RefreshList();
        }

        private void SetMessage(string text, MessageType kind)
        {
            message = text;
            messageType = kind;
        }

        private void StartNewItem()
        {
            editing = null;
            message = null;
            ResetForm();
            GUI.FocusControl(null);
        }

        private void StartEditing(ItemData item)
        {
            editing = item;
            message = null;
            LoadFromItem(item);
            GUI.FocusControl(null);
        }

        private void LoadFromItem(ItemData item)
        {
            SerializedObject so = new SerializedObject(item);
            type = item.Type;
            previousType = type;
            id = so.FindProperty("id").stringValue;
            displayName = so.FindProperty("displayName").stringValue;
            icon = so.FindProperty("icon").objectReferenceValue as Sprite;
            description = so.FindProperty("description").stringValue;
            maxStack = so.FindProperty("maxStack").intValue;
            defaultGrade = (ItemGrade)so.FindProperty("defaultGrade").enumValueIndex;
            sellPrice = so.FindProperty("sellPrice").intValue;

            SerializedProperty buy = so.FindProperty("buyPrice");
            buyPrice = buy != null ? buy.intValue : 0;

            multipliers = NewMultipliers();
            SerializedProperty multiplierProperty = so.FindProperty("sellPriceMultipliers");
            for (int i = 0; i < multipliers.Length && i < multiplierProperty.arraySize; i++)
            {
                multipliers[i] = multiplierProperty.GetArrayElementAtIndex(i).floatValue;
            }
        }

        // ───────────────────────── 왼쪽: 목록 ─────────────────────────

        private void DrawExistingList()
        {
            EditorGUILayout.LabelField($"등록된 아이템 ({existing.Count})", EditorStyles.boldLabel);

            if (GUILayout.Button("+ 새 아이템", GUILayout.Height(24))) StartNewItem();

            List<string> problems = FindProblems();
            foreach (string problem in problems)
            {
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
            }

            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUI.skin.box);
            foreach (ItemData item in existing)
            {
                if (item == null) continue;
                string kind = item.Type == ItemType.Mineral ? "[광물]" : "[제작품]";
                string label = $"{kind} {item.Id}  ({item.DisplayName})";

                bool selected = item == editing;
                Color previous = GUI.backgroundColor;
                if (selected) GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
                if (GUILayout.Button(new GUIContent(label, AssetDatabase.GetAssetPath(item)), EditorStyles.miniButtonMid, GUILayout.Height(22)))
                {
                    StartEditing(item);
                    Selection.activeObject = item;
                    EditorGUIUtility.PingObject(item);
                }
                GUI.backgroundColor = previous;
            }
            EditorGUILayout.EndScrollView();
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

            // 수정 중이던 에셋이 삭제됐으면 새로 만들기 모드로 복귀
            if (editing != null && !existing.Contains(editing))
            {
                editing = null;
                ResetForm();
            }
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

        // ───────────────────────── 생성 / 수정 로직 ─────────────────────────

        /// <summary>입력값 검증. 문제가 있으면 false와 이유를 반환한다. self는 중복 ID 검사에서 제외할 자기 자신.</summary>
        private static bool Validate(ref string id, ref string displayName, int maxStack, int sellPrice, int buyPrice,
            ItemData self, out string error)
        {
            id = id == null ? "" : id.Trim();
            displayName = displayName == null ? "" : displayName.Trim();

            if (id.Length == 0) { error = "ID를 입력하세요."; return false; }
            if (!IdPattern.IsMatch(id)) { error = "ID는 영소문자, 숫자, 밑줄(_)만 사용할 수 있습니다."; return false; }
            if (displayName.Length == 0) { error = "이름을 입력하세요."; return false; }
            if (maxStack < 1) { error = "최대 스택 수는 1 이상이어야 합니다."; return false; }
            if (sellPrice < 0 || buyPrice < 0) { error = "가격은 0 이상이어야 합니다."; return false; }

            foreach (ItemData item in FindAllItems())
            {
                if (item != self && item.Id == id)
                {
                    error = $"이미 같은 ID '{id}' 의 아이템이 있습니다: {AssetDatabase.GetAssetPath(item)}";
                    return false;
                }
            }

            error = null;
            return true;
        }

        /// <summary>
        /// 아이템 에셋을 검증하고 만든다. 실패하면 null을 반환하고 error에 이유를 담는다.
        /// 창의 만들기 버튼과 테스트 코드가 같이 쓴다.
        /// </summary>
        public static ItemData CreateItem(ItemType type, string id, string displayName, Sprite icon, string description,
            int maxStack, ItemGrade defaultGrade, int sellPrice, int buyPrice, float[] multipliers, out string error)
        {
            if (!Validate(ref id, ref displayName, maxStack, sellPrice, buyPrice, null, out error)) return null;

            string folder = type == ItemType.Mineral ? MineralFolder : CraftedFolder;
            EnsureFolder(folder);

            ItemData asset = type == ItemType.Mineral
                ? (ItemData)CreateInstance<MineralItemData>()
                : CreateInstance<CraftedItemData>();
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + id + ".asset");
            AssetDatabase.CreateAsset(asset, path);

            ApplyFields(asset, id, displayName, icon, description, maxStack, defaultGrade, sellPrice, buyPrice, multipliers);
            error = null;
            return asset;
        }

        /// <summary>기존 아이템 에셋을 수정한다. 종류(광물/제작품)는 바꿀 수 없다. 실패하면 false와 이유를 반환한다.</summary>
        public static bool UpdateItem(ItemData item, string id, string displayName, Sprite icon, string description,
            int maxStack, ItemGrade defaultGrade, int sellPrice, int buyPrice, float[] multipliers, out string error)
        {
            if (item == null) { error = "수정할 아이템이 없습니다."; return false; }
            if (!Validate(ref id, ref displayName, maxStack, sellPrice, buyPrice, item, out error)) return false;

            // ID가 바뀌면 파일명도 맞춘다 (GUID가 유지되므로 참조는 깨지지 않는다)
            if (item.Id != id)
            {
                string path = AssetDatabase.GetAssetPath(item);
                string renameError = AssetDatabase.RenameAsset(path, id);
                if (!string.IsNullOrEmpty(renameError))
                {
                    error = "파일 이름을 바꾸지 못했습니다: " + renameError;
                    return false;
                }
            }

            ApplyFields(item, id, displayName, icon, description, maxStack, defaultGrade, sellPrice, buyPrice, multipliers);
            error = null;
            return true;
        }

        private static void ApplyFields(ItemData asset, string id, string displayName, Sprite icon, string description,
            int maxStack, ItemGrade defaultGrade, int sellPrice, int buyPrice, float[] multipliers)
        {
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

            SerializedProperty buy = so.FindProperty("buyPrice");
            if (asset.Type == ItemType.Mineral && buy != null)
            {
                buy.intValue = buyPrice;
            }
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
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
}
