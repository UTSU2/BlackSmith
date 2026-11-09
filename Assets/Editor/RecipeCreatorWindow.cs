using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using BlackSmith.Crafting;
using BlackSmith.Items;

namespace BlackSmith.EditorTools
{
    /// <summary>
    /// 레시피(ScriptableObject)를 쉽게 만들고 수정하는 에디터 창. 메뉴: Tools > BlackSmith > Recipe Creator
    /// 왼쪽: 등록된 레시피 목록 / 오른쪽: 새로 만들기 또는 선택한 레시피 수정 폼.
    /// 만든 에셋은 Assets/Resources/Recipes/ 아래에 저장되어 RecipeDatabase가 자동으로 읽는다.
    /// CraftRecipeData의 private 필드 이름(id, result, ingredients, stages)을 SerializedObject로 직접 쓰므로,
    /// 그 필드 이름을 바꾸면 이 창도 같이 수정해야 한다.
    /// </summary>
    public class RecipeCreatorWindow : EditorWindow
    {
        private const string RootFolder = "Assets/Resources/Recipes";
        private const float ListWidth = 260f;
        private static readonly Regex IdPattern = new Regex("^[a-z0-9_]+$");

        /// <summary>폼에서 편집하는 재료 한 줄.</summary>
        public class IngredientInput
        {
            public MineralItemData Item;
            public int Count = 1;
        }

        private string id = "";
        private CraftedItemData result;
        private List<IngredientInput> ingredients = new List<IngredientInput>();
        private List<CraftStage> stages = DefaultStages();
        private int baseDifficulty = 1;

        /// <summary>null이면 새로 만들기 모드, 아니면 이 레시피를 수정 중.</summary>
        private CraftRecipeData editing;

        private Vector2 formScroll;
        private Vector2 listScroll;
        private readonly List<CraftRecipeData> existing = new List<CraftRecipeData>();
        private string message;
        private MessageType messageType = MessageType.None;

        [MenuItem("Tools/BlackSmith/Recipe Creator")]
        public static void Open()
        {
            RecipeCreatorWindow window = GetWindow<RecipeCreatorWindow>("Recipe Creator");
            window.minSize = new Vector2(700, 480);
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
            EditorGUILayout.LabelField(isEditing ? $"레시피 수정: {editing.Id}" : "새 레시피 만들기", EditorStyles.boldLabel);

            id = EditorGUILayout.TextField(new GUIContent("ID", "영소문자, 숫자, 밑줄만 사용. 예: iron_sword"), id);
            result = (CraftedItemData)EditorGUILayout.ObjectField(
                new GUIContent("결과물", "이 레시피로 만들어지는 제작품 (Item Creator에서 만든 제작품)"), result, typeof(CraftedItemData), false);

            if (result != null)
            {
                EditorGUILayout.LabelField("결과물 등급", result.DefaultGrade.ToString());
            }
            baseDifficulty = EditorGUILayout.IntSlider(
                new GUIContent("기본 난이도", "미니게임 기본 난이도(1~10). 레시피 등급보다 높은 재료를 쓰면 가중이 더해진다."),
                baseDifficulty, CraftDifficulty.MinLevel, CraftDifficulty.MaxLevel);

            EditorGUILayout.Space();
            DrawIngredients();

            EditorGUILayout.Space();
            DrawStages();

            EditorGUILayout.Space();
            if (isEditing)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("저장", GUILayout.Height(28))) OnSaveClicked();
                    if (GUILayout.Button("취소", GUILayout.Height(28), GUILayout.Width(80))) StartNewRecipe();
                    if (GUILayout.Button("삭제", GUILayout.Height(28), GUILayout.Width(80))) OnDeleteClicked();
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

        private void DrawIngredients()
        {
            EditorGUILayout.LabelField("재료", EditorStyles.boldLabel);

            int removeAt = -1;
            for (int i = 0; i < ingredients.Count; i++)
            {
                IngredientInput line = ingredients[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    line.Item = (MineralItemData)EditorGUILayout.ObjectField(line.Item, typeof(MineralItemData), false);
                    EditorGUILayout.LabelField("x", GUILayout.Width(12));
                    line.Count = Mathf.Max(1, EditorGUILayout.IntField(line.Count, GUILayout.Width(50)));
                    if (GUILayout.Button("✕", GUILayout.Width(24))) removeAt = i;
                }
            }
            if (removeAt >= 0) ingredients.RemoveAt(removeAt);

            if (GUILayout.Button("+ 재료 추가", GUILayout.Width(110))) ingredients.Add(new IngredientInput());
        }

        private void DrawStages()
        {
            EditorGUILayout.LabelField("제작 단계 (위에서 아래 순서)", EditorStyles.boldLabel);

            int removeAt = -1;
            int moveUp = -1;
            int moveDown = -1;
            for (int i = 0; i < stages.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(20));
                    stages[i] = (CraftStage)EditorGUILayout.EnumPopup(stages[i]);
                    using (new EditorGUI.DisabledScope(i == 0))
                    {
                        if (GUILayout.Button("▲", GUILayout.Width(24))) moveUp = i;
                    }
                    using (new EditorGUI.DisabledScope(i == stages.Count - 1))
                    {
                        if (GUILayout.Button("▼", GUILayout.Width(24))) moveDown = i;
                    }
                    if (GUILayout.Button("✕", GUILayout.Width(24))) removeAt = i;
                }
            }
            if (removeAt >= 0) stages.RemoveAt(removeAt);
            if (moveUp > 0) Swap(stages, moveUp, moveUp - 1);
            if (moveDown >= 0 && moveDown < stages.Count - 1) Swap(stages, moveDown, moveDown + 1);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ 단계 추가", GUILayout.Width(110))) stages.Add(CraftStage.Heat);
                if (GUILayout.Button("기본 순서로", GUILayout.Width(110))) stages = DefaultStages();
            }
        }

        private void OnCreateClicked()
        {
            string error;
            CraftRecipeData created = CreateRecipe(id, result, ingredients, stages, baseDifficulty, out error);
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
            if (UpdateRecipe(editing, id, result, ingredients, stages, baseDifficulty, out error))
            {
                SetMessage("저장 완료: " + AssetDatabase.GetAssetPath(editing), MessageType.Info);
            }
            else
            {
                SetMessage(error, MessageType.Error);
            }
            RefreshList();
        }

        private void OnDeleteClicked()
        {
            if (editing == null) return;

            string path = AssetDatabase.GetAssetPath(editing);
            if (!EditorUtility.DisplayDialog("레시피 삭제", $"'{editing.Id}' 레시피를 휴지통으로 보낼까요?\n{path}", "삭제", "취소")) return;

            AssetDatabase.MoveAssetToTrash(path);
            StartNewRecipe();
            SetMessage("삭제 완료: " + path, MessageType.Info);
            RefreshList();
        }

        private void SetMessage(string text, MessageType kind)
        {
            message = text;
            messageType = kind;
        }

        private void StartNewRecipe()
        {
            editing = null;
            message = null;
            ResetForm();
            GUI.FocusControl(null);
        }

        private void StartEditing(CraftRecipeData recipe)
        {
            editing = recipe;
            message = null;
            LoadFromRecipe(recipe);
            GUI.FocusControl(null);
        }

        private void LoadFromRecipe(CraftRecipeData recipe)
        {
            id = recipe.Id;
            result = recipe.Result;

            ingredients = new List<IngredientInput>();
            foreach (CraftIngredient ingredient in recipe.Ingredients)
            {
                ingredients.Add(new IngredientInput { Item = ingredient.Item, Count = ingredient.Count });
            }

            stages = new List<CraftStage>(recipe.Stages);
            baseDifficulty = recipe.BaseDifficulty;
        }

        // ───────────────────────── 왼쪽: 목록 ─────────────────────────

        private void DrawExistingList()
        {
            EditorGUILayout.LabelField($"등록된 레시피 ({existing.Count})", EditorStyles.boldLabel);

            if (GUILayout.Button("+ 새 레시피", GUILayout.Height(24))) StartNewRecipe();

            foreach (string problem in FindProblems())
            {
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
            }

            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUI.skin.box);
            foreach (CraftRecipeData recipe in existing)
            {
                if (recipe == null) continue;
                string resultName = recipe.Result != null ? recipe.Result.DisplayName : "결과물 없음";
                string label = $"{recipe.Id}  ({resultName})";

                bool selected = recipe == editing;
                Color previous = GUI.backgroundColor;
                if (selected) GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
                if (GUILayout.Button(new GUIContent(label, AssetDatabase.GetAssetPath(recipe)), EditorStyles.miniButtonMid, GUILayout.Height(22)))
                {
                    StartEditing(recipe);
                    Selection.activeObject = recipe;
                    EditorGUIUtility.PingObject(recipe);
                }
                GUI.backgroundColor = previous;
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>목록 경고: 빈/중복 ID, 폴더 밖 에셋, 결과물/재료 누락, 재료 조합이 같은 레시피(매칭 모호).</summary>
        private List<string> FindProblems()
        {
            List<string> problems = new List<string>();
            HashSet<string> seen = new HashSet<string>();
            HashSet<string> reported = new HashSet<string>();
            Dictionary<string, CraftRecipeData> bySignature = new Dictionary<string, CraftRecipeData>();

            foreach (CraftRecipeData recipe in existing)
            {
                if (recipe == null) continue;
                string path = AssetDatabase.GetAssetPath(recipe);

                if (string.IsNullOrWhiteSpace(recipe.Id))
                {
                    problems.Add($"ID가 비어 있는 레시피: {path}");
                    continue;
                }
                if (!seen.Add(recipe.Id) && reported.Add(recipe.Id))
                {
                    problems.Add($"중복 ID '{recipe.Id}' 가 있습니다. 게임에서는 하나만 등록됩니다.");
                }
                if (!path.StartsWith(RootFolder + "/", StringComparison.Ordinal))
                {
                    problems.Add($"'{recipe.Id}'은(는) {RootFolder} 밖에 있어서 게임에서 자동으로 읽히지 않습니다: {path}");
                }
                if (recipe.Result == null)
                {
                    problems.Add($"'{recipe.Id}'에 결과물이 없어 게임에서 등록되지 않습니다.");
                }

                string signature = IngredientSignature(recipe);
                if (signature.Length == 0) continue;
                if (bySignature.TryGetValue(signature, out CraftRecipeData other))
                {
                    problems.Add($"'{recipe.Id}'와(과) '{other.Id}'의 재료 조합이 같아 매칭이 모호합니다.");
                }
                else
                {
                    bySignature.Add(signature, recipe);
                }
            }
            return problems;
        }

        private void RefreshList()
        {
            existing.Clear();
            existing.AddRange(FindAllRecipes());
            existing.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

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
            result = null;
            ingredients = new List<IngredientInput>();
            stages = DefaultStages();
            baseDifficulty = CraftDifficulty.MinLevel;
        }

        /// <summary>GameDesign.md의 확정 순서: 가열 → 단조 → 열처리 → 연마.</summary>
        private static List<CraftStage> DefaultStages()
        {
            return new List<CraftStage> { CraftStage.Heat, CraftStage.Forge, CraftStage.HeatTreat, CraftStage.Polish };
        }

        private static void Swap(List<CraftStage> list, int a, int b)
        {
            CraftStage temp = list[a];
            list[a] = list[b];
            list[b] = temp;
        }

        /// <summary>재료 조합을 비교용 문자열로 만든다 (순서 무관). 재료가 없으면 빈 문자열.</summary>
        private static string IngredientSignature(CraftRecipeData recipe)
        {
            List<string> parts = new List<string>();
            foreach (CraftIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient == null || ingredient.Item == null) continue;
                parts.Add(ingredient.Item.Id + ":" + ingredient.Count);
            }
            parts.Sort(string.CompareOrdinal);
            return string.Join("|", parts);
        }

        /// <summary>프로젝트 안의 모든 CraftRecipeData 에셋.</summary>
        private static List<CraftRecipeData> FindAllRecipes()
        {
            List<CraftRecipeData> recipes = new List<CraftRecipeData>();
            foreach (string guid in AssetDatabase.FindAssets("t:CraftRecipeData"))
            {
                CraftRecipeData recipe = AssetDatabase.LoadAssetAtPath<CraftRecipeData>(AssetDatabase.GUIDToAssetPath(guid));
                if (recipe != null) recipes.Add(recipe);
            }
            return recipes;
        }

        // ───────────────────────── 생성 / 수정 로직 ─────────────────────────

        /// <summary>입력값 검증. 문제가 있으면 false와 이유를 반환한다. self는 중복 ID 검사에서 제외할 자기 자신.</summary>
        private static bool Validate(ref string id, CraftedItemData result, IList<IngredientInput> ingredients,
            IList<CraftStage> stages, CraftRecipeData self, out string error)
        {
            id = id == null ? "" : id.Trim();

            if (id.Length == 0) { error = "ID를 입력하세요."; return false; }
            if (!IdPattern.IsMatch(id)) { error = "ID는 영소문자, 숫자, 밑줄(_)만 사용할 수 있습니다."; return false; }
            if (result == null) { error = "결과물을 지정하세요."; return false; }
            if (ingredients == null || ingredients.Count == 0) { error = "재료를 1개 이상 추가하세요."; return false; }

            HashSet<MineralItemData> usedItems = new HashSet<MineralItemData>();
            foreach (IngredientInput line in ingredients)
            {
                if (line.Item == null) { error = "비어 있는 재료 칸이 있습니다."; return false; }
                if (line.Count < 1) { error = "재료 수량은 1 이상이어야 합니다."; return false; }
                if (!usedItems.Add(line.Item)) { error = $"같은 재료 '{line.Item.Id}'가 두 번 들어 있습니다. 한 줄로 합쳐 수량을 늘리세요."; return false; }
            }

            if (stages == null || stages.Count == 0) { error = "제작 단계를 1개 이상 추가하세요."; return false; }

            foreach (CraftRecipeData recipe in FindAllRecipes())
            {
                if (recipe != self && recipe.Id == id)
                {
                    error = $"이미 같은 ID '{id}' 의 레시피가 있습니다: {AssetDatabase.GetAssetPath(recipe)}";
                    return false;
                }
            }

            error = null;
            return true;
        }

        /// <summary>레시피 에셋을 검증하고 만든다. 실패하면 null을 반환하고 error에 이유를 담는다.</summary>
        public static CraftRecipeData CreateRecipe(string id, CraftedItemData result, IList<IngredientInput> ingredients,
            IList<CraftStage> stages, int baseDifficulty, out string error)
        {
            if (!Validate(ref id, result, ingredients, stages, null, out error)) return null;

            EnsureFolder(RootFolder);

            CraftRecipeData asset = CreateInstance<CraftRecipeData>();
            string path = AssetDatabase.GenerateUniqueAssetPath(RootFolder + "/" + id + ".asset");
            AssetDatabase.CreateAsset(asset, path);

            ApplyFields(asset, id, result, ingredients, stages, baseDifficulty);
            error = null;
            return asset;
        }

        /// <summary>기존 레시피 에셋을 수정한다. 실패하면 false와 이유를 반환한다.</summary>
        public static bool UpdateRecipe(CraftRecipeData recipe, string id, CraftedItemData result,
            IList<IngredientInput> ingredients, IList<CraftStage> stages, int baseDifficulty, out string error)
        {
            if (recipe == null) { error = "수정할 레시피가 없습니다."; return false; }
            if (!Validate(ref id, result, ingredients, stages, recipe, out error)) return false;

            // ID가 바뀌면 파일명도 맞춘다 (GUID가 유지되므로 참조는 깨지지 않는다)
            if (recipe.Id != id)
            {
                string path = AssetDatabase.GetAssetPath(recipe);
                string renameError = AssetDatabase.RenameAsset(path, id);
                if (!string.IsNullOrEmpty(renameError))
                {
                    error = "파일 이름을 바꾸지 못했습니다: " + renameError;
                    return false;
                }
            }

            ApplyFields(recipe, id, result, ingredients, stages, baseDifficulty);
            error = null;
            return true;
        }

        private static void ApplyFields(CraftRecipeData asset, string id, CraftedItemData result,
            IList<IngredientInput> ingredients, IList<CraftStage> stages, int baseDifficulty)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("baseDifficulty").intValue = Mathf.Clamp(baseDifficulty, CraftDifficulty.MinLevel, CraftDifficulty.MaxLevel);
            so.FindProperty("result").objectReferenceValue = result;

            SerializedProperty ingredientProperty = so.FindProperty("ingredients");
            ingredientProperty.arraySize = ingredients.Count;
            for (int i = 0; i < ingredients.Count; i++)
            {
                SerializedProperty element = ingredientProperty.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = ingredients[i].Item;
                element.FindPropertyRelative("count").intValue = ingredients[i].Count;
            }

            SerializedProperty stageProperty = so.FindProperty("stages");
            stageProperty.arraySize = stages.Count;
            for (int i = 0; i < stages.Count; i++)
            {
                stageProperty.GetArrayElementAtIndex(i).enumValueIndex = (int)stages[i];
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
