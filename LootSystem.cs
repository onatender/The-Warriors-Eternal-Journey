using System;
using System.Collections.Generic;

namespace EternalJourney;

public class LootItem
{
    public int ItemId { get; set; }
    public double Chance { get; set; } // 0.0 - 100.0 arası yüzde
    public int MinQuantity { get; set; } = 1;
    public int MaxQuantity { get; set; } = 1;

    public LootItem(int itemId, double chance, int min = 1, int max = 1)
    {
        ItemId = itemId;
        Chance = chance;
        MinQuantity = min;
        MaxQuantity = max;
    }
}

public class LootTable
{
    private List<LootItem> _items = new List<LootItem>();
    private Random _random = new Random();

    public void AddItem(int itemId, double chance, int min=1, int max=1)
    {
        _items.Add(new LootItem(itemId, chance, min, max));
    }

    public List<(int ItemId, int Quantity)> GenerateLoot()
    {
        List<(int, int)> drops = new List<(int, int)>();
        
        foreach (var item in _items)
        {
            double roll = _random.NextDouble() * 100.0;
            if (roll <= item.Chance)
            {
                int qty = _random.Next(item.MinQuantity, item.MaxQuantity + 1);
                drops.Add((item.ItemId, qty));
            }
        }
        
        return drops;
    }
}

public static class LootManager
{
    private static Dictionary<EnemyType, LootTable> _lootTables = new Dictionary<EnemyType, LootTable>();

    public static void Initialize()
    {
        // Goblin Drop Havuzu
        // Goblin Drop Havuzu (Başlangıç)
        LootTable goblinLoot = new LootTable();
        goblinLoot.AddItem(25, 4.0); // %4 Can İksiri
        goblinLoot.AddItem(40, 2.0); // %2 Tahta Kalkan
        goblinLoot.AddItem(20, 2.0); // %2 Şans Tılsımı %10
        goblinLoot.AddItem(99, 1.0); // %1 Güçlendirme Taşı
        _lootTables[EnemyType.Goblin] = goblinLoot;

        // Spider Drop Havuzu (Seviye 2 - Güçlendirildi)
        LootTable spiderLoot = new LootTable();
        spiderLoot.AddItem(99, 5.0); // %5 Güçlendirme Taşı
        spiderLoot.AddItem(50, 4.0); // %4 Deri Kask
        spiderLoot.AddItem(40, 3.0); // %3 Tahta Kalkan
        spiderLoot.AddItem(20, 5.0); // %5 Şans Tılsımı %10
        spiderLoot.AddItem(25, 8.0); // %8 Can İksiri
        spiderLoot.AddItem(4, 2.0);  // %2 Demir Kılıç (Yeni)
        _lootTables[EnemyType.Spider] = spiderLoot;
        
        // Skeleton Drop Havuzu (Seviye 3 - Güçlendirildi)
        LootTable skeletonLoot = new LootTable();
        skeletonLoot.AddItem(41, 4.0); // %4 Demir Kalkan 
        skeletonLoot.AddItem(99, 10.0); // %10 Güçlendirme Taşı
        skeletonLoot.AddItem(21, 3.0); // %3 Şans Tılsımı %25
        skeletonLoot.AddItem(30, 1.0); // %1 Muska
        skeletonLoot.AddItem(25, 5.0); // %5 Can İksiri
        skeletonLoot.AddItem(4, 5.0);  // %5 Demir Kılıç
        _lootTables[EnemyType.Skeleton] = skeletonLoot;

        // Demon Drop Havuzu (Seviye 4 - Final - Güçlendirildi)
        LootTable demonLoot = new LootTable();
        demonLoot.AddItem(51, 5.0); // %5 Demir Kask
        demonLoot.AddItem(41, 4.0); // %4 Demir Kalkan
        demonLoot.AddItem(99, 15.0); // %15 Güçlendirme Taşı
        demonLoot.AddItem(22, 2.0); // %2 Şans Tılsımı %50
        demonLoot.AddItem(31, 1.0); // %1 Büyülü Muska
        demonLoot.AddItem(3, 3.0);  // %3 Deri Zırh
        demonLoot.AddItem(25, 8.0); // %8 Can İksiri
        demonLoot.AddItem(4, 8.0);  // %8 Demir Kılıç
        // Efsanevi şans
        demonLoot.AddItem(11, 0.5); // %0.5 Ejderha Zırhı
        _lootTables[EnemyType.Demon] = demonLoot;
    }

    public static List<(int ItemId, int Quantity)> GetLoot(EnemyType type, int mapIndex = 1)
    {
        List<(int, int)> loot = new List<(int, int)>();
        
        if (_lootTables.ContainsKey(type))
        {
            loot = _lootTables[type].GenerateLoot();
        }
        
        // --- MAP SCALING BONUS ---
        // Harita ilerledikçe ekstra drop şansı
        Random rnd = new Random();
        
        // Her map artışında %5 ekstra Potion şansı
        if (rnd.Next(100) < (mapIndex * 5)) 
        {
            loot.Add((25, 1)); 
        }

        // Her map artışında %3 ekstra Taş şansı
        if (rnd.Next(100) < (mapIndex * 3))
        {
            loot.Add((99, 1));
        }
        
        // Map 5 ve üzeri ise ekstra şans
        if (mapIndex >= 5 && rnd.Next(100) < 5)
        {
             loot.Add((20, 1)); // %5 Şans Tılsımı
        }
        
        return loot;
    }
}
