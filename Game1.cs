using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace EternalJourney;

public enum GameState
{
    Login,
    Playing,
    Paused,
    Dead
}

public partial class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Player _player;
    private Inventory _inventory;
    private EnhancementUI _enhancementUI;
    private EnemyManager _enemyManager;
    private SpriteFont _gameFont;
    private Texture2D _pixelTexture;
    private Texture2D _backgroundTexture; // Yeni arka plan
    
    public static double TotalTime; // Statik zaman değişkeni
    
    // Hasar sayıları (floating damage numbers)
    private List<DamageNumber> _damageNumbers = new List<DamageNumber>();
    
    // Cursor
    // Cursor
    // private Texture2D _cursorTexture; // Varsayılan cursor (Kaldırıldı)
    // private Texture2D _magicCursorTexture; // Mod için (Kaldırıldı)
    
    // Tam ekran toggle için
    // Tam ekran toggle için
    // Tam ekran toggle için
    private KeyboardState _previousKeyState;
    private MouseState _previousMouseState;

    // SFX
    private SoundEffect _sfxSlice1;
    private SoundEffect _sfxSlice2;
    private SoundEffect _sfxClash;
    
    // Login ve Save
    private GameState _currentState = GameState.Login;
    private string _playerName = "";
    private KeyboardState _currentKeyState;

    // Harita Sistemi
    private int _currentMapIndex = 1;
    private const int MAX_MAPS = 5;
    
    // Ölüm Sistemi
    private bool _playerNeedsRespawn = false;
    private int _pendingDeathPenalty = 0;
    
    // Satıcı NPC Sistemi
    private ShopUI _shopUI;
    private Vector2 _vendorPosition;
    private Texture2D _vendorTexture;
    private bool _nearVendor = false;
    private const float VENDOR_INTERACTION_RANGE = 100f;
    
    // Kamera ve Harita
    private Camera2D _camera;
    private const int MAP_WIDTH = 2500;
    private const int MAP_HEIGHT = 1600;
    
    // Game Log
    private GameLog _gameLog;
    
    // UI Butonlar
    private Texture2D _bagButtonTexture;
    private Rectangle _bagButtonRect;
    private bool _isHoveringBag;
    
    // Title Screen
    private TitleScreen _titleScreen;
    private int _currentSlotIndex = 0;
    
    // Music Manager
    private MusicManager _musicManager;
    
    // Stats UI
    private StatsUI _statsUI;
    private bool _showStats = false;
    
    // Skill UI
    private SkillBarUI _skillBarUI;
    
    // SFX
    private SoundEffect _sfxCoinPickup;
    private SoundEffect _sfxCoinBuy;
    private SoundEffect _sfxCoinSell;
    private SoundEffect _sfxCoinDrop;
    private SoundEffect _sfxUpgradeSuccess;
    
    // Volume Settings
    private float _sfxVolume = 0.5f;
    private bool _isDraggingMusic;
    private bool _isDraggingSFX;
    
    // Mobile UI (Removed Joystick)
    // private VirtualJoystick _joystick;
    private Rectangle _statsButtonRect;
    private bool _isHoveringStats;
    private Texture2D _statsIconTexture;
    private Texture2D _goldIconTexture;
    
    // Revive UI
    private ReviveUI _reviveUI;
    
    // Pause Menu State
    private bool _showSettingsInPause = false;
    private float _saveConfirmationTimer = 0f;
    private double _lastClashSoundTime = 0; // Cooldown for hit sound
    


    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;

        // Tam ekran ayarları
        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        _graphics.IsFullScreen = true;
        _graphics.HardwareModeSwitch = false;
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        // Karakter girişi için klavye olayını dinle (TitleScreen kullanıyoruz artık)
        // Window.TextInput += OnTextInput;
        
        // Çıkış yaparken kaydet
        Exiting += OnGameExiting;
    }
    
    private void OnTitleScreenStart(int slotIndex, string newName)
    {
        _currentState = GameState.Playing;
        _currentSlotIndex = slotIndex;
        
        if (newName != null)
        {
            _playerName = newName;
            
            // Başlangıç itemlarını envantere ekle
            // Başlangıç itemlarını envantere ekle ve kuşan (Böylece skiller çalışır)
            Item startWeapon = ItemDatabase.GetItem(1); // Tahta Kılıç
            if (startWeapon != null)
            {
                _player.EquipWeapon(startWeapon);
                _inventory.WeaponSlot.Item = startWeapon;
                _inventory.WeaponSlot.Quantity = 1;
            }
            
            // 3 Adet Can İksiri
            _inventory.AddItem(25, 3);
            
            // Deri Zırh kaldırıldı

            
            // Başlangıç pozisyonu
             _player.SetPosition(new Vector2(MAP_WIDTH / 2f - 24, MAP_HEIGHT / 2f - 32));
             _player.CurrentHealth = _player.MaxHealth;
             _player.Experience = 0;
             _player.Level = 1;
             _player.Gold = 0;
             
             LoadMap(1);
         }
        else
        {
            // --- KAYITLI OYUN YÜKLE ---
            SaveData data = SaveManager.LoadGame(slotIndex);
            if (data != null)
            {
                 _playerName = data.PlayerName;
                 _player.CurrentHealth = data.CurrentHealth;
                 _player.MaxHealth = data.MaxHealth;
                 _player.Level = data.Level;
                 _player.Experience = data.Experience; // Direkt set et, GainExperience çağırma (yoksa ekliyor ve level atlıyor)
                 //_player.GainExperience((int)data.Experience); 
                 _player.Gold = data.Gold; // Direkt set et, GainGold çağırma (ekleme yapıyor)
                 
                 // 1. Önce Map'i yükle
                 _currentMapIndex = data.MapIndex;
                 LoadMap(_currentMapIndex);
                 
                 // 2. Map yüklendikten sonra pozisyonu set et (LoadMap bazen sıfırlayabilir diye garantiye alalım)
                 _player.SetPosition(new Vector2(data.PositionX, data.PositionY));
                 
                 // 3. Kamerayı karaktere odakla
                 _camera.Position = _player.Position;
                 
                 // 4. Diğer verileri yükle
                 _inventory.LoadItems(data.InventoryItems);
                 
                 // Eşyaları kuşan 
                 if (data.EquippedWeapon != null && data.EquippedWeapon.ItemId > 0) 
                 {
                     Item weapon = ItemDatabase.GetItem(data.EquippedWeapon.ItemId);
                     if (weapon != null)
                     {
                         for(int i=0; i<data.EquippedWeapon.EnhancementLevel; i++) weapon.UpgradeSuccess();
                         _player.EquipWeapon(weapon);
                         _inventory.WeaponSlot.Item = weapon;
                         _inventory.WeaponSlot.Quantity = 1;
                     }
                 }
                 
                 if (data.EquippedArmor != null && data.EquippedArmor.ItemId > 0) 
                 {
                     Item armor = ItemDatabase.GetItem(data.EquippedArmor.ItemId);
                     if (armor != null)
                     {
                         for(int i=0; i<data.EquippedArmor.EnhancementLevel; i++) armor.UpgradeSuccess();
                         _player.EquipArmor(armor);
                         _inventory.ArmorSlot.Item = armor;
                         _inventory.ArmorSlot.Quantity = 1;
                     }
                 }
                 
                 if (data.EquippedShield != null && data.EquippedShield.ItemId > 0) 
                 {
                     Item shield = ItemDatabase.GetItem(data.EquippedShield.ItemId);
                     if (shield != null)
                     {
                         for(int i=0; i<data.EquippedShield.EnhancementLevel; i++) shield.UpgradeSuccess();
                         _player.EquipShield(shield);
                         _inventory.ShieldSlot.Item = shield;
                         _inventory.ShieldSlot.Quantity = 1;
                     }
                 }
                 
                 if (data.EquippedHelmet != null && data.EquippedHelmet.ItemId > 0) 
                 {
                     Item helmet = ItemDatabase.GetItem(data.EquippedHelmet.ItemId);
                     if (helmet != null)
                     {
                         for(int i=0; i<data.EquippedHelmet.EnhancementLevel; i++) helmet.UpgradeSuccess();
                         _player.EquipHelmet(helmet);
                         _inventory.HelmetSlot.Item = helmet;
                         _inventory.HelmetSlot.Quantity = 1;
                     }
                 }
             }
         }

    }
    
    private void SaveCurrentGame()
    {
        if (_player == null) return;

        // Ekipmanları hazırla
        SavedItem savedWeapon = null;
        var w = _player.GetEquippedWeapon();
        if (w != null)
            savedWeapon = new SavedItem { ItemId = w.Id, Quantity = 1, EnhancementLevel = w.EnhancementLevel };
            
        SavedItem savedArmor = null;
        var a = _player.GetEquippedArmor();
        if (a != null)
            savedArmor = new SavedItem { ItemId = a.Id, Quantity = 1, EnhancementLevel = a.EnhancementLevel };

        SavedItem savedShield = null;
        var sh = _player.GetEquippedShield();
        if (sh != null)
            savedShield = new SavedItem { ItemId = sh.Id, Quantity = 1, EnhancementLevel = sh.EnhancementLevel };

        SavedItem savedHelmet = null;
        var h = _player.GetEquippedHelmet();
        if (h != null)
            savedHelmet = new SavedItem { ItemId = h.Id, Quantity = 1, EnhancementLevel = h.EnhancementLevel };

        // Kaydet
        SaveData data = new SaveData
        {
            PlayerName = _playerName,
            PositionX = _player.Position.X,
            PositionY = _player.Position.Y,
            Level = _player.Level,
            Experience = _player.Experience,
            CurrentHealth = _player.CurrentHealth,
            MaxHealth = _player.MaxHealth,
            Gold = _player.Gold, 
            MapIndex = _currentMapIndex,
            InventoryItems = _inventory.GetItemsForSave(),
            EquippedWeapon = savedWeapon,
            EquippedArmor = savedArmor,
            EquippedShield = savedShield,
            EquippedHelmet = savedHelmet
        };
        
        SaveManager.SaveGame(data, _currentSlotIndex);
    }

    private void OnGameExiting(object sender, EventArgs args)
    {
        if (_currentState == GameState.Playing || _currentState == GameState.Paused)
        {
            SaveCurrentGame();
        }
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        // Pixel texture for UI elements
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        // Font yükle
        _gameFont = Content.Load<SpriteFont>("GameFont");

        // Oyuncu oluştur
        Vector2 startPosition = new Vector2(
            _graphics.PreferredBackBufferWidth / 2f - 24,
            _graphics.PreferredBackBufferHeight / 2f - 32
        );

        _player = new Player(GraphicsDevice, startPosition);
        
        // Oyuncu saldırı event'i
        _player.OnAttackHit += (enemy, damage) =>
        {
            // Oynat Clash Sesi (Cooldown ekle: 0.1 sn)
            if (TotalTime - _lastClashSoundTime > 0.1)
            {
                _sfxClash?.Play(0.3f, 0.0f, 0.0f);
                _lastClashSoundTime = TotalTime;
            }

            // Hasar sayısı ekle
            _damageNumbers.Add(new DamageNumber(
                enemy.Position + new Vector2(0, -30),
                damage,
                Color.Yellow
            ));
            
            // Eğer öldüyse XP ve Altın kazan
            if (enemy.IsDead)
            {
                // XP
                int xpAmount = enemy.Type == EnemyType.Demon ? 50 : 10;
                _player.GainExperience(xpAmount);
                
                // Altın (Economy Rebalance)
                int goldAmount = 0;
                
                switch (enemy.Type)
                {
                    case EnemyType.Goblin:
                        goldAmount = new Random().Next(2, 7); // 2-6 Gold
                        break;
                    case EnemyType.Spider:
                        goldAmount = new Random().Next(8, 16); // 8-15 Gold
                        break;
                    case EnemyType.Demon:
                        goldAmount = new Random().Next(25, 51); // 25-50 Gold
                        break;
                    default:
                        goldAmount = 1;
                        break;
                }
                
                _player.GainGold(goldAmount);
                
                // XP Popup yerine Log
                // _damageNumbers.Add(...) -> iptal
                _gameLog.AddMessage($"+{xpAmount} XP", Color.Cyan);
                
                // Altın Popup yerine Log
                _gameLog.AddMessage($"+{goldAmount} Altın", Color.Gold);
                
                // === LOOT DROP ===
                var drops = LootManager.GetLoot(enemy.Type);
                foreach(var drop in drops)
                {
                    bool added = _inventory.AddItem(drop.ItemId, drop.Quantity);
                    if(added)
                    {
                        Item itemDrop = ItemDatabase.GetItem(drop.ItemId);
                        
                        // Drop bildirimi
                        _gameLog.AddMessage($"Kazanıldı: {itemDrop.Name}", itemDrop.GetRarityColor());
                    }
                }
            }
        };
        
        // Item veritabanını başlat
        ItemDatabase.Initialize(GraphicsDevice, Content);
        LootManager.Initialize(); // Loot tablolarını yükle
        
        // Envanter oluştur
        _inventory = new Inventory(GraphicsDevice, Content,
            GraphicsDevice.Viewport.Width, 
            GraphicsDevice.Viewport.Height);
        _inventory.SetPlayer(_player);
            
        // Kamera Oluştur
        _camera = new Camera2D(GraphicsDevice.Viewport, MAP_WIDTH, MAP_HEIGHT);
        _shopUI = new ShopUI(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        
        // Game Log Oluştur
        _gameLog = new GameLog(GraphicsDevice.Viewport.Height);
            
        // Arka Plan Texture Oluştur
        _backgroundTexture = CreateDungeonFloor(1);
        
        // Başlangıç Eşyaları - ARTIK BURADA EKLENMIYOR (OnTitleScreenStart'a taşındı)
        // _inventory.AddItem(1); 
        // _inventory.AddItem(25); 
        // _inventory.AddItem(25); 
        // _inventory.AddItem(25); 

        
        // Envanter event'lerini bağla
        _inventory.OnWeaponEquipped += (weapon) => _player.EquipWeapon(weapon);
        _inventory.OnArmorEquipped += (armor) => _player.EquipArmor(armor);
        _inventory.OnShieldEquipped += (shield) => _player.EquipShield(shield);
        _inventory.OnHelmetEquipped += (helmet) => _player.EquipHelmet(helmet);
        _inventory.OnEnhancementTargetSelected += (item) => 
        {
            _enhancementUI.Open(item, _player, _inventory);
        };
        
        // Enhancement UI
        _enhancementUI = new EnhancementUI(GraphicsDevice, 
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight);
        
        // Shop UI ve Satıcı NPC
        _shopUI = new ShopUI(GraphicsDevice,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight);
        _vendorPosition = new Vector2(200, 300); // Map 1'de sabit pozisyon
        _vendorPosition = new Vector2(200, 300); // Map 1'de sabit pozisyon
        try {
            // Raw PNG Load
            // Absolute path or relative to output
            using(var stream = new System.IO.FileStream("Content/icons/shop.png", System.IO.FileMode.Open))
            {
                _vendorTexture = Texture2D.FromStream(GraphicsDevice, stream);
            }
        } catch(System.Exception ex) {
            // Backup color
            _vendorTexture = new Texture2D(GraphicsDevice, 48, 64);
            Color[] backup = new Color[48 * 64];
            for(int i = 0; i < backup.Length; i++) backup[i] = Color.Purple;
            _vendorTexture.SetData(backup);
            
            _gameLog.AddMessage($"Shop Icon Load Error: {ex.Message}", Color.Red);
        }
        
        // Enemy Manager oluştur
        _enemyManager = new EnemyManager(GraphicsDevice);
        
        // Cursor Textures (Basitçe pixelden oluşturuyoruz veya yüklüyoruz)
        // Cursor için basit bir logic ekleyeceğiz, texture yerine.

        // SFX Yukle
        try 
        {
            _sfxSlice1 = Content.Load<SoundEffect>("SFX/sword_slice_1");
            _sfxSlice2 = Content.Load<SoundEffect>("SFX/sword_slice_2");
            _sfxClash = Content.Load<SoundEffect>("SFX/sword_clash");
            var sfxWalk = Content.Load<SoundEffect>("SFX/walk");
            
            _player.SetCombatSounds(_sfxSlice1, _sfxSlice2);
            _player.SetWalkSound(sfxWalk);
        }
        catch(System.Exception e) 
        {
            System.Diagnostics.Debug.WriteLine("SFX Load Error: " + e.Message);
        }
        
        // Düşman saldırı event'i - oyuncu hasar alınca
        _enemyManager.OnPlayerDamaged += (damage) =>
        {
            // BLOKLAMA KONTROLÜ (Rastgele, Kalkan varsa)
            if (_player.TryBlock())
            {
                // Bloklandı! Hasar yok
                _damageNumbers.Add(new DamageNumber(
                    _player.Center + new Vector2(0, -40),
                    0,
                    new Color(100, 150, 255) // Mavi
                ) { CustomText = "BLOKLANDI!" });
                return; // Hasar işlemini atla
            }
            
            // SAVUNMA İLE HASAR AZALTMA (Yuzdesel)
            int totalDefense = _player.GetTotalDefense();
            float reduction = Math.Min(totalDefense, 90) / 100.0f; // Max %90 reduction
            int actualDamage = Math.Max(1, (int)(damage * (1.0f - reduction)));
            
            _player.TakeDamage(actualDamage);
            
            // Kırmızı hasar sayısı
            _damageNumbers.Add(new DamageNumber(
                _player.Center + new Vector2(0, -40),
                actualDamage,
                new Color(255, 80, 80) // Kırmızı
            ));
            
            // ÖLÜM KONTROLÜ
            if (_player.CurrentHealth <= 0)
            {
                // Can't die if already dead
                if (_currentState == GameState.Dead) return;
                
                _currentState = GameState.Dead;
                _reviveUI.Open(_player);
            }
        };
        
        // İlk Haritayı Yükle
        // LoadMap(1);
        
        // Çanta Butonu Oluştur (Sağ Alt)
        int bagSize = 64;
        _bagButtonRect = new Rectangle(
            GraphicsDevice.Viewport.Width - bagSize - 20,
            GraphicsDevice.Viewport.Height - bagSize - 20,
            bagSize, bagSize
        );
        
        // UI Textures
        try { 
            _bagButtonTexture = Content.Load<Texture2D>("icons/inventory"); 
            _statsIconTexture = Content.Load<Texture2D>("icons/stats");
            _goldIconTexture = Content.Load<Texture2D>("icons/gold");
        } 
        catch 
        {
            // Fallbacks if not found
             _bagButtonTexture = CreateBagIcon(GraphicsDevice, 64);
             _statsIconTexture = CreateStatsIcon(GraphicsDevice, 64);
             _goldIconTexture = CreateBagIcon(GraphicsDevice, 32); 
        }
        
        // Title Screen
        _titleScreen = new TitleScreen(GraphicsDevice, 
            _graphics.PreferredBackBufferWidth, 
            _graphics.PreferredBackBufferHeight);
            
        _titleScreen.OnGameStart += OnTitleScreenStart;
        _titleScreen.OnExitRequested += Exit;
        
        // Music Manager
        _musicManager = new MusicManager();
        _musicManager.LoadContent(Content);
        _musicManager.PlayMusicForMap(0); // Title Music
        
        // Skill System Init
        _player.InitializeSkills(GraphicsDevice);
        _skillBarUI = new SkillBarUI(GraphicsDevice, _player.SkillManager, _inventory,
            _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            
        _statsUI = new StatsUI(GraphicsDevice, 
            _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            
        // Mobile UI Init
        _statsButtonRect = new Rectangle(_graphics.PreferredBackBufferWidth - 100, _graphics.PreferredBackBufferHeight - 180, 64, 64);

        // Load SFX
        _sfxCoinPickup = Content.Load<SoundEffect>("SFX/sfx_coin_pickup");
        _sfxCoinBuy = Content.Load<SoundEffect>("SFX/sfx_coin_buy");
        _sfxCoinSell = Content.Load<SoundEffect>("SFX/sfx_coin_sell");
        _sfxCoinDrop = Content.Load<SoundEffect>("SFX/sfx_coin_drop");
        
        // Item SFX
        SoundEffect sfxItemPickup = null;
        SoundEffect sfxItemEquip = null;
        SoundEffect sfxPotionDrink = null;
        try {
            sfxItemPickup = Content.Load<SoundEffect>("SFX/item_pickup");
            sfxItemEquip = Content.Load<SoundEffect>("SFX/item_equip");
            sfxPotionDrink = Content.Load<SoundEffect>("SFX/drink_potion");
            _player.SetLevelUpSound(Content.Load<SoundEffect>("SFX/level_up"));
        } catch { System.Diagnostics.Debug.WriteLine("Item SFX not found"); }
        
        _inventory.SetCoinSounds(_sfxCoinPickup, _sfxCoinBuy, _sfxCoinSell, _sfxCoinDrop);
        _inventory.SetItemSounds(sfxItemPickup, sfxItemEquip);
        _inventory.SetPotionSound(sfxPotionDrink);
        _shopUI.SetCoinSounds(_sfxCoinBuy, _sfxCoinSell);
        
        try {
            _sfxUpgradeSuccess = Content.Load<SoundEffect>("SFX/upgrade_succesful");
            _enhancementUI.SetSFX(_sfxUpgradeSuccess);
        } catch { }
        
        // Enemy SFX
        Enemy.SfxDemonIdle = Content.Load<SoundEffect>("SFX/devil_sound");
        Enemy.SfxGoblinIdle = Content.Load<SoundEffect>("SFX/goblin_idle");
        Enemy.SfxGoblinDeath = Content.Load<SoundEffect>("SFX/goblin_death");
        
        // Load Skeleton Assets (Raw PNGs)
        Enemy.LoadSkeletonContent(GraphicsDevice);
        
        // Player SFX
        Player.SfxCoinPickup = _sfxCoinPickup;
        
        // Revive UI
        _reviveUI = new ReviveUI(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        
        _reviveUI.OnReviveClicked += () => 
        {
            // Remove Gold
            int cost = _reviveUI.GetCost();
            _player.LoseGold(cost);
            
            // Full Heal
            _player.CurrentHealth = _player.MaxHealth;
            
            // Continue Playing
            _currentState = GameState.Playing;
            
            // Effect
            _damageNumbers.Add(new DamageNumber(
                _player.Center + new Vector2(0, -60),
                0,
                Color.Green
            ) { CustomText = $"-{cost} Altin" });
        };
        
        _reviveUI.OnTownClicked += () =>
        {
            // Remove Gold (Town Penalty)
            int cost = _reviveUI.GetTownCost();
            _player.LoseGold(cost);
            
            // Full Heal
            _player.CurrentHealth = _player.MaxHealth;
            
            // Teleport to Town
            LoadMap(1);
            _player.SetPosition(new Vector2(MAP_WIDTH / 2f - 24, MAP_HEIGHT / 2f - 32));
            
            _currentState = GameState.Playing;
            
             // Effect
            _damageNumbers.Add(new DamageNumber(
                _player.Center + new Vector2(0, -60),
                0,
                Color.Green
            ) { CustomText = $"-{cost} Altin" });
        };

        LoadMap(1);
    } // End LoadContent    

    private Texture2D CreateBagIcon(GraphicsDevice graphicsDevice, int size)
    {
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] colors = new Color[size * size];
        
        Color bagColor = new Color(100, 60, 30);
        Color strapColor = new Color(140, 100, 60);
        Color buckleColor = Color.Gold;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                colors[i] = Color.Transparent;
                
                // Çanta Gövdesi (Yuvarlakça)
                float cx = size / 2f;
                float cy = size / 2f + 5;
                float dx = x - cx;
                float dy = y - cy;
                
                if ((dx*dx)/500 + (dy*dy)/400 < 1.0f)
                {
                     colors[i] = bagColor;
                     
                     // Kapak
                     if (y < size/2 && Math.Abs(x - cx) < size/2.5f) colors[i] = strapColor;
                     
                     // Toka
                     if (Math.Abs(x - cx) < 4 && Math.Abs(y - size/2) < 4) colors[i] = buckleColor;
                }
            }
        }
        texture.SetData(colors);
        return texture;
    }
    
    private void LoadMap(int mapIndex)
    {
        _currentMapIndex = mapIndex;
        _enemyManager.ClearGroups();
        _damageNumbers.Clear();
        
        // Müzik Değiştir - Eğer Login ekranındaysak müziği değiştirme (bg_main çalmaya devam etsin)
        if (_currentState != GameState.Login)
        {
            _musicManager.PlayMusicForMap(mapIndex);
        }
        
        // Arka Plan Texture'ını map'e göre oluştur
        _backgroundTexture = CreateDungeonFloor(mapIndex);
        
        // Harita boyutlarını kameraya bildir (Gerekirse map'e göre değişebilir)
        _camera.SetMapSize(MAP_WIDTH, MAP_HEIGHT);
        
        int safeMargin = 100;
        Random rnd = new Random();
        
        if (mapIndex == 1)
        {
            // --- MAP 1: GÜVENLİ BÖLGE ---
            // Satıcı Tam Ortada
            // Satıcı (Spawn noktasından uzakta)
            _vendorPosition = new Vector2(250, 250); // Sol üst tarafa yakın
        }
        else if (mapIndex == 2)
        {
            // --- MAP 2: GOBLIN LANDS --- (Kolay, Çok sayıda)
            for (int i = 0; i < 8; i++) // 8 Grup
            {
                int x = rnd.Next(safeMargin, MAP_WIDTH - safeMargin);
                int y = rnd.Next(safeMargin, MAP_HEIGHT - safeMargin);
                _enemyManager.SpawnGroup(new Vector2(x, y), EnemyType.Goblin, 3);
            }
        }
        else if (mapIndex == 3)
        {
            // --- MAP 3: SPIDER CAVE --- (Orta)
            for (int i = 0; i < 6; i++) // 6 Grup
            {
                int x = rnd.Next(safeMargin, MAP_WIDTH - safeMargin);
                int y = rnd.Next(safeMargin, MAP_HEIGHT - safeMargin);
                _enemyManager.SpawnGroup(new Vector2(x, y), EnemyType.Spider, 4);
            }
        }
        else if (mapIndex == 4)
        {
            // --- MAP 4: SKELETON DUNGEON --- (Zorlu)
            for (int i = 0; i < 7; i++)
            {
                int x = rnd.Next(safeMargin, MAP_WIDTH - safeMargin);
                int y = rnd.Next(safeMargin, MAP_HEIGHT - safeMargin);
                _enemyManager.SpawnGroup(new Vector2(x, y), EnemyType.Skeleton, 3);
            }
        }
        else if (mapIndex == 5)
        {
            // --- MAP 5: DEMON HALL --- (Final)
            for (int i = 0; i < 5; i++)
            {
                int x = rnd.Next(safeMargin, MAP_WIDTH - safeMargin);
                int y = rnd.Next(safeMargin, MAP_HEIGHT - safeMargin);
                _enemyManager.SpawnGroup(new Vector2(x, y), EnemyType.Demon, 2);
            }
            
            // FİNAL BOSS - Ortada tek başına
            _enemyManager.SpawnGroup(new Vector2(MAP_WIDTH/2, MAP_HEIGHT/2), EnemyType.Demon, 1);
        }
    }
        


    protected override void Update(GameTime gameTime)
    {
        TotalTime = gameTime.TotalGameTime.TotalSeconds;
        
        KeyboardState currentKeyState = Keyboard.GetState();
        MouseState currentMouseState = Mouse.GetState();
        _currentKeyState = currentKeyState;
        
        // F11 ve Alt+Enter her zaman çalışsın
        bool f11Pressed = currentKeyState.IsKeyDown(Keys.F11) && !_previousKeyState.IsKeyDown(Keys.F11);
        bool altEnterPressed = (currentKeyState.IsKeyDown(Keys.LeftAlt) || currentKeyState.IsKeyDown(Keys.RightAlt)) 
                               && currentKeyState.IsKeyDown(Keys.Enter) && !_previousKeyState.IsKeyDown(Keys.Enter);
        
        if (f11Pressed || altEnterPressed)
        {
            ToggleFullScreen();
            _inventory.UpdateScreenSize(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _skillBarUI.UpdateScreenSize(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        }
        
        if (_currentState == GameState.Login)
        {
            // Müzik yöneticisini güncelle (FadeIn/FadeOut çalışması için)
            _musicManager.Update(gameTime);
            
            IsMouseVisible = true;
            _titleScreen.Update(gameTime, currentMouseState, currentKeyState);
            
            _previousKeyState = currentKeyState;
            base.Update(gameTime);
            base.Update(gameTime);
            return;
        }

        if (_currentState == GameState.Paused)
        {
            IsMouseVisible = true;
            _player.StopMoving();
            
            // Mouse logic handled in Draw/UI section or here?
            // Actually, for simple buttons, handling clicks in Draw is easiest for Immediate Mode GUI
            // But we should ideally separate Update/Draw.
            // For now, let's just keep the state updates here minimal and rely on Draw's immediate mode logic 
            // OR move button logic here. 
            // Since I implemented the click logic in Draw(), I don't need update logic here for buttons.
            
            // Just Escape key to Unpause is convenient to keep? 
            // User requested "butonlu yapıya geçir ... klavye tuşları değil".
            // So removing keyboard unpause.
            
            if (currentKeyState.IsKeyDown(Keys.Escape) && !_previousKeyState.IsKeyDown(Keys.Escape))
            {
                 // Optional: Keep ESC to toggle back to game as a standard usage?
                 // User said "ESC menüsünü butonlu yapıya geçirmeni istiyorum klavye tuşları değil."
                 // This implies interaction with the menu items. 
                 // Toggling the menu ON/OFF with ESC is standard. 
                 _currentState = GameState.Playing;
                 _currentState = GameState.Playing;
                 IsMouseVisible = !_graphics.IsFullScreen;
            }
            
            if (_saveConfirmationTimer > 0)
                _saveConfirmationTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            _previousKeyState = currentKeyState;
            base.Update(gameTime);
            return;
        }

        if (_currentState == GameState.Dead)
        {
            IsMouseVisible = true;
            _player.StopMoving();
            _reviveUI.Update(gameTime, currentMouseState);
            
            _previousKeyState = currentKeyState;
            base.Update(gameTime);
            return;
        }

        // --- GAME PLAYING ---
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Müzik Yöneticisini Güncelle
        _musicManager.Update(gameTime);
        
        // Skill Updates
        _player.UpdateSkills(gameTime, _enemyManager);

        // Ölüm gerçekleştiyse güvenli yerde respawn yap
        if (_playerNeedsRespawn)
        {
            _playerNeedsRespawn = false;
            HandlePlayerDeath();
            return; // Bu frame'i atla
        }

        // Enhancement UI açıkken alt tarafı update etme
        if (_enhancementUI.IsOpen)
        {
            _enhancementUI.Update(gameTime);
            base.Update(gameTime);
            return;
        }
        
        // Shop UI açıkken alt tarafı update etme
        if (_shopUI.IsOpen)
        {
            IsMouseVisible = true; // Mouse görünür olsun
            _shopUI.Update(gameTime, currentKeyState, currentMouseState);
            _previousKeyState = currentKeyState;
            base.Update(gameTime);
            return;
        }
        
        // Satıcı NPC Yakınlık Kontrolü (Sadece Map 1'de)
        _nearVendor = false;
        if (_currentMapIndex == 1)
        {
            float distToVendor = Vector2.Distance(_player.Center, _vendorPosition + new Vector2(24, 32));
            _nearVendor = distToVendor < VENDOR_INTERACTION_RANGE;
            
            // F tuşu ile satıcıyı aç
            if (_nearVendor && currentKeyState.IsKeyDown(Keys.F) && !_previousKeyState.IsKeyDown(Keys.F))
            {
                _player.StopMoving();
                _shopUI.Open(_player, _inventory);
                return; // Prevent falling through to Player.Update which restarts movement/sound
            }
        }

        if (currentKeyState.IsKeyDown(Keys.Escape) && !_previousKeyState.IsKeyDown(Keys.Escape))
        {
            if (!_inventory.IsOpen && !_shopUI.IsOpen && !_enhancementUI.IsOpen)
            {
                 _currentState = GameState.Paused;
                 _showSettingsInPause = false; // Reset settings view
                 _player.StopMoving();
                 IsMouseVisible = true;
            }
            else
            {
                if (_inventory.IsOpen) _inventory.Close();
                if (_shopUI.IsOpen) _shopUI.Close();
                if (_enhancementUI.IsOpen) _enhancementUI.Close();
            }
        }
        
        // Stats UI Toggle (C Key)
        if (currentKeyState.IsKeyDown(Keys.C) && !_previousKeyState.IsKeyDown(Keys.C))
        {
            if (!_inventory.IsOpen && !_shopUI.IsOpen && !_enhancementUI.IsOpen)
            {
                _showStats = !_showStats;
            }
            else
            {
                 // Close others if opening stats? Or prevent opening?
                 // Let's close others just in case, or just simple toggle.
                 _showStats = !_showStats;
                 if (_showStats) 
                 {
                     _inventory.Close();
                     _shopUI.Close();
                     _enhancementUI.Close();
                 }
            }
        }
        
        // Envanteri güncelle
        _inventory.Update(gameTime, currentKeyState, currentMouseState);
        
        if (_inventory.IsOpen)
        {
            IsMouseVisible = true;
        }
        else
        {
           IsMouseVisible = !_graphics.IsFullScreen;
            
        // Kamerayı güncelle
        _camera.Update(_player.Center);
        
        // --- MOBILE UI UPDATE ---
        // _joystick.Update(currentMouseState, _previousMouseState);
        _isHoveringStats = _statsButtonRect.Contains(currentMouseState.Position);
        if (_isHoveringStats && currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            _showStats = !_showStats;
            if (_showStats)
            {
                _inventory.Close();
                _shopUI.Close();
                _enhancementUI.Close();
            }
        }
        
        // Player update
        Vector2 prevPos = _player.Position; // Collision için
        _player.Update(gameTime, MAP_WIDTH, MAP_HEIGHT, _enemyManager, Vector2.Zero);
        
        // Vendor Collision (Map 1)
        if (_currentMapIndex == 1)
        {
            Rectangle vendorRect = new Rectangle((int)_vendorPosition.X, (int)_vendorPosition.Y, 48, 64);
            // Basit bir bounding box daraltma (ayaklara odaklanmak için)
            Rectangle playerFeet = new Rectangle((int)_player.Position.X + 10, (int)_player.Position.Y + 40, 28, 20);
            Rectangle vendorFeet = new Rectangle(vendorRect.X + 5, vendorRect.Y + 40, 38, 20);
            
            if (playerFeet.Intersects(vendorFeet))
            {
                _player.SetPosition(prevPos);
                // _player.StopMoving(); // Opsiyonel: Kaymayı engellemek için
            }
        }
        _player.UpdateCombat(gameTime, _enemyManager);
        _enemyManager.Update(gameTime, _player.Center, _player.Bounds);
        
        // --- HARİTA GEÇİŞ KONTROLÜ ---
        // Yukarıdan çıkış -> Sonraki Map
        if (_player.Position.Y < -20)
        {
            if (_currentMapIndex < MAX_MAPS)
            {
                LoadMap(_currentMapIndex + 1);
                // Yeni mapte en aşağıdan başlat
                _player.SetPosition(new Vector2(_player.Position.X, MAP_HEIGHT - 80));
            }
            else
            {
                // Son maptesin, gidemezsin
                _player.SetPosition(new Vector2(_player.Position.X, 0));
            }
        }
        // Aşağıdan çıkış -> Önceki Map
        else if (_player.Position.Y > MAP_HEIGHT + 20)
        {
            if (_currentMapIndex > 1)
            {
                LoadMap(_currentMapIndex - 1);
                // Yeni mapte en yukarıdan başlat
                _player.SetPosition(new Vector2(_player.Position.X, 10));
            }
            else
            {
                // İlk maptesin, gidemezsin
                _player.SetPosition(new Vector2(_player.Position.X, MAP_HEIGHT - 64));
            }
        }
        }
        
        // Hasar sayıları
        for (int i = _damageNumbers.Count - 1; i >= 0; i--)
        {
            _damageNumbers[i].Update(deltaTime);
            if (_damageNumbers[i].IsExpired) _damageNumbers.RemoveAt(i);
        }
        
        // Log Update
        _gameLog.Update(gameTime);
        
        // Çanta butonu kontrolü
        Point mousePos = new Point(Mouse.GetState().X, Mouse.GetState().Y);
        _isHoveringBag = _bagButtonRect.Contains(mousePos);
        
        // Potion Shortcut (0)
        if (currentKeyState.IsKeyDown(Keys.D0) && !_previousKeyState.IsKeyDown(Keys.D0))
        {
            if (_inventory.UseFirstPotion())
            {
                 // Effect
                 _damageNumbers.Add(new DamageNumber(
                     _player.Center + new Vector2(0, -60),
                     0,
                     Color.Lime
                 ) { CustomText = "+Can" });
            }
        }
        
        if (_isHoveringBag && Mouse.GetState().LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            if (_inventory.IsOpen) _inventory.Close();
            else _inventory.Open();
        }
        
        _previousKeyState = currentKeyState;
        _previousMouseState = Mouse.GetState();
        base.Update(gameTime);
    }
    
    private void ToggleFullScreen()
    {
        if (_graphics.IsFullScreen)
        {
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.IsFullScreen = false;
            IsMouseVisible = true;
        }
        else
        {
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.IsFullScreen = true;
            IsMouseVisible = false;
        }
        _graphics.ApplyChanges();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // --- 1. WORLD SPACE - BACKGROUND (Wrap) ---
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, null, null, null, _camera.GetViewMatrix());
        
        // --- ARKA PLAN ÇİZİMİ ---
        if (_currentState != GameState.Login)
        {
            Rectangle mapRect = new Rectangle(0, 0, MAP_WIDTH, MAP_HEIGHT);
            _spriteBatch.Draw(_backgroundTexture, Vector2.Zero, mapRect, Color.White);
        }
        
        _spriteBatch.End();

        // --- 2. WORLD SPACE - ENTITIES (Clamp) ---
        // Clamp kullanarak sprite kenarlarındaki taşmaları (artifact/bleeding) engelliyoruz
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());

        if (_currentState == GameState.Login)
        {
            // Login logic handled in UI layer mostly
        }
        else
        {
            // Düşmanları çiz
            _enemyManager.Draw(_spriteBatch);
        
            // Satıcı NPC'yi çiz (Sadece Map 1)
            if (_currentMapIndex == 1)
            {
                _spriteBatch.Draw(_vendorTexture, _vendorPosition, Color.White);
            }
            
            // Oyuncuyu çiz
            _player.Draw(_spriteBatch);
            
            // Hasar sayılarını çiz
            foreach (var dmgNum in _damageNumbers)
            {
                dmgNum.Draw(_spriteBatch, _gameFont);
            }
        } 

        _spriteBatch.End();
        
        // --- 2. SCREEN SPACE UI (Transformsuz) ---
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend); // Varsayılan matrix=Identity
        
        if (_currentState == GameState.Login)
        {
            // Full screen solid background for Login
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), new Color(15, 15, 20));
            _titleScreen.Draw(_spriteBatch, _gameFont);
        }
        else if (_currentState == GameState.Playing || _currentState == GameState.Paused)
        {
            // Oyuncu sağlık barı (üst sol)
            DrawPlayerHealthBar();
            
            // Envanter
            _inventory.Draw(_spriteBatch, _gameFont);
            
            // Mobile UI Draw
            // _joystick.Draw(_spriteBatch);
            _spriteBatch.Draw(_statsIconTexture, _statsButtonRect, _isHoveringStats ? Color.White : new Color(200, 200, 200, 200));
            // 'C' Label
            _spriteBatch.DrawString(_gameFont, "C", new Vector2(_statsButtonRect.X - 15, _statsButtonRect.Center.Y - 10), Color.White, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
            
            // Eğer Enhancement Mode'daysa bilgi yaz
            if (_inventory.IsEnhancementMode)
            {
               string modeText = "BİR EŞYAYA TIKLA (YÜKSELTME İÇİN)";
               Vector2 mousePos = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
               _spriteBatch.DrawString(_gameFont, modeText, mousePos + new Vector2(15, 15), Color.Cyan);
            }
            
            // Enhancement UI (En üstte)
            _enhancementUI.Draw(_spriteBatch, _gameFont);
            _shopUI.Draw(_spriteBatch, _gameFont);
            
            // Satıcı etkileşim ipucu
            if (_nearVendor && !_shopUI.IsOpen && !_inventory.IsOpen)
            {
                string interactText = "[F] Etkileşime geç";
                Vector2 textSize = _gameFont.MeasureString(interactText);
                Vector2 textPos = new Vector2(
                    (GraphicsDevice.Viewport.Width - textSize.X) / 2,
                    GraphicsDevice.Viewport.Height - 140
                );
                _spriteBatch.DrawString(_gameFont, interactText, textPos + new Vector2(2, 2), new Color(0, 0, 0, 200));
                _spriteBatch.DrawString(_gameFont, interactText, textPos, Color.Yellow);
            }
            
            // Game Log Çiz
            _gameLog.Draw(_spriteBatch, _gameFont);
            
            // Çanta Butonu Çiz
            if (!_inventory.IsOpen && !_shopUI.IsOpen && !_enhancementUI.IsOpen) // Sadece UI kapalıyken veya her zaman? Genelde HUD parçasıdır.
            {
                Color bagTint = _isHoveringBag ? Color.White : new Color(220, 220, 220);
                _spriteBatch.Draw(_bagButtonTexture, _bagButtonRect, bagTint);
                // Tuş ipucu - 'I' Label
                 _spriteBatch.DrawString(_gameFont, "I", new Vector2(_bagButtonRect.X - 15, _bagButtonRect.Center.Y - 10), Color.White, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
            }
        }
        
        if (_currentState == GameState.Paused)
        {
            // Karartma
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), new Color(0, 0, 0, 200));
            
            // Başlık
            string title = _showSettingsInPause ? "AYARLAR" : "OYUN DURAKLATILDI";
            Vector2 titleSize = _gameFont.MeasureString(title);
            Vector2 titlePos = new Vector2(
                (GraphicsDevice.Viewport.Width - titleSize.X) / 2, 
                GraphicsDevice.Viewport.Height / 2 - 120
            );
            
            _spriteBatch.DrawString(_gameFont, title, titlePos + new Vector2(2, 2), Color.Black);
            _spriteBatch.DrawString(_gameFont, title, titlePos, Color.Gold);
            
            // Mouse Durumu
            MouseState ms = Mouse.GetState();
            Rectangle mouseRect = new Rectangle(ms.X, ms.Y, 1, 1);
            
            // Button Helper
            void DrawButton(string text, int yOffset, Color baseColor, Color hoverColor, Action onClick)
            {
                Vector2 size = _gameFont.MeasureString(text);
                Rectangle btnRect = new Rectangle(
                    (int)((GraphicsDevice.Viewport.Width - size.X) / 2) - 10,
                    (int)(titlePos.Y + yOffset),
                    (int)size.X + 20,
                    (int)size.Y + 10
                );
                
                bool isHover = btnRect.Intersects(mouseRect);
                
                // Arkaplan
                _spriteBatch.Draw(_pixelTexture, btnRect, isHover ? Color.DarkGray * 0.5f : Color.Black * 0.5f);
                
                // Çerçeve
                if (isHover) 
                {
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(btnRect.X, btnRect.Y, btnRect.Width, 2), Color.Gold);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(btnRect.X, btnRect.Bottom, btnRect.Width, 2), Color.Gold);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(btnRect.X, btnRect.Y, 2, btnRect.Height), Color.Gold);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(btnRect.Right, btnRect.Y, 2, btnRect.Height + 2), Color.Gold);
                }
                
                // Text
                _spriteBatch.DrawString(_gameFont, text, new Vector2(btnRect.X + 10, btnRect.Y + 5), isHover ? hoverColor : baseColor);
                
                // Click
                if (isHover && ms.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                {
                    onClick?.Invoke();
                }
            }
            
            if (!_showSettingsInPause)
            {
                // -- NORMAL PAUSE MENU --
                DrawButton("DEVAM ET", 80, Color.White, Color.Lime, () => {
                    _currentState = GameState.Playing;
                });
                
                DrawButton("KAYDET", 140, Color.LightBlue, Color.Cyan, () => {
                    SaveCurrentGame();
                    _saveConfirmationTimer = 2.0f;
                });
                
                if (_saveConfirmationTimer > 0)
                {
                    string msg = "OYUN KAYDEDİLDİ";
                    Vector2 msgSz = _gameFont.MeasureString(msg);
                    _spriteBatch.DrawString(_gameFont, msg, new Vector2((GraphicsDevice.Viewport.Width - msgSz.X)/2, titlePos.Y + 320), Color.Lime);
                }
                
                DrawButton("AYARLAR", 200, Color.LightGray, Color.Yellow, () => {
                    _showSettingsInPause = true;
                });

                DrawButton("ANA MENÜ", 260, Color.Orange, Color.Gold, () => {
                    SaveCurrentGame();
                    _currentState = GameState.Login;
                    _titleScreen.Reset(); // Goes to Main Menu
                });
            }
            else
            {
                 // -- SETTINGS SUBMENU --
                
                // --- VOLUME SLIDERS ---
                int sliderW = 300;
                int sliderH = 10;
                int sliderX = (GraphicsDevice.Viewport.Width - sliderW) / 2;
                int musicSliderY = (int)titlePos.Y + 100;
                int sfxSliderY = (int)titlePos.Y + 180;
                
                void DrawSlider(string label, int x, int y, float volume, bool isMusic)
                {
                    Rectangle barRect = new Rectangle(x, y, sliderW, sliderH);
                    Rectangle knobRect = new Rectangle(x + (int)(volume * sliderW) - 5, y - 5, 10, 20);
                    
                    // Label
                    string lblText = $"{label}: %{(int)(volume * 100)}";
                    Vector2 lblSz = _gameFont.MeasureString(lblText);
                    _spriteBatch.DrawString(_gameFont, lblText, new Vector2(x + (sliderW - lblSz.X)/2, y - 30), Color.LightGray);
                    
                    // Bar
                    _spriteBatch.Draw(_pixelTexture, barRect, Color.Gray * 0.5f);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, (int)(volume * sliderW), sliderH), Color.Lime);
                    
                    // Knob
                    bool isHover = knobRect.Contains(ms.X, ms.Y) || barRect.Contains(ms.X, ms.Y);
                    _spriteBatch.Draw(_pixelTexture, knobRect, isHover ? Color.Gold : Color.White);
                    
                    // Drag Logic
                    if (ms.LeftButton == ButtonState.Pressed)
                    {
                        if (isMusic && (_isDraggingMusic || barRect.Contains(ms.X, ms.Y)))
                        {
                            _isDraggingMusic = true;
                            float newVol = Math.Clamp((float)(ms.X - x) / sliderW, 0f, 1f);
                            _musicManager.SetVolume(newVol);
                        }
                        else if (!isMusic && (_isDraggingSFX || barRect.Contains(ms.X, ms.Y)))
                        {
                            _isDraggingSFX = true;
                            float newVol = Math.Clamp((float)(ms.X - x) / sliderW, 0f, 1f);
                            SoundEffect.MasterVolume = _sfxVolume = newVol;
                        }
                    }
                    else
                    {
                        if (isMusic) _isDraggingMusic = false;
                        else _isDraggingSFX = false;
                    }
                }
                
                DrawSlider("MÜZİK SESİ", sliderX, musicSliderY, _musicManager.MasterVolume, true);
                DrawSlider("SES EFEKTLERİ", sliderX, sfxSliderY, _sfxVolume, false);
                
                // BACK BUTTON
                 DrawButton("GERİ", 260, Color.White, Color.Cyan, () => {
                    _showSettingsInPause = false;
                });
            }
        }
        
        if (_currentState == GameState.Dead)
        {
            // Oyuncu öldüğünde de stat barı görünsün (belki)
            // Ama Revive UI en üstte olmalı
            _reviveUI.Draw(_spriteBatch, _gameFont);
        }
        
        if (_showStats)
        {
            _statsUI.Draw(_spriteBatch, _gameFont, _player);
        }
        
        _spriteBatch.End();

        base.Draw(gameTime);
    }
    
    private void DrawPlayerHealthBar()
    {
        int barWidth = 200;
        int barHeight = 20;
        int barX = 20;
        int barY = 20;
        
        // --- SAĞLIK BARI ---
        // Arka plan
        _spriteBatch.Draw(_pixelTexture, new Rectangle(barX - 2, barY - 2, barWidth + 4, barHeight + 4), new Color(30, 30, 40));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, barY, barWidth, barHeight), new Color(60, 20, 20));
        
        // Sağlık
        float healthPercent = (float)_player.CurrentHealth / _player.MaxHealth;
        int healthWidth = (int)(barWidth * healthPercent);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, barY, healthWidth, barHeight), new Color(180, 50, 50));
        
        // Parlaklık efekti
        _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, barY, healthWidth, 4), new Color(220, 80, 80));
        
        // Metin
        string healthText = $"{_player.CurrentHealth}/{_player.MaxHealth}";
        Vector2 textSize = _gameFont.MeasureString(healthText);
        _spriteBatch.DrawString(_gameFont, healthText, 
            new Vector2(barX + (barWidth - textSize.X) / 2, barY + 2), 
            Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            
        // --- XP BARI ---
        int xpBarHeight = 10;
        int xpBarY = barY + barHeight + 8;
        
        // Arka plan
        _spriteBatch.Draw(_pixelTexture, new Rectangle(barX - 2, xpBarY - 2, barWidth + 4, xpBarHeight + 4), new Color(30, 30, 40));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, xpBarY, barWidth, xpBarHeight), new Color(30, 30, 30));
        
        // XP
        float xpPercent = (float)_player.Experience / _player.MaxExperience;
        int xpWidth = (int)(barWidth * xpPercent);
        Color xpColor = new Color(50, 150, 250); // Mavi
        
        _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, xpBarY, xpWidth, xpBarHeight), xpColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, xpBarY, xpWidth, 2), new Color(100, 200, 255)); // Parlaklık
        
        // Level Metni
        string levelText = $"Lvl {_player.Level}";
        _spriteBatch.DrawString(_gameFont, levelText, new Vector2(barX + barWidth + 10, barY + 5), new Color(255, 200, 50));
        
        // --- ALTIN (Updated with Icon) ---
        string goldText = $"{_player.Gold}";
        Vector2 goldSz = _gameFont.MeasureString(goldText);
        Vector2 goldPos = new Vector2(barX + 35, barY + xpBarHeight + 35); // Icon width added
        
        if (_goldIconTexture != null)
        {
            // En boy oranını koruyarak çiz (basık görünmemesi için)
            float iconHeight = 24f;
            float aspectRatio = (float)_goldIconTexture.Width / _goldIconTexture.Height;
            int drawWidth = (int)(iconHeight * aspectRatio);
            
            _spriteBatch.Draw(_goldIconTexture, new Rectangle(barX, (int)goldPos.Y - 2, drawWidth, (int)iconHeight), Color.White);
            
            // Metin pozisyonunu ikona göre ayarla
            goldPos.X = barX + drawWidth + 8;
        }
        else 
        {
            _spriteBatch.DrawString(_gameFont, "G:", new Vector2(barX, goldPos.Y), Color.Gold);
        }
        
        _spriteBatch.DrawString(_gameFont, goldText, goldPos + new Vector2(0, -2), Color.Gold);
        
        // Skill Bar
        _skillBarUI.Draw(_spriteBatch, _gameFont);
        
        // Map Bilgisi (Sağ Üst)
        string mapText = $"Bölge: {_currentMapIndex}";
        if (_currentMapIndex == 1) mapText += " (Güvenli)";
        else if (_currentMapIndex == 2) mapText += " (Goblin)";
        else if (_currentMapIndex == 3) mapText += " (Örümcek)";
        else if (_currentMapIndex == 4) mapText += " (İskelet)";
        else if (_currentMapIndex == 5) mapText += " (Şeytan)";
        
        Vector2 mapSize = _gameFont.MeasureString(mapText);
        _spriteBatch.DrawString(_gameFont, mapText, 
            new Vector2(_graphics.PreferredBackBufferWidth - mapSize.X - 20, 20), 
            Color.LightGray);
            
         // Gold (Sağ Üstte de olabilir ama sol üstte HP barın altında daha iyi)
    }
    
    private void HandlePlayerDeath()
    {
        // 1. Para Cezası
        _player.LoseGold(_pendingDeathPenalty);
        
        // 2. Canı Fullle
        _player.CurrentHealth = _player.MaxHealth;
        
        // 3. Merkeze (Map 1) Işınla
        LoadMap(1);
        _player.SetPosition(new Vector2(MAP_WIDTH / 2f - 24, MAP_HEIGHT / 2f - 32));
        
        // 4. Bilgi Mesajı (Yeni map yüklendikten sonra ekle ki silinmesin)
        _damageNumbers.Add(new DamageNumber(
            _player.Center + new Vector2(0, -60),
            _pendingDeathPenalty,
            Color.Red
        ) { CustomText = $"ÖLDÜN! -{_pendingDeathPenalty} Gold" });
    }
    


    private Texture2D CreateDungeonFloor(int mapIndex)
    {
        int size = 64; // Her bir kare taşın boyutu
        Texture2D texture = new Texture2D(GraphicsDevice, size, size);
        Color[] data = new Color[size * size];
        Random rnd = new Random();
        
        // Temaya göre renkler
        Color baseColor = new Color(40, 45, 55); // Map 1: Standart
        Color jointColor = new Color(20, 20, 25);
        Color highlightColor = new Color(60, 65, 75);
        
        if (mapIndex == 2)
        {
            // Map 2: Goblin (Yosunlu/Yeşilimsi)
            baseColor = new Color(35, 45, 30);
            jointColor = new Color(15, 25, 10);
            highlightColor = new Color(50, 60, 45);
        }
        else if (mapIndex == 3)
        {
            // Map 3: Spider (Karanlık/Mor)
            baseColor = new Color(25, 20, 30);
            jointColor = new Color(10, 5, 15);
            highlightColor = new Color(40, 35, 50);
        }
        else if (mapIndex == 4)
        {
            // Map 4: Skeleton (Gri/Kemik Rengi)
            baseColor = new Color(50, 50, 55);
            jointColor = new Color(30, 30, 35);
            highlightColor = new Color(70, 70, 75);
        }
        else if (mapIndex == 5)
        {
            // Map 5: Demon (Alevli/Kırmızı)
            baseColor = new Color(50, 20, 15);
            jointColor = new Color(30, 10, 5);
            highlightColor = new Color(70, 30, 25);
        }
        
        for (int i = 0; i < size * size; i++)
        {
            // Hafif gürültü (Noise)
            int noise = rnd.Next(-5, 6);
            
            // Demon map için ekstra gürültü (lav efekti gibi)
            if (mapIndex == 4 && rnd.Next(100) < 5) noise += 20;
            
            data[i] = new Color(
                Math.Clamp(baseColor.R + noise, 0, 255),
                Math.Clamp(baseColor.G + noise, 0, 255),
                Math.Clamp(baseColor.B + noise, 0, 255)
            );
        }
        
        // Taş kenarlıkları (Derz)
        for (int x = 0; x < size; x++)
        {
            data[x] = jointColor; // Üst
            data[(size-1)*size + x] = jointColor; // Alt
            
            // İç detay
            if(x > 0 && x < size-1)
                data[size + x] = highlightColor; 
        }
        for (int y = 0; y < size; y++)
        {
            data[y*size] = jointColor; // Sol
            data[y*size + size-1] = jointColor; // Sağ
            
            // İç detay
            if(y > 0 && y < size-1)
                data[y*size + 1] = highlightColor;
        }
        
        texture.SetData(data);
        return texture;
    }
}

// Floating damage number class
public class DamageNumber
{
    public Vector2 Position { get; private set; }
    public int Damage { get; private set; }
    public Color Color { get; private set; }
    public float Alpha { get; private set; } = 1f;
    public bool IsExpired => Alpha <= 0;
    public string CustomText { get; set; } = null;
    
    private float _lifetime = 1.0f;
    private float _timer = 0f;
    private float _velocity = -50f;
    
    public DamageNumber(Vector2 position, int damage, Color color)
    {
        Position = position;
        Damage = damage;
        Color = color;
    }
    
    public void Update(float deltaTime)
    {
        _timer += deltaTime;
        Position = new Vector2(Position.X, Position.Y + _velocity * deltaTime);
        _velocity *= 0.98f; // Yavaşla
        
        // Fade out
        Alpha = 1f - (_timer / _lifetime);
    }
    
    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        if (IsExpired) return;
        
        string text = CustomText ?? Damage.ToString();
        Color drawColor = new Color(Color.R, Color.G, Color.B, (int)(255 * Alpha));
        
        // Gölge
        spriteBatch.DrawString(font, text, Position + new Vector2(1, 1), 
            new Color(0, 0, 0, (int)(150 * Alpha)), 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
        
        // Ana metin
        spriteBatch.DrawString(font, text, Position, drawColor, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
    }
}

public partial class Game1
{
    private Texture2D CreateStatsIcon(GraphicsDevice gd, int size)
    {
        Texture2D texture = new Texture2D(gd, size, size);
        Color[] data = new Color[size * size];
        for (int i = 0; i < data.Length; i++) data[i] = Color.Transparent;

        Color frameColor = Color.Gold;
        Color paperColor = Color.White;

        for (int y = 5; y < size - 5; y++)
        {
            for (int x = 10; x < size - 10; x++)
            {
                // Paper
                data[y * size + x] = paperColor;
                
                // Lines
                if ((y == 15 || y == 25 || y == 35 || y == 45) && x > 15 && x < size - 15)
                    data[y * size + x] = Color.Gray;

                // Frame
                if (x == 10 || x == size - 11 || y == 5 || y == size - 6)
                    data[y * size + x] = frameColor;
            }
        }
        texture.SetData(data);
        return texture;
    }
}
