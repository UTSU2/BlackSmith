using System;
using System.Collections.Generic;
using UnityEngine;
using BlackSmith.Items;

namespace BlackSmith.Data
{
    /// <summary>
    /// 플레이어 데이터: 보유 금액과 소지 아이템.
    /// 순수 C# 직렬화 클래스라 JsonUtility로 그대로 저장할 수 있다. (저장/불러오기는 아직 없음)
    /// 소지 아이템은 (아이템 ID, 등급, 품질, 재료 합산 가격)별 수량 목록이며 칸 수 제한과 maxStack은 적용하지 않는다.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        /// <summary>새 게임을 시작할 때 가진 금액. 시작 자금은 튜토리얼 보상(TutorialRewardMoney)으로 받는다.</summary>
        public const int DefaultStartMoney = 0;

        /// <summary>튜토리얼을 마치거나 건너뛰었을 때 한 번 받는 금액.</summary>
        public const int TutorialRewardMoney = 1000;

        [SerializeField] private int money = DefaultStartMoney;
        // 튜토리얼이 아직 끝나지 않았는가. "대기 중"을 true로 둔 이유는, 이 필드가 없던 기존 저장 파일이 false(완료)로 읽혀
        // 불러올 때 보상이 다시 지급되지 않게 하기 위해서다.
        [SerializeField] private bool tutorialPending;
        [SerializeField] private List<InventoryEntry> items = new List<InventoryEntry>();
        // 레시피별 누적 숙련도. 이 필드가 없는 기존 저장 파일은 빈 목록으로 읽힌다.
        [SerializeField] private List<ProficiencyEntry> proficiencies = new List<ProficiencyEntry>();
        // 도감에 해금한 레시피 id. 도감 번호가 아니라 id로 저장하므로 번호가 바뀌어도 해금이 다른 레시피로 옮겨 가지 않는다.
        [SerializeField] private List<string> unlockedRecipes = new List<string>();
        // 도감 목록을 준비했는가. 이 필드가 없던 기존 저장 파일은 false로 읽혀, 처음 불러올 때 숙련도 기록으로 해금을 채운다.
        [SerializeField] private bool codexInitialized;

        /// <summary>금액이 바뀔 때 호출 (새 금액).</summary>
        [field: NonSerialized] public event Action<int> OnMoneyChanged;

        /// <summary>소지 아이템이 바뀔 때 호출.</summary>
        [field: NonSerialized] public event Action OnInventoryChanged;

        // 저장 이후 금액·소지품이 바뀌었는지. [SerializeField]가 아니므로 저장 파일에 들어가지 않는다.
        private bool hasUnsavedChanges;

        public int Money => money;

        /// <summary>튜토리얼이 아직 끝나지 않았는가. 기존 저장 파일은 항상 false(완료)다.</summary>
        public bool IsTutorialPending => tutorialPending;

        /// <summary>
        /// 새 게임에서 튜토리얼을 시작 상태로 만들고, startMoney가 0보다 크면 튜토리얼용 시작 지급금으로 바로 받는다.
        /// (튜토리얼에서 쓰는 재료 구매가 합. 계산은 TutorialConfig.CalculateStartMoney) 완료 보상은 CompleteTutorial에서 따로 받는다.
        /// </summary>
        public void StartTutorial(int startMoney)
        {
            tutorialPending = true;
            if (startMoney > 0) AddMoney(startMoney);
        }

        /// <summary>
        /// 튜토리얼을 마쳤거나 건너뛰었을 때 부른다. 진행 중일 때 한 번만 실행한다. 튜토리얼에서 번 돈(철검 판매금 등)은 회수하고
        /// 소지금을 정확히 TutorialRewardMoney로 맞춘다. 이미 끝났으면 아무것도 하지 않는다. (완료든 스킵이든 같은 결과)
        /// </summary>
        public void CompleteTutorial()
        {
            if (!tutorialPending) return;
            tutorialPending = false;
            TrySpendMoney(money);
            AddMoney(TutorialRewardMoney);
        }

        /// <summary>마지막 저장(또는 불러오기·새 게임) 이후 금액이나 소지품이 바뀌었는가. 타이틀로 나갈 때 확인 창을 띄울지 정하는 데 쓴다.</summary>
        public bool HasUnsavedChanges => hasUnsavedChanges;

        /// <summary>저장에 성공했을 때 SaveSlotManager가 부른다.</summary>
        public void MarkSaved()
        {
            hasUnsavedChanges = false;
        }
        public IReadOnlyList<InventoryEntry> Items => items;

        // ---- 금액 ----

        public bool CanAfford(int amount) => amount >= 0 && money >= amount;

        public void AddMoney(int amount)
        {
            if (amount <= 0) return;
            money = (int)Math.Min((long)money + amount, int.MaxValue);
            hasUnsavedChanges = true;
            OnMoneyChanged?.Invoke(money);
        }

        /// <summary>금액을 차감한다. 부족하면 아무것도 하지 않고 false.</summary>
        public bool TrySpendMoney(int amount)
        {
            if (amount < 0 || money < amount) return false;
            if (amount == 0) return true;
            money -= amount;
            hasUnsavedChanges = true;
            OnMoneyChanged?.Invoke(money);
            return true;
        }

        // ---- 아이템 ----

        // 등급만 받는 메서드는 품질이 없는 아이템(광물)용이다. 완성품은 ItemVariant를 받는 메서드를 쓴다.

        public int GetCount(string itemId, ItemGrade grade) => GetCount(itemId, new ItemVariant(grade));

        public int GetCount(string itemId, ItemVariant variant)
        {
            InventoryEntry entry = Find(itemId, variant);
            return entry != null ? entry.Count : 0;
        }

        /// <summary>등급·품질 구분 없이 해당 아이템의 모든 ItemVariant 수량을 합한다. 없으면 0.</summary>
        public int GetTotalCount(string itemId)
        {
            long total = 0;
            foreach (InventoryEntry entry in items)
            {
                if (entry.ItemId == itemId) total += entry.Count;
            }
            return (int)Math.Min(total, int.MaxValue);
        }

        public bool HasItem(string itemId, ItemGrade grade, int count = 1) => HasItem(itemId, new ItemVariant(grade), count);

        public bool HasItem(string itemId, ItemVariant variant, int count = 1)
        {
            return count > 0 && GetCount(itemId, variant) >= count;
        }

        /// <summary>아이템을 추가한다. 존재하지 않는 ID거나 수량이 0 이하이면 false.</summary>
        public bool AddItem(string itemId, int count, ItemGrade grade) => AddItem(itemId, count, new ItemVariant(grade));

        /// <summary>개별 상태(등급, 품질, 재료 합산 가격)가 같은 항목에 합쳐서 추가한다.</summary>
        public bool AddItem(string itemId, int count, ItemVariant variant)
        {
            if (count <= 0) return false;
            if (!ItemDatabase.TryGet(itemId, out _))
            {
                Debug.LogWarning($"[PlayerData] 존재하지 않는 아이템 ID라 추가하지 않았습니다: '{itemId}'");
                return false;
            }

            InventoryEntry entry = Find(itemId, variant);
            if (entry != null)
            {
                entry.SetCount((int)Math.Min((long)entry.Count + count, int.MaxValue));
            }
            else
            {
                items.Add(new InventoryEntry(itemId, variant, count));
            }

            hasUnsavedChanges = true;
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>아이템을 제거한다. 수량이 부족하면 아무것도 하지 않고 false. 0이 되면 항목을 삭제한다.</summary>
        public bool TryRemoveItem(string itemId, int count, ItemGrade grade) => TryRemoveItem(itemId, count, new ItemVariant(grade));

        public bool TryRemoveItem(string itemId, int count, ItemVariant variant)
        {
            if (count <= 0) return false;
            InventoryEntry entry = Find(itemId, variant);
            if (entry == null || entry.Count < count) return false;

            if (entry.Count == count) items.Remove(entry);
            else entry.SetCount(entry.Count - count);

            hasUnsavedChanges = true;
            OnInventoryChanged?.Invoke();
            return true;
        }

        // ---- 숙련도 ----

        /// <summary>레시피의 누적 숙련도. 기록이 없으면 0.</summary>
        public int GetProficiencyPoints(string recipeId)
        {
            ProficiencyEntry entry = FindProficiency(recipeId);
            return entry != null ? entry.Points : 0;
        }

        /// <summary>누적 숙련도를 더한다(maxPoints를 넘지 않음). 실제로 바뀐 양을 돌려준다.</summary>
        public int AddProficiencyPoints(string recipeId, int amount, int maxPoints)
        {
            if (string.IsNullOrEmpty(recipeId) || amount <= 0) return 0;

            ProficiencyEntry entry = FindProficiency(recipeId);
            int before = entry != null ? entry.Points : 0;
            int after = (int)Math.Min((long)before + amount, maxPoints);
            if (after <= before) return 0;

            if (entry != null) entry.SetPoints(after);
            else
            {
                if (proficiencies == null) proficiencies = new List<ProficiencyEntry>();
                proficiencies.Add(new ProficiencyEntry(recipeId, after));
            }

            hasUnsavedChanges = true;
            return after - before;
        }

        // ---- 도감 (레시피 해금) ----

        /// <summary>
        /// 도감 목록을 처음 한 번 준비한다. 도감이 생기기 전의 저장 파일은 숙련도 기록이 있는 레시피를 해금된 것으로 본다.
        /// 새 게임·불러오기 때 GameSession이 부른다. 저장하지 않은 변경으로 보지 않는다. (docs/V2.md §3)
        /// </summary>
        public void EnsureCodexInitialized()
        {
            if (codexInitialized) return;
            codexInitialized = true;
            if (unlockedRecipes == null) unlockedRecipes = new List<string>();
            if (proficiencies == null) return;
            foreach (ProficiencyEntry entry in proficiencies)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.RecipeId) && !unlockedRecipes.Contains(entry.RecipeId))
                {
                    unlockedRecipes.Add(entry.RecipeId);
                }
            }
        }

        public bool IsRecipeUnlocked(string recipeId)
        {
            return !string.IsNullOrEmpty(recipeId) && unlockedRecipes != null && unlockedRecipes.Contains(recipeId);
        }

        /// <summary>레시피를 도감에 해금한다. 새로 해금했으면 true, 이미 해금돼 있으면 false.</summary>
        public bool UnlockRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return false;
            if (unlockedRecipes == null) unlockedRecipes = new List<string>();
            if (unlockedRecipes.Contains(recipeId)) return false;
            unlockedRecipes.Add(recipeId);
            hasUnsavedChanges = true;
            return true;
        }

        private ProficiencyEntry FindProficiency(string recipeId)
        {
            if (proficiencies == null) return null;
            foreach (ProficiencyEntry entry in proficiencies)
            {
                if (entry != null && entry.RecipeId == recipeId) return entry;
            }
            return null;
        }

        private InventoryEntry Find(string itemId, ItemVariant variant)
        {
            foreach (InventoryEntry entry in items)
            {
                if (entry.ItemId == itemId && entry.Variant.Equals(variant)) return entry;
            }
            return null;
        }
    }
}
