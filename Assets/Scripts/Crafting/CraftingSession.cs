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
        /// <summary>투입한 재료 한 줄: 아이템별 수량.</summary>
        public class Slot
        {
            public string ItemId { get; }
            public int Count { get; internal set; }

            internal Slot(string itemId)
            {
                ItemId = itemId;
            }
        }

        private readonly PlayerData player;
        private readonly UnknownCraftSettings unknownCraft;
        private readonly List<Slot> slots = new List<Slot>();

        /// <summary>투입 내용이 바뀔 때 호출.</summary>
        public event Action Changed;

        /// <param name="unknownCraft">레시피와 일치하지 않는 조합의 설정. null이면 일치하는 조합만 제작할 수 있다.</param>
        public CraftingSession(PlayerData player, UnknownCraftSettings unknownCraft = null)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.unknownCraft = unknownCraft;
        }

        public IReadOnlyList<Slot> Inserted => slots;

        /// <summary>현재 투입한 재료와 일치하는 레시피. 없으면 null. (플레이어에게는 보여주지 않는다)</summary>
        public CraftRecipeData Recipe { get; private set; }

        /// <summary>
        /// 재료가 하나 이상이면 시작할 수 있다. 레시피와 일치하지 않으면 알 수 없는 제작품이 된다.
        /// 단서를 주지 않으므로 이 값으로 일치 여부를 알 수 없다(설정 에셋이 없을 때만 일치 여부와 같아진다).
        /// </summary>
        public bool CanStart => !IsEmpty && (Recipe != null || HasUnknownCraft);

        private bool HasUnknownCraft => unknownCraft != null && unknownCraft.Result != null;

        public bool IsEmpty => slots.Count == 0;

        public int GetInserted(string itemId)
        {
            Slot slot = Find(itemId);
            return slot != null ? slot.Count : 0;
        }

        /// <summary>한 개 투입한다. 보유 수량을 넘기면 false.</summary>
        public bool TryAdd(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            if (GetInserted(itemId) >= GetOwned(itemId)) return false;

            Slot slot = Find(itemId);
            if (slot == null)
            {
                slot = new Slot(itemId);
                slots.Add(slot);
            }
            slot.Count++;
            Refresh();
            return true;
        }

        /// <summary>한 개 뺀다. 투입한 게 없으면 false.</summary>
        public bool TryRemove(string itemId)
        {
            Slot slot = Find(itemId);
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
        /// 제작을 시작할 수 있으면 재료를 모두 소모하고 제작 요청(레시피, 난이도, 재료 합산 가격)을 돌려준다.
        /// 일치하는 레시피가 없으면 알 수 없는 제작 요청(레시피 null)을 만든다. 투입한 재료는 전부 소모된다.
        /// </summary>
        public bool TryStart(out CraftRequest request)
        {
            request = null;
            if (!CanStart) return false;
            CraftRecipeData recipe = Recipe;

            foreach (Slot slot in slots)
            {
                if (GetOwned(slot.ItemId) < slot.Count) return false;
            }

            // 소모하면 소지품이 바뀌므로 계산에 쓸 입력은 소모 전에 스냅샷으로 만든다.
            List<CraftInput> inputs = GetInputs();
            request = recipe != null
                ? CraftScoring.CreateRequest(recipe, inputs)
                : CraftScoring.CreateUnknownRequest(unknownCraft, inputs);
            Commit();
            return true;
        }

        /// <summary>투입한 재료를 제작 규칙 계산용 스냅샷 목록으로 돌려준다.</summary>
        public List<CraftInput> GetInputs()
        {
            List<CraftInput> inputs = new List<CraftInput>(slots.Count);
            foreach (Slot slot in slots)
            {
                inputs.Add(new CraftInput(slot.ItemId, slot.Count));
            }
            return inputs;
        }

        /// <summary>투입한 재료를 소지품에서 모두 뺀다. 제작을 시작하는 순간 소모된다.</summary>
        private void Commit()
        {
            foreach (Slot slot in slots)
            {
                if (ItemDatabase.TryGet(slot.ItemId, out ItemData item))
                {
                    player.TryRemoveItem(slot.ItemId, slot.Count, new ItemVariant(item.DefaultGrade));
                }
            }
            slots.Clear();
        }

        /// <summary>보유 수량. 광물은 아이템 데이터의 등급 하나로만 보관되므로 그 등급의 항목을 센다. 모르는 아이템이면 0.</summary>
        private int GetOwned(string itemId)
        {
            return ItemDatabase.TryGet(itemId, out ItemData item)
                ? player.GetCount(itemId, new ItemVariant(item.DefaultGrade))
                : 0;
        }

        private void Refresh()
        {
            Recipe = RecipeMatcher.Match(GetInputs());
            Changed?.Invoke();
        }

        private Slot Find(string itemId)
        {
            foreach (Slot slot in slots)
            {
                if (slot.ItemId == itemId) return slot;
            }
            return null;
        }
    }
}
