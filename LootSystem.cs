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
        LootTable goblinLoot = new LootTable();
        // goblinLoot.AddItem(1, 10.0); // %10 ihtimalle Tahta Kılıç (şimdilik kapalı)
        goblinLoot.AddItem(99, 2.0); // %2 ihtimalle Güçlendirme Taşı (2 katına çıktı)
        goblinLoot.AddItem(20, 1.0); // %1 Şans Tılsımı %10
        goblinLoot.AddItem(21, 0.5); // %0.5 Şans Tılsımı %25
        goblinLoot.AddItem(30, 2.0); // %2 Muska
        _lootTables[EnemyType.Goblin] = goblinLoot;

        // Demon Drop Havuzu
        LootTable demonLoot = new LootTable();
        demonLoot.AddItem(3, 5.0); // %5 ihtimalle Deri Zırh
        demonLoot.AddItem(99, 4.0); // %4 Güçlendirme Taşı
        demonLoot.AddItem(22, 0.25); // %0.25 Şans Tılsımı %50
        demonLoot.AddItem(31, 0.2); // %0.2 Büyülü Muska
        _lootTables[EnemyType.Demon] = demonLoot;
    }

    public static List<(int ItemId, int Quantity)> GetLoot(EnemyType type)
    {
        if (_lootTables.ContainsKey(type))
        {
            return _lootTables[type].GenerateLoot();
        }
        return new List<(int, int)>();
    }
}
