using System;
using System.Collections.Generic;
using BlackSmith.Data;
using BlackSmith.Items;

namespace BlackSmith.Crafting
{
    /// <summary>
    /// 모루에 재료를 넣고 제작을 시작하기까지의 상태. 순수 C# 클래스라 UI를 모른다.
    /// 투입한 재료는 소지품에서 아직 빼지 않고 여기에만 기록하며, 실제 차감은 Commit에서 한 번에 한다.
    /// </summary>
    public class CraftingSession
    {
        /// <summary>투입한 재료 한 줄: (아이템, 등급)별 수량.</summary>
        public class Slot
        {
            public string ItemId { get; }
            public ItemGrade Grade { get; }
            public int Count { get; internal set; }

            internal Slot(string itemId, ItemGrade grade)
            {
                ItemId = itemId;
                Grade = grade;
            }
        }

        private readonly PlayerData player;
        private readonly List<Slot> slots = new List<Slot>();

        /// <summary>투입 내용이 바뀔 때 호출.</summary>
        public event Action Changed;

        public CraftingSession(PlayerData player)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public IReadOnlyList<Slot> Inserted => slots;

        /// <summary>현재 투입한 재료와 일치하는 레시피. 없으면 null. (플레이어에게는 보여주지 않는다)</summary>
        public CraftRecipeData Recipe { get; private set; }

        public bool CanStart => Recipe != null;

        public bool IsEmpty => slots.Count == 0;

        public int GetInserted(string itemId, ItemGrade grade)
        {
            Slot slot = Find(itemId, grade);
            return slot != null ? slot.Count : 0;
        }

        /// <summary>한 개 투입한다. 보유 수량을 넘기면 false.</summary>
        public bool TryAdd(string itemId, ItemGrade grade)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            if (GetInserted(itemId, grade) >= player.GetCount(itemId, grade)) return false;

            Slot slot = Find(itemId, grade);
            if (slot == null)
            {
                slot = new Slot(itemId, grade);
                slots.Add(slot);
            }
            slot.Count++;
            Refresh();
            return true;
        }

        /// <summary>한 개 뺀다. 투입한 게 없으면 false.</summary>
        public bool TryRemove(string itemId, ItemGrade grade)
        {
            Slot slot = Find(itemId, grade);
            if (slot == null) return false;

            slot.Count--;
            if (slot.Count <= 0) slots.Remove(slot);
            Refresh();
            return true;
        }

        public void Clear()
        {
            if (slots.Count == 0) return;
            slots.Clear();
            Refresh();
        }

        /// <summary>제작을 시작할 수 있으면 레시피와 난이도를 돌려주고 true.</summary>
        public bool TryStart(out CraftRecipeData recipe, out int difficulty)
        {
            recipe = Recipe;
            difficulty = CraftDifficulty.MinLevel;
            if (recipe == null) return false;

            List<ItemGrade> unitGrades = new List<ItemGrade>();
            foreach (Slot slot in slots)
            {
                if (player.GetCount(slot.ItemId, slot.Grade) < slot.Count) return false;
                for (int i = 0; i < slot.Count; i++) unitGrades.Add(slot.Grade);
            }

            difficulty = CraftDifficulty.Calculate(unitGrades);
            Commit();
            return true;
        }

        /// <summary>
        /// 재료를 실제로 소모하는 자리. 소모 시점(시작 시 / 성공 시)과 실패 시 소모 여부가 미정이라
        /// 지금은 소지품에서 빼지 않는다. 정해지면 이 메서드에서만 처리한다.
        /// </summary>
        private void Commit()
        {
            // TODO: 재료 소모 규칙이 정해지면 player.TryRemoveItem으로 slots를 차감한다. (미정, CoreLoop §6)
        }

        private void Refresh()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (Slot slot in slots)
            {
                counts.TryGetValue(slot.ItemId, out int current);
                counts[slot.ItemId] = current + slot.Count;
            }
            Recipe = RecipeMatcher.Match(counts);
            Changed?.Invoke();
        }

        private Slot Find(string itemId, ItemGrade grade)
        {
            foreach (Slot slot in slots)
            {
                if (slot.ItemId == itemId && slot.Grade == grade) return slot;
            }
            return null;
        }
    }
}
