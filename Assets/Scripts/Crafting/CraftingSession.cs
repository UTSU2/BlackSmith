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

        /// <summary>
        /// 제작을 시작할 수 있으면 재료를 모두 소모하고 제작 요청(레시피, 난이도, 어드밴티지, 재료 합산 가격)을 돌려준다.
        /// 투입량이 레시피 요구량보다 많아도 투입한 재료는 전부 소모된다.
        /// </summary>
        public bool TryStart(out CraftRequest request)
        {
            request = null;
            CraftRecipeData recipe = Recipe;
            if (recipe == null) return false;

            foreach (Slot slot in slots)
            {
                if (player.GetCount(slot.ItemId, slot.Grade) < slot.Count) return false;
            }

            // 소모하면 소지품이 바뀌므로 계산에 쓸 입력은 소모 전에 스냅샷으로 만든다.
            List<CraftInput> inputs = GetInputs();
            request = CraftScoring.CreateRequest(recipe, inputs);
            Commit();
            return true;
        }

        /// <summary>투입한 재료를 제작 규칙 계산용 스냅샷 목록으로 돌려준다.</summary>
        public List<CraftInput> GetInputs()
        {
            List<CraftInput> inputs = new List<CraftInput>(slots.Count);
            foreach (Slot slot in slots)
            {
                inputs.Add(new CraftInput(slot.ItemId, slot.Grade, slot.Count));
            }
            return inputs;
        }

        /// <summary>투입한 재료를 소지품에서 모두 뺀다. 제작을 시작하는 순간 소모된다.</summary>
        private void Commit()
        {
            foreach (Slot slot in slots)
            {
                player.TryRemoveItem(slot.ItemId, slot.Count, slot.Grade);
            }
            slots.Clear();
        }

        private void Refresh()
        {
            Recipe = RecipeMatcher.Match(GetInputs());
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
