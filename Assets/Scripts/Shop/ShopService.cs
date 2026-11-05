using System;
using System.Collections.Generic;
using BlackSmith.Data;
using BlackSmith.Items;

namespace BlackSmith.Shop
{
    /// <summary>
    /// 상점 구매 로직. UI와 분리된 순수 C# 클래스이며 PlayerData를 주입받아 사용한다.
    /// 판매 목록은 ItemDatabase에서 CanBuy인 아이템 전체다. (상점 재고 규칙은 기획 미정)
    /// </summary>
    public class ShopService
    {
        /// <summary>한 번에 구매할 수 있는 최대 수량. 임시값 (기획 미정).</summary>
        public const int MaxPurchaseQuantity = 99;

        public const int MinPurchaseQuantity = 1;

        private readonly PlayerData player;

        public ShopService(PlayerData player)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public PlayerData Player => player;

        /// <summary>현재 판매 중인 아이템. 정렬은 ID 순 (정렬 기준 미정이라 임시).</summary>
        public List<ItemData> GetStock()
        {
            List<ItemData> stock = new List<ItemData>();
            foreach (ItemData item in ItemDatabase.All)
            {
                if (item.CanBuy) stock.Add(item);
            }
            stock.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return stock;
        }

        public static int ClampQuantity(int quantity)
        {
            return ClampQuantity(quantity, MaxPurchaseQuantity);
        }

        /// <summary>수량을 1 ~ max로 제한한다. max가 1 미만이면 1.</summary>
        public static int ClampQuantity(int quantity, int max)
        {
            if (max < MinPurchaseQuantity) max = MinPurchaseQuantity;
            if (quantity < MinPurchaseQuantity) return MinPurchaseQuantity;
            if (quantity > max) return max;
            return quantity;
        }

        /// <summary>총 구매 금액. int 범위를 넘으면 int.MaxValue로 제한한다.</summary>
        public int GetTotalPrice(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return 0;
            long total = (long)item.BuyPrice * quantity;
            return (int)Math.Min(total, int.MaxValue);
        }

        public bool CanBuy(ItemData item, int quantity)
        {
            if (item == null || !item.CanBuy) return false;
            if (quantity < MinPurchaseQuantity || quantity > MaxPurchaseQuantity) return false;
            return player.CanAfford(GetTotalPrice(item, quantity));
        }

        // ---- 판매 ----

        /// <summary>판매 목록의 한 항목: 보유 중인 (아이템, 등급, 수량).</summary>
        public readonly struct SellEntry
        {
            public readonly ItemData Item;
            public readonly ItemGrade Grade;
            public readonly int Count;

            public SellEntry(ItemData item, ItemGrade grade, int count)
            {
                Item = item;
                Grade = grade;
                Count = count;
            }
        }

        /// <summary>판매할 수 있는 보유 아이템. 정렬은 ID, 등급 순 (정렬 기준 미정이라 임시).</summary>
        public List<SellEntry> GetSellList()
        {
            List<SellEntry> list = new List<SellEntry>();
            foreach (InventoryEntry entry in player.Items)
            {
                if (entry.Count <= 0) continue;
                if (!ItemDatabase.TryGet(entry.ItemId, out ItemData item)) continue;
                if (!item.CanSell) continue;
                list.Add(new SellEntry(item, entry.Grade, entry.Count));
            }
            list.Sort((a, b) =>
            {
                int byId = string.CompareOrdinal(a.Item.Id, b.Item.Id);
                return byId != 0 ? byId : a.Grade.CompareTo(b.Grade);
            });
            return list;
        }

        public int GetOwnedCount(ItemData item, ItemGrade grade)
        {
            return item == null ? 0 : player.GetCount(item.Id, grade);
        }

        /// <summary>총 판매 금액. int 범위를 넘으면 int.MaxValue로 제한한다.</summary>
        public int GetSellTotalPrice(ItemData item, ItemGrade grade, int quantity)
        {
            if (item == null || quantity <= 0) return 0;
            long total = (long)item.GetSellPrice(grade) * quantity;
            return (int)Math.Min(total, int.MaxValue);
        }

        public bool CanSell(ItemData item, ItemGrade grade, int quantity)
        {
            if (item == null || !item.CanSell) return false;
            return player.HasItem(item.Id, grade, quantity);
        }

        /// <summary>판매한다. 보유 수량이 부족하면 아무 변화 없이 false.</summary>
        public bool TrySell(ItemData item, ItemGrade grade, int quantity)
        {
            if (!CanSell(item, grade, quantity)) return false;

            int total = GetSellTotalPrice(item, grade, quantity);
            if (!player.TryRemoveItem(item.Id, quantity, grade)) return false;

            player.AddMoney(total);
            return true;
        }

        /// <summary>구매한다. 조건이 맞지 않으면 아무 변화 없이 false.</summary>
        public bool TryBuy(ItemData item, int quantity)
        {
            if (!CanBuy(item, quantity)) return false;

            int total = GetTotalPrice(item, quantity);
            if (!player.TrySpendMoney(total)) return false;

            if (!player.AddItem(item.Id, quantity, item.DefaultGrade))
            {
                // 아이템 지급에 실패하면 차감한 금액을 되돌린다.
                player.AddMoney(total);
                return false;
            }
            return true;
        }
    }
}
