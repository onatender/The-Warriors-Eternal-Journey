using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EternalJourney;

public enum ItemType
{
    None,
    Weapon,
    Armor,
    Material
}

public enum ItemRarity
{
    Common,     // Beyaz
    Uncommon,   // Yeşil
    Rare,       // Mavi
    Epic,       // Mor
    Legendary   // Turuncu
}

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ItemType Type { get; set; }
    public ItemRarity Rarity { get; set; }
    public int RequiredLevel { get; set; }
    public int EnhancementLevel { get; set; } // +0, +1, +2...
    
    // Silah özellikleri
    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }
    public int AttackSpeed { get; set; }
    
    // Zırh özellikleri
    public int Defense { get; set; }
    public int Health { get; set; }
    
    // Görsel
    public Texture2D Icon { get; set; }
    public Color IconColor { get; set; } = Color.White;
    
    public Item()
    {
        Name = "Unknown";
        Type = ItemType.None;
        Rarity = ItemRarity.Common;
    }
    
    public string GetDisplayName()
    {
        string enhanceText = EnhancementLevel > 0 ? $" +{EnhancementLevel}" : "";
        return $"{Name}{enhanceText}";
    }
    
    public Color GetRarityColor()
    {
        return Rarity switch
        {
            ItemRarity.Common => new Color(200, 200, 200),
            ItemRarity.Uncommon => new Color(100, 200, 100),
            ItemRarity.Rare => new Color(100, 150, 255),
            ItemRarity.Epic => new Color(180, 100, 255),
            ItemRarity.Legendary => new Color(255, 150, 50),
            _ => Color.White
        };
    }

    public Item Clone()
    {
        return new Item
        {
            Id = this.Id,
            Name = this.Name,
            Description = this.Description,
            Type = this.Type,
            Rarity = this.Rarity,
            RequiredLevel = this.RequiredLevel,
            EnhancementLevel = this.EnhancementLevel,
            MinDamage = this.MinDamage,
            MaxDamage = this.MaxDamage,
            AttackSpeed = this.AttackSpeed,
            Defense = this.Defense,
            Health = this.Health,
            Icon = this.Icon,
            IconColor = this.IconColor
        };
    }

    public void UpgradeSuccess()
    {
        EnhancementLevel++;
        
        // Silah ise hasar artır
        if (Type == ItemType.Weapon)
        {
            MinDamage = (int)(MinDamage * 1.2f) + 1; // %20 artış
            MaxDamage = (int)(MaxDamage * 1.2f) + 1;
        }
        // Zırh ise defans/can artır
        else if (Type == ItemType.Armor)
        {
            Defense = (int)(Defense * 1.2f) + 1;
            Health = (int)(Health * 1.2f) + 5;
        }
    }

    public void Downgrade()
    {
        if (EnhancementLevel <= 0) return;
        EnhancementLevel--;
        
        // Formülün tersi: (Değer - Ekleme) / 1.2
        if (Type == ItemType.Weapon)
        {
            MinDamage = (int)((MinDamage - 1) / 1.2f);
            MaxDamage = (int)((MaxDamage - 1) / 1.2f);
        }
        // Zırh 
        else if (Type == ItemType.Armor)
        {
            Defense = (int)((Defense - 1) / 1.2f);
            Health = (int)((Health - 5) / 1.2f);
        }
    }
}

// Item Database - Oyundaki tüm itemlar
public static class ItemDatabase
{
    private static Item[] _items;
    private static bool _initialized = false;
    
    public static void Initialize(GraphicsDevice graphicsDevice)
    {
        if (_initialized) return;
        
        _items = new Item[100]; // Max 100 farklı item
        
        // ID 1: Tahta Kılıç
        _items[1] = new Item
        {
            Id = 1,
            Name = "Tahta Kilic",
            Description = "Basit bir tahta kilic",
            Type = ItemType.Weapon,
            Rarity = ItemRarity.Common,
            RequiredLevel = 1,
            EnhancementLevel = 0,
            MinDamage = 5,
            MaxDamage = 10,
            AttackSpeed = 50,
            Icon = CreateWeaponIcon(graphicsDevice, new Color(139, 90, 43)) // Kahverengi tahta
        };
        
        // ID 2: Kumaş Zırh
        _items[2] = new Item
        {
            Id = 2,
            Name = "Kumas Zirh",
            Description = "Basit kumas zirh",
            Type = ItemType.Armor,
            Rarity = ItemRarity.Common,
            RequiredLevel = 1,
            EnhancementLevel = 0,
            Defense = 5,
            Health = 10,
            Icon = CreateArmorIcon(graphicsDevice, new Color(150, 120, 90))
        };
        
        // ID 3: Deri Zırh
        _items[3] = new Item
        {
            Id = 3,
            Name = "Deri Zirh",
            Description = "Dayanikli deri zirh",
            Type = ItemType.Armor,
            Rarity = ItemRarity.Uncommon, // Biraz daha iyi
            RequiredLevel = 1,
            EnhancementLevel = 0,
            Defense = 10,
            Health = 20,
            Icon = CreateArmorIcon(graphicsDevice, new Color(120, 80, 50)) // Koyu kahve
        };
        
        // ID 99: Güçlendirme Taşı
        _items[99] = new Item
        {
            Id = 99,
            Name = "Guclendirme Tasi",
            Description = "Bu tas esyalarinin seviyesini yukseltmenize yarar.",
            Type = ItemType.Material,
            Rarity = ItemRarity.Rare,
            RequiredLevel = 1,
            Icon = CreateMaterialIcon(graphicsDevice, Color.Cyan)
        };
        
        // ID 10: Ebedi Kılıç (Legendary)
        _items[10] = new Item
        {
            Id = 10,
            Name = "Ebedi Kilic",
            Description = "Efsanelere konu olmus, sonsuz guc barindiran kilic.",
            Type = ItemType.Weapon,
            Rarity = ItemRarity.Legendary,
            RequiredLevel = 1, // Hileli olduğu için level 1
            EnhancementLevel = 0, 
            MinDamage = 100,
            MaxDamage = 150,
            AttackSpeed = 100, // Çok Hızlı
            Icon = CreateWeaponIcon(graphicsDevice, Color.Purple)
        };

        // ID 11: Ejderha Zırhı (Legendary)
        _items[11] = new Item
        {
            Id = 11,
            Name = "Ejderha Zirhi",
            Description = "Ejderha pullarindan yapilmis, asilmaz bir zirh.",
            Type = ItemType.Armor,
            Rarity = ItemRarity.Legendary,
            RequiredLevel = 1,
            EnhancementLevel = 0,
            Defense = 100,
            Health = 1000,
            Icon = CreateArmorIcon(graphicsDevice, Color.DarkRed)
        };
        
        // --- EFSUN & SUPPORT ITEMLARI ---
        
        // ID 20: Şans Tılsımı %10
        _items[20] = new Item
        {
            Id = 20,
            Name = "Sans Tilsimi (+%10)",
            Description = "Yukseltme sansini %10 artirir.",
            Type = ItemType.Material,
            Rarity = ItemRarity.Common,
            Icon = CreateScrollIcon(graphicsDevice, new Color(240, 230, 200), new Color(50, 150, 50)) // Yeşil mühürlü parşömen
        };
        
        // ID 21: Şans Tılsımı %25
        _items[21] = new Item
        {
            Id = 21,
            Name = "Sans Tilsimi (+%25)",
            Description = "Yukseltme sansini %25 artirir.",
            Type = ItemType.Material,
            Rarity = ItemRarity.Rare,
            Icon = CreateScrollIcon(graphicsDevice, new Color(255, 250, 220), new Color(50, 50, 150)) // Mavi mühürlü
        };
        
        // ID 22: Şans Tılsımı %50
        _items[22] = new Item
        {
            Id = 22,
            Name = "Sans Tilsimi (+%50)",
            Description = "Yukseltme sansini %50 artirir!",
            Type = ItemType.Material,
            Rarity = ItemRarity.Legendary,
            Icon = CreateScrollIcon(graphicsDevice, new Color(255, 215, 0), new Color(200, 0, 0)) // Altın kağıt, kırmızı mühür
        };
        
        // ID 30: Muska
        _items[30] = new Item
        {
            Id = 30,
            Name = "Muska",
            Description = "Basarisiz yukseltmede esyanin yok olmasini engeller, ancak seviyesi duser.",
            Type = ItemType.Material,
            Rarity = ItemRarity.Rare,
            Icon = CreateAmuletIcon(graphicsDevice, Color.Silver, Color.Gray) // Gümüş çerçeve, gri taş
        };
        
        // ID 31: Büyülü Muska
        _items[31] = new Item
        {
            Id = 31,
            Name = "Buyulu Muska",
            Description = "Basarisiz yukseltmede esyayi tamamen korur.",
            Type = ItemType.Material,
            Rarity = ItemRarity.Legendary,
            Icon = CreateAmuletIcon(graphicsDevice, Color.Gold, Color.Purple) // Altın çerçeve, mor taş
        };

        _initialized = true;
    }
    
    private static Texture2D CreateMaterialIcon(GraphicsDevice gd, Color color)
    {
        // Fallback or generic
        return CreateCrystalIcon(gd, color);
    }

    private static Texture2D CreateCrystalIcon(GraphicsDevice gd, Color color)
    {
        // === KRİSTAL / TAŞ ===
        int size = 32;
        Texture2D texture = new Texture2D(gd, size, size);
        Color[] data = new Color[size * size];
        
        Vector2 center = new Vector2(size/2, size/2);
        
        for(int y=0; y<size; y++)
        {
            for(int x=0; x<size; x++)
            {
                int i = y*size+x;
                float dx = Math.Abs(x - center.X);
                float dy = Math.Abs(y - center.Y);
                
                // Baklava dilimi (Elmas) şekli
                if (dx + dy < size/2 - 2)
                {
                    // Kristalize etki: Rastgele parlamalar veya fasetler yerine bölgesel tonlama
                    Color baseColor = color;
                    
                    // Sol üst parlak, sağ alt koyu
                    if (x < center.X && y < center.Y) baseColor = Color.Lerp(color, Color.White, 0.5f);
                    else if (x > center.X && y > center.Y) baseColor = Color.Lerp(color, Color.Black, 0.3f);
                    
                    // Kenar çizgileri (Facet)
                    if (Math.Abs(dx - dy) < 1 || x == (int)center.X || y == (int)center.Y)
                        baseColor = Color.Lerp(baseColor, Color.White, 0.2f); // Hafif parlak kenarlar
                        
                    data[i] = baseColor;
                }
                else
                {
                    data[i] = Color.Transparent;
                }
            }
        }
        texture.SetData(data);
        return texture;
    }

    private static Texture2D CreateScrollIcon(GraphicsDevice gd, Color paperColor, Color runeColor)
    {
        // === TILSIM (PARŞÖMEN) ===
        int size = 32;
        Texture2D texture = new Texture2D(gd, size, size);
        Color[] data = new Color[size * size];
        
        for(int y=0; y<size; y++)
        {
            for(int x=0; x<size; x++)
            {
                int i = y*size+x;
                
                // Dikey dikdörtgen
                if (x >= 8 && x <= 24 && y >= 4 && y <= 28)
                {
                    data[i] = paperColor;
                    
                    // Kağıt Dokusu (Hafif gölgeli kenarlar)
                    if (x == 8 || x == 24 || y == 4 || y == 28) data[i] = Color.Lerp(paperColor, Color.Black, 0.2f);
                    
                    // Ortada Sembol (Rün)
                    if (x >= 14 && x <= 18 && y >= 10 && y <= 22)
                    {
                        data[i] = runeColor;
                    }
                    // Yatay çizgi (Mühür bağı gibi)
                    if (y == 16 && x > 10 && x < 22) data[i] = runeColor;
                }
                else
                {
                    data[i] = Color.Transparent;
                }
            }
        }
        texture.SetData(data);
        return texture;
    }

    private static Texture2D CreateAmuletIcon(GraphicsDevice gd, Color frameColor, Color gemColor)
    {
        // === MUSKA (KOLYE) ===
        int size = 32;
        Texture2D texture = new Texture2D(gd, size, size);
        Color[] data = new Color[size * size];
        
        Vector2 center = new Vector2(size/2, size/2);
        
        for(int y=0; y<size; y++)
        {
            for(int x=0; x<size; x++)
            {
                int i = y*size+x;
                float dist = Vector2.Distance(new Vector2(x, y), center);
                
                if (dist < 12)
                {
                    // Çerçeve
                    if (dist > 8)
                    {
                        data[i] = frameColor;
                        // Altın/Gümüş parlama efekti
                        if (x < center.X && y < center.Y) data[i] = Color.Lerp(frameColor, Color.White, 0.4f);
                    }
                    // İç Taş (Gem)
                    else
                    {
                        data[i] = gemColor;
                        // Taş Parlaması
                        if (dist < 4 && x < center.X - 2 && y < center.Y - 2) data[i] = Color.Lerp(gemColor, Color.White, 0.6f);
                    }
                }
                else
                {
                    // Zincir (Basit V şekli)
                    if (y < center.Y - 10 && Math.Abs(x - center.X) == Math.Abs(y - (center.Y - 12)) + 2)
                    {
                        data[i] = Color.Gray; 
                    }
                    else
                    {
                        data[i] = Color.Transparent;
                    }
                }
            }
        }
        texture.SetData(data);
        return texture;
    }
    
    public static Item GetItem(int id)
    {
        if (id < 0 || id >= _items.Length || _items[id] == null)
            return null;
        
        // Kopyasını döndür (her slot için ayrı instance)
        var original = _items[id];
        return new Item
        {
            Id = original.Id,
            Name = original.Name,
            Description = original.Description,
            Type = original.Type,
            Rarity = original.Rarity,
            RequiredLevel = original.RequiredLevel,
            EnhancementLevel = original.EnhancementLevel,
            MinDamage = original.MinDamage,
            MaxDamage = original.MaxDamage,
            AttackSpeed = original.AttackSpeed,
            Defense = original.Defense,
            Health = original.Health,
            Icon = original.Icon,
            IconColor = original.IconColor
        };
    }
    
    private static Texture2D CreateWeaponIcon(GraphicsDevice graphicsDevice, Color bladeColor)
    {
        int size = 40;
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] colors = new Color[size * size];
        
        // Kılıç şekli çiz
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                colors[i] = Color.Transparent;
                
                // Kılıç gövdesi (çapraz)
                int bladeWidth = 4;
                int diff = Math.Abs((size - 1 - y) - x);
                
                if (diff < bladeWidth && x > 5 && x < size - 8 && y > 5 && y < size - 8)
                {
                    // Blade gradient
                    float gradient = 1f - (float)diff / bladeWidth;
                    colors[i] = new Color(
                        (int)(bladeColor.R * gradient + 50),
                        (int)(bladeColor.G * gradient + 30),
                        (int)(bladeColor.B * gradient + 10)
                    );
                }
                
                // Kabza (sağ alt köşe)
                if (x >= size - 12 && x <= size - 5 && y >= size - 12 && y <= size - 5)
                {
                    // Kabza
                    colors[i] = new Color(80, 60, 40);
                }
                
                // Koruma (crossguard)
                if (y >= size / 2 - 2 && y <= size / 2 + 2 && x >= size / 2 - 6 && x <= size / 2 + 6)
                {
                    colors[i] = new Color(100, 80, 60);
                }
            }
        }
        
        texture.SetData(colors);
        return texture;
    }
    
    private static Texture2D CreateArmorIcon(GraphicsDevice graphicsDevice, Color armorColor)
    {
        int size = 40;
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] colors = new Color[size * size];
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                colors[i] = Color.Transparent;
                
                // Zırh gövdesi (göğüs)
                int centerX = size / 2;
                
                // Üst kısım (omuzlar)
                if (y >= 5 && y < 15)
                {
                    int width = 16 - (y - 5) / 2;
                    if (Math.Abs(x - centerX) < width)
                    {
                        float gradient = 1f - (float)Math.Abs(x - centerX) / width * 0.3f;
                        colors[i] = new Color(
                            (int)(armorColor.R * gradient),
                            (int)(armorColor.G * gradient),
                            (int)(armorColor.B * gradient)
                        );
                    }
                }
                
                // Alt kısım (gövde)
                if (y >= 15 && y < size - 5)
                {
                    int width = 10;
                    if (Math.Abs(x - centerX) < width)
                    {
                        float gradient = 1f - (float)(y - 15) / (size - 20) * 0.2f;
                        colors[i] = new Color(
                            (int)(armorColor.R * gradient),
                            (int)(armorColor.G * gradient),
                            (int)(armorColor.B * gradient)
                        );
                    }
                }
            }
        }
        
        texture.SetData(colors);
        return texture;
    }
}
