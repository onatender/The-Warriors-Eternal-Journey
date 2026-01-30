using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EternalJourney;

public enum ItemType
{
    None,
    Weapon,
    Armor,
    Material,
    Shield,  // Kalkan
    Helmet,   // Kask
    Consumable // Ikser vb.
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
    
    // Kalkan özellikleri
    public int BlockChance { get; set; } // 0-100 arası yüzde
    
    // Satış Fiyatı
    public int BuyPrice { get; set; } = 10; // Varsayılan 10 altın
    public int SellPrice => BuyPrice / 2; // Satış fiyatı = Alış / 2
    
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
            IconColor = this.IconColor,
            BlockChance = this.BlockChance
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
        else if (Type == ItemType.Armor || Type == ItemType.Helmet)
        {
            Defense = (int)(Defense * 1.2f) + 1;
            Health = (int)(Health * 1.2f) + 5;
        }
        else if (Type == ItemType.Shield)
        {
            Defense = (int)(Defense * 1.2f) + 1;
            BlockChance = Math.Min(BlockChance + 2, 75); // Max %75
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
        else if (Type == ItemType.Armor || Type == ItemType.Helmet)
        {
            Defense = (int)((Defense - 1) / 1.2f);
            Health = (int)((Health - 5) / 1.2f);
        }
        else if (Type == ItemType.Shield)
        {
            Defense = (int)((Defense - 1) / 1.2f);
            BlockChance = Math.Max(BlockChance - 2, 0);
        }
    }
}

// Item Database - Oyundaki tüm itemlar
public static class ItemDatabase
{
    private static Item[] _items;
    private static bool _initialized = false;
    
    public static void Initialize(GraphicsDevice graphicsDevice, Microsoft.Xna.Framework.Content.ContentManager content)
    {
        if (_initialized) return;
        
        _items = new Item[100]; // Max 100 farklı item
        
        // --- LOAD TEXTURES ---
        Texture2D armor1 = null, armor2 = null;
        Texture2D helmet1 = null, helmet2 = null;
        Texture2D shield1 = null, shield2 = null;
        Texture2D amulet = null;
        Texture2D charm = null;
        Texture2D sword1 = null, sword2 = null;
        Texture2D upgradeStone = null;
        
        try { armor1 = content.Load<Texture2D>("icons/chestplate_1"); } catch {}
        try { armor2 = content.Load<Texture2D>("icons/chestplate_2"); } catch {}
        try { helmet1 = content.Load<Texture2D>("icons/helmet_1"); } catch {}
        try { helmet2 = content.Load<Texture2D>("icons/helmet_2"); } catch {}
        try { shield1 = content.Load<Texture2D>("icons/shield_1"); } catch {}
        try { shield2 = content.Load<Texture2D>("icons/shield_2"); } catch {}
        try { amulet = content.Load<Texture2D>("icons/amulet"); } catch {}
        try { charm = content.Load<Texture2D>("icons/charm"); } catch {}
        try { sword1 = content.Load<Texture2D>("icons/sword_1"); } catch {}
        try { sword2 = content.Load<Texture2D>("icons/sword_2"); } catch {}
        try { upgradeStone = content.Load<Texture2D>("icons/upgrade_stone"); } catch {}
        
        // Fallbacks (if load fails)
        if (armor1 == null) armor1 = CreateArmorIcon(graphicsDevice, new Color(150, 120, 90));
        if (armor2 == null) armor2 = CreateArmorIcon(graphicsDevice, new Color(120, 120, 140));
        if (helmet1 == null) helmet1 = CreateHelmetIcon(graphicsDevice, new Color(120, 80, 50)); // helmet1 = BROWN (Leather)
        if (helmet2 == null) helmet2 = CreateHelmetIcon(graphicsDevice, new Color(180, 180, 190)); // helmet2 = GRAY (Iron)
        if (shield1 == null) shield1 = CreateShieldIcon(graphicsDevice, new Color(180, 180, 190));
        if (shield2 == null) shield2 = CreateShieldIcon(graphicsDevice, new Color(139, 90, 43));
        
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
            BuyPrice = 150, // Rebalance: 50 -> 150
            Icon = sword1 ?? CreateWeaponIcon(graphicsDevice, new Color(139, 90, 43)) // sword_1
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
            BuyPrice = 120, // Rebalance: 40 -> 120
            Icon = armor1 // chestplate_1
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
            BuyPrice = 400, // Rebalance: New Price
            Icon = armor1 // Also chestplate_1 (or use Color filtering in visual if needed, but icon is static)
        };
        // NOTE: If user wants different colors, we might need a shader or separate textures. 
        // Or we can Tint it in Draw() using IconColor property!
        _items[3].IconColor = new Color(200, 150, 100); // Slight tint difference
        
        // ID 99: Güçlendirme Taşı
        _items[99] = new Item
        {
            Id = 99,
            Name = "Guclendirme Tasi",
            Description = "Bu tas esyalarinin seviyesini yukseltmenize yarar.",
            Type = ItemType.Material,
            Rarity = ItemRarity.Rare,
            RequiredLevel = 1,
            BuyPrice = 250, // Rebalance: Enhancement Stone
            Icon = upgradeStone ?? CreateMaterialIcon(graphicsDevice, Color.Cyan)
        };
        
        // ID 10: Ebedi Kılıç (Legendary)
        _items[10] = new Item
        {
            Id = 10,
            Name = "Ebedi Kilic",
            Description = "Efsanelere konu olmus, sonsuz guc barindiran kilic.",
            Type = ItemType.Weapon,
            Rarity = ItemRarity.Legendary,
            RequiredLevel = 25, // Hileli olduğu için level 1
            EnhancementLevel = 0, 
            MinDamage = 100,
            MaxDamage = 150,
            AttackSpeed = 100, // Çok Hızlı
            Icon = CreateWeaponIcon(graphicsDevice, Color.Purple),
            BuyPrice = 1000000 // Rebalance: Legendary Price
        };

        // ID 11: Ejderha Zırhı (Legendary)
        _items[11] = new Item
        {
            Id = 11,
            Name = "Ejderha Zirhi",
            Description = "Ejderha pullarindan yapilmis, asilmaz bir zirh.",
            Type = ItemType.Armor,
            Rarity = ItemRarity.Legendary,
            RequiredLevel = 25,
            EnhancementLevel = 0,
            Defense = 50,
            Health = 200,
            BuyPrice = 1000000,
            Icon = armor2, // chestplate_2
            IconColor = Color.Red // Tint Red
        };

        // ID 25: Can İksiri
        Texture2D potionIcon = null;
        try { potionIcon = content.Load<Texture2D>("icons/health_spell"); }
        catch { potionIcon = CreatePotionIcon(graphicsDevice, Color.Red); }

        _items[25] = new Item
        {
            Id = 25,
            Name = "Can Iksiri",
            Description = "50 Can yeniler.",
            Type = ItemType.Consumable,
            Rarity = ItemRarity.Common,
            RequiredLevel = 1,
            Health = 50, // Yenilenen can miktarı
            BuyPrice = 500,
            Icon = potionIcon
        };

        
        // --- EFSUN & SUPPORT ITEMLARI ---
        
        // ID 20: Şans Tılsımı %10 (CHARM)
        _items[20] = new Item
        {
            Id = 20,
            Name = "Sans Tilsimi (+%10)",
            Description = "Yukseltme sansini %10 artirir.",
            Type = ItemType.Material,
            Rarity = ItemRarity.Common,
            BuyPrice = 300, // Rebalance: +10%
            Icon = charm ?? CreateScrollIcon(graphicsDevice, new Color(240, 230, 200), new Color(50, 150, 50)),
            IconColor = Color.LightGreen // TINT
        };
        
        // ID 21: Şans Tılsımı %25
        _items[21] = new Item
        {
            Id = 21,
            Name = "Sans Tilsimi (+%25)",
            Description = "Yukseltme sansini %25 artirir.",
            Type = ItemType.Material,
            Rarity = ItemRarity.Rare,
            BuyPrice = 750, // Rebalance: +25%
            Icon = charm ?? CreateScrollIcon(graphicsDevice, new Color(255, 250, 220), new Color(50, 50, 150)),
            IconColor = Color.LightBlue // TINT
        };
        
        // ID 22: Şans Tılsımı %50
        _items[22] = new Item
        {
            Id = 22,
            Name = "Sans Tilsimi (+%50)",
            Description = "Yukseltme sansini %50 artirir!",
            Type = ItemType.Material,
            Rarity = ItemRarity.Legendary,
            BuyPrice = 2500, // Rebalance: +50%
            Icon = charm ?? CreateScrollIcon(graphicsDevice, new Color(255, 215, 0), new Color(200, 0, 0)),
            IconColor = Color.Gold // TINT
        };
        
        // ID 30: Muska (AMULET)
        _items[30] = new Item
        {
            Id = 30,
            Name = "Muska",
            Description = "Basarisiz yukseltmede esyanin yok olmasini engeller, ancak seviyesi duser.",
            Type = ItemType.Material,
            Rarity = ItemRarity.Rare,
            BuyPrice = 1500, // Rebalance: Amulet
            Icon = amulet ?? CreateAmuletIcon(graphicsDevice, Color.Silver, Color.Gray),
            IconColor = Color.Silver // TINT
        };
        
        // ID 31: Büyülü Muska
        _items[31] = new Item
        {
            Id = 31,
            Name = "Buyulu Muska",
            Description = "Basarisiz yukseltmede esyayi tamamen korur.",
            Type = ItemType.Material,
            Rarity = ItemRarity.Legendary,
            BuyPrice = 5000, // Rebalance: Magic Amulet
            Icon = amulet ?? CreateAmuletIcon(graphicsDevice, Color.Gold, Color.Purple),
            IconColor = Color.Violet // TINT
        };

        // === KALKANLAR ===
        
        // ID 40: Tahta Kalkan
        _items[40] = new Item
        {
            Id = 40,
            Name = "Tahta Kalkan",
            Description = "Basit tahta kalkan. Bloklama sansi verir.",
            Type = ItemType.Shield,
            Rarity = ItemRarity.Common,
            RequiredLevel = 1,
            Defense = 5,
            BlockChance = 10,
            BuyPrice = 150, // Rebalance: 60 -> 150
            Icon = shield2 ?? CreateShieldIcon(graphicsDevice, new Color(139, 90, 43)) // shield_2 (Simpler)
        };
        
        // ID 41: Demir Kalkan
        _items[41] = new Item
        {
            Id = 41,
            Name = "Demir Kalkan",
            Description = "Saglam demir kalkan. Yuksek bloklama sansi.",
            Type = ItemType.Shield,
            Rarity = ItemRarity.Uncommon,
            RequiredLevel = 1,
            Defense = 12,
            BlockChance = 18,
            BuyPrice = 750, // Rebalance: 150 -> 750
            Icon = shield1 ?? CreateShieldIcon(graphicsDevice, new Color(180, 180, 190)) // shield_1 (Better)
        };
        
        // === KASKLAR ===
        
        // ID 50: Deri Kask
        _items[50] = new Item
        {
            Id = 50,
            Name = "Deri Kask",
            Description = "Hafif deri kask.",
            Type = ItemType.Helmet,
            Rarity = ItemRarity.Common,
            RequiredLevel = 1,
            Defense = 3,
            Health = 5,
            BuyPrice = 150,
            Icon = helmet1 // helmet_1 (Leather)
        };
        
        // ID 51: Demir Kask
        _items[51] = new Item
        {
            Id = 51,
            Name = "Demir Kask",
            Description = "Saglam demir kask.",
            Type = ItemType.Helmet,
            Rarity = ItemRarity.Common,
            RequiredLevel = 1,
            Defense = 8,
            Health = 15,
            BuyPrice = 500,
            Icon = helmet2 // helmet_2 (Iron)
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
            BlockChance = original.BlockChance, // Fix: Copy BlockChance
            BuyPrice = original.BuyPrice,       // Fix: Copy BuyPrice
            Icon = original.Icon,
            IconColor = original.IconColor
        };
    }
    
    private static Texture2D CreateWeaponIcon(GraphicsDevice graphicsDevice, Color bladeColor)
    {
        int size = 40;
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] colors = new Color[size * size];
        
        // Renkler
        Color silver = new Color(200, 200, 220);
        Color darkSilver = new Color(120, 120, 140);
        Color handleColor = new Color(90, 60, 40);
        Color guardColor = new Color(190, 170, 60);
        
        // Eğer bladeColor özel bir renkse (örn Efsanevi), onu kullan
        bool isSpecial = bladeColor != Color.Gray; 
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                colors[i] = Color.Transparent;
                
                // Dikey Kılıç (Ortada)
                int cx = size / 2;
                
                // Kabza (Sap)
                if (x >= cx - 1 && x <= cx + 1 && y >= size - 8 && y < size - 2)
                {
                    colors[i] = handleColor;
                    if (y == size - 3) colors[i] = guardColor; // Pommel
                }
                
                // Koruma (Guard)
                if (y >= size - 12 && y < size - 8)
                {
                    if (Math.Abs(x - cx) <= 5) colors[i] = guardColor;
                }
                
                // Bıçak (Blade)
                if (y < size - 12 && y > 2)
                {
                    if (Math.Abs(x - cx) <= 2)
                    {
                        colors[i] = isSpecial ? bladeColor : silver;
                        
                        // Ortadaki oluk veya parlaklık
                        if (x == cx) colors[i] = isSpecial ? Color.White : new Color(240, 240, 255);
                        
                        // Kenar gölgeleri
                        if (Math.Abs(x - cx) == 2) colors[i] = isSpecial ? new Color(bladeColor.R/2, bladeColor.G/2, bladeColor.B/2) : darkSilver;
                    }
                    
                    // Uç kısmı sivriltme
                    if (y < 6 && Math.Abs(x - cx) > (y-2)/2) colors[i] = Color.Transparent;
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
    
    private static Texture2D CreateShieldIcon(GraphicsDevice graphicsDevice, Color shieldColor)
    {
        int size = 40;
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] colors = new Color[size * size];
        
        int centerX = size / 2;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                colors[i] = Color.Transparent;
                
                // Kalkan şekli (Üstte geniş, altta sivri)
                int maxWidth = 16;
                int width = maxWidth - (y * y) / 80; // Parabolik daralma
                
                if (y >= 4 && y < size - 4 && width > 0 && Math.Abs(x - centerX) < width)
                {
                    float gradient = 1f - (float)Math.Abs(x - centerX) / width * 0.3f;
                    colors[i] = new Color(
                        (int)(shieldColor.R * gradient),
                        (int)(shieldColor.G * gradient),
                        (int)(shieldColor.B * gradient)
                    );
                    
                    // Orta şerit (dekorasyon)
                    if (Math.Abs(x - centerX) < 2)
                    {
                        colors[i] = Color.Lerp(shieldColor, Color.White, 0.3f);
                    }
                    
                    // Kenar çizgisi
                    if (Math.Abs(x - centerX) >= width - 1)
                    {
                        colors[i] = Color.Lerp(shieldColor, Color.Black, 0.3f);
                    }
                }
            }
        }
        
        texture.SetData(colors);
        return texture;
    }
    
    private static Texture2D CreateHelmetIcon(GraphicsDevice graphicsDevice, Color helmetColor)
    {
        int size = 40;
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] colors = new Color[size * size];
        
        int centerX = size / 2;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                colors[i] = Color.Transparent;
                
                // Kask şekli (Yarım küre)
                if (y >= 8 && y < 30)
                {
                    int radius = 14;
                    int headCenterY = 18;
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, headCenterY));
                    
                    if (dist < radius)
                    {
                        float gradient = 1f - dist / radius * 0.3f;
                        colors[i] = new Color(
                            (int)(helmetColor.R * gradient),
                            (int)(helmetColor.G * gradient),
                            (int)(helmetColor.B * gradient)
                        );
                        
                        // Yüz açıklığı (T şekli)
                        if (y > 15 && y < 26 && Math.Abs(x - centerX) < 4)
                        {
                            colors[i] = Color.Transparent;
                        }
                        if (y >= 14 && y < 17 && Math.Abs(x - centerX) < 8)
                        {
                            colors[i] = Color.Transparent;
                        }
                    }
                }
                
                // Tepe süsü
                if (y >= 4 && y < 10 && Math.Abs(x - centerX) < 3)
                {
                    colors[i] = Color.Lerp(helmetColor, Color.Gold, 0.3f);
                }
            }
        }
        
        texture.SetData(colors);
        return texture;
    }
    
    private static Texture2D CreatePotionIcon(GraphicsDevice gd, Color color)
    {
        int size = 32;
        Texture2D texture = new Texture2D(gd, size, size);
        Color[] data = new Color[size * size];

        float cx = size / 2f;
        float cy = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                
                // Flask Shape
                bool inNeck = Math.Abs(x - cx) < 4 && y < 10 && y > 2;
                bool inBody = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy + 4)) < 10;
                
                if (inNeck || inBody)
                {
                    if (y > 10) data[i] = color;
                    else data[i] = Color.Lerp(color, Color.White, 0.5f); 
                }
                else
                {
                    data[i] = Color.Transparent;
                }
                
                 if ((inNeck || inBody) && (x < cx - 9 || x > cx + 9 || y < 3 || y > 26))
                     data[i] = Color.White; // border
            }
        }
        texture.SetData(data);
        return texture;
    }
}
