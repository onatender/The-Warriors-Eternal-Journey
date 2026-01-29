using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework; // Vector2 kullanmayıp float X, Y kullanacağız ama namespace dursun

namespace EternalJourney;

public class SaveData
{
    public string PlayerName { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public int Level { get; set; }
    public long Experience { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int Gold { get; set; }
    
    // Envanter
    public List<SavedItem> InventoryItems { get; set; } = new List<SavedItem>();
    
    // Nesne tabanlı ekipman kaydı
    public SavedItem EquippedWeapon { get; set; }
    public SavedItem EquippedArmor { get; set; }
    public SavedItem EquippedShield { get; set; }
    public SavedItem EquippedHelmet { get; set; }
}

public class SavedItem
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public int EnhancementLevel { get; set; } = 0;
    // Slot bilgisini de tutabiliriz ama basitlik için sırayla ekleriz
}

public static class SaveManager
{
    private static string GetSaveDirectory()
    {
        // Exe'nin olduğu yerde "Saves" klasörü
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        return folder;
    }

    private static string GetSavePath(string playerName)
    {
        return Path.Combine(GetSaveDirectory(), $"{playerName}.json");
    }

    public static void SaveGame(SaveData data)
    {
        try
        {
            string path = GetSavePath(data.PlayerName);
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            System.Diagnostics.Debug.WriteLine($"Oyun kaydedildi: {path}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Kaydetme hatası: {ex.Message}");
        }
    }

    public static SaveData LoadGame(string playerName)
    {
        try
        {
            string path = GetSavePath(playerName);
            if (!File.Exists(path)) return null;
            
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SaveData>(json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Yükleme hatası: {ex.Message}");
            return null;
        }
    }
}
