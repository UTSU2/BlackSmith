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

        /// <summary>판매 목록의 한 항목: 보유 중인 (아이템, 개별 상태, 수량).</summary>
        public readonly struct SellEntry
        {
            public readonly ItemData Item;
            public readonly ItemVariant Variant;
            public readonly int Count;

            public ItemGrade Grade => Variant.Grade;

            public SellEntry(ItemData item, ItemVariant variant, int count)
            {
                Item = item;
                Variant = variant;
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
                list.Add(new SellEntry(item, entry.Variant, entry.Count));
            }
            list.Sort((a, b) =>
            {
                int byId = string.CompareOrdinal(a.Item.Id, b.Item.Id);
                if (byId != 0) return byId;
                int byGrade = a.Variant.Grade.CompareTo(b.Variant.Grade);
                if (byGrade != 0) return byGrade;
                int byQuality = a.Variant.Quality.CompareTo(b.Variant.Quality);
                return byQuality != 0 ? byQuality : a.Variant.IngredientValue.CompareTo(b.Variant.IngredientValue);
            });
            return list;
        }

        public int GetOwnedCount(ItemData item, ItemVariant variant)
        {
            return item == null ? 0 : player.GetCount(item.Id, variant);
        }

        /// <summary>판매 단가. 광물은 Common 기준 판매가 x 등급 배율, 제작품은 재료 합산 가격 x 품질 배율.</summary>
        public int GetSellUnitPrice(ItemData item, ItemVariant variant)
        {
            return item == null ? 0 : item.GetSellUnitPrice(variant);
        }

        /// <summary>총 판매 금액. int 범위를 넘으면 int.MaxValue로 제한한다.</summary>
        public int GetSellTotalPrice(ItemData item, ItemVariant variant, int quantity)
        {
            if (item == null || quantity <= 0) return 0;
            long total = (long)item.GetSellUnitPrice(variant) * quantity;
            return (int)Math.Min(total, int.MaxValue);
        }

        public bool CanSell(ItemData item, ItemVariant variant, int quantity)
        {
            if (item == null || !item.CanSell) return false;
            return player.HasItem(item.Id, variant, quantity);
        }

        /// <summary>판매한다. 보유 수량이 부족하면 아무 변화 없이 false.</summary>
        public bool TrySell(ItemData item, ItemVariant variant, int quantity)
        {
            if (!CanSell(item, variant, quantity)) return false;

            int total = GetSellTotalPrice(item, variant, quantity);
            if (!player.TryRemoveItem(item.Id, quantity, variant)) return false;

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
