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
    Paused
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
    private const int MAX_MAPS = 4;
    
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
    
    // Mobile UI
    private VirtualJoystick _joystick;
    private Rectangle _statsButtonRect;
    private bool _isHoveringStats;
    private Texture2D _statsIconTexture;
    


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
        _currentSlotIndex = slotIndex;
        
        if (newName != null)
        {
            // --- YENİ OYUN ---
            _playerName = newName;
            
            // Başlangıç itemlarını envantere ekle
            // Başlangıç itemlarını envantere ekle ve kuşan (Böylece skiller çalışır)
            Item startWeapon = ItemDatabase.GetItem(1); // Tahta Kılıç
            _player.EquipWeapon(startWeapon);
            _inventory.AddItem(startWeapon.Id, 1); 
            
            _inventory.AddItem(3); // Deri Zırh
            
            // Başlangıç pozisyonu
             _player.SetPosition(new Vector2(MAP_WIDTH / 2f - 24, MAP_HEIGHT / 2f - 32));
             _player.CurrentHealth = _player.MaxHealth;
             _player.Experience = 0;
             _player.Level = 1;
             _player.Gold = 0;
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
                 _player.GainExperience((int)data.Experience);
                 _player.GainGold(data.Gold);
                 
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
        
        _currentState = GameState.Playing;
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
            // Oynat Clash Sesi
            _sfxClash?.Play(0.3f, 0.0f, 0.0f);

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
                _gameLog.AddMessage($"+{goldAmount} Altin", Color.Gold);
                
                // === LOOT DROP ===
                var drops = LootManager.GetLoot(enemy.Type);
                foreach(var drop in drops)
                {
                    bool added = _inventory.AddItem(drop.ItemId, drop.Quantity);
                    if(added)
                    {
                        Item itemDrop = ItemDatabase.GetItem(drop.ItemId);
                        
                        // Drop bildirimi
                        _gameLog.AddMessage($"Kazanildi: {itemDrop.Name}", itemDrop.GetRarityColor());
                    }
                }
            }
        };
        
        // Item veritabanını başlat
        ItemDatabase.Initialize(GraphicsDevice);
        LootManager.Initialize(); // Loot tablolarını yükle
        
        // Envanter oluştur
        _inventory = new Inventory(GraphicsDevice, 
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
        
        // Başlangıç Eşyaları
        _inventory.AddItem(1); // Tahta Kılıç
        _inventory.AddItem(25); // Can İksiri
        _inventory.AddItem(25); // Can İksiri
        _inventory.AddItem(25); // Can İksiri
        
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
        _vendorTexture = CreateVendorTexture(GraphicsDevice);
        
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
                // İşlemi hemen yapma, bayrağı kaldır (Update başında yapacağız)
                _pendingDeathPenalty = (int)(_player.Gold * 0.10f);
                _playerNeedsRespawn = true;
            }
        };
        
        // İlk Haritayı Yükle
        // İlk Haritayı Yükle - MusicManager yüklendikten sonra çağrılacak
        // LoadMap(1);
        
        // Çanta Butonu Oluştur (Sağ Alt)
        int bagSize = 64;
        _bagButtonRect = new Rectangle(
            GraphicsDevice.Viewport.Width - bagSize - 20,
            GraphicsDevice.Viewport.Height - bagSize - 20,
            bagSize, bagSize
        );
        
        _bagButtonTexture = CreateBagIcon(GraphicsDevice, bagSize);
        
        // Title Screen
        _titleScreen = new TitleScreen(GraphicsDevice, 
            _graphics.PreferredBackBufferWidth, 
            _graphics.PreferredBackBufferHeight);
            
        _titleScreen.OnGameStart += OnTitleScreenStart;
        
        // Music Manager
        _musicManager = new MusicManager();
        _musicManager.LoadContent(Content);
        
        // Skill System Init
        _player.InitializeSkills(GraphicsDevice);
        _skillBarUI = new SkillBarUI(GraphicsDevice, _player.SkillManager, 
            _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            
        _statsUI = new StatsUI(GraphicsDevice, 
            _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            
        // Mobile UI Init
        _joystick = new VirtualJoystick(GraphicsDevice, new Vector2(150, _graphics.PreferredBackBufferHeight - 150), 80);
        _statsButtonRect = new Rectangle(_graphics.PreferredBackBufferWidth - 100, _graphics.PreferredBackBufferHeight - 180, 64, 64);
        _statsIconTexture = CreateStatsIcon(GraphicsDevice, 64);
            
        // Load SFX
        _sfxCoinPickup = Content.Load<SoundEffect>("SFX/sfx_coin_pickup");
        _sfxCoinBuy = Content.Load<SoundEffect>("SFX/sfx_coin_buy");
        _sfxCoinSell = Content.Load<SoundEffect>("SFX/sfx_coin_sell");
        _sfxCoinDrop = Content.Load<SoundEffect>("SFX/sfx_coin_drop");
        
        _inventory.SetCoinSounds(_sfxCoinPickup, _sfxCoinBuy, _sfxCoinSell, _sfxCoinDrop);
        _shopUI.SetCoinSounds(_sfxCoinBuy, _sfxCoinSell);
        
        // Enemy SFX
        Enemy.SfxDemonIdle = Content.Load<SoundEffect>("SFX/devil_sound");
        Enemy.SfxGoblinIdle = Content.Load<SoundEffect>("SFX/goblin_idle");
        Enemy.SfxGoblinDeath = Content.Load<SoundEffect>("SFX/goblin_death");
        
        // Player SFX
        Player.SfxCoinPickup = _sfxCoinPickup;
        
        LoadMap(1);
        

    }
    
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
        
        // Müzik Değiştir
        _musicManager.PlayMusicForMap(mapIndex);
        
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
            // --- MAP 4: DEMON HALL --- (Zor)
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
            
            // Resume
            if (currentKeyState.IsKeyDown(Keys.Escape) && !_previousKeyState.IsKeyDown(Keys.Escape))
            {
                _currentState = GameState.Playing;
                IsMouseVisible = !_graphics.IsFullScreen;
            }
            // Exit
            else if (currentKeyState.IsKeyDown(Keys.Enter) && !_previousKeyState.IsKeyDown(Keys.Enter))
            {
                SaveCurrentGame();
                Exit();
            }
            // Volume Control
            else if (currentKeyState.IsKeyDown(Keys.Up) && !_previousKeyState.IsKeyDown(Keys.Up))
            {
                _musicManager.IncreaseVolume();
            }
            else if (currentKeyState.IsKeyDown(Keys.Down) && !_previousKeyState.IsKeyDown(Keys.Down))
            {
                _musicManager.DecreaseVolume();
            }
            
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
                _shopUI.Open(_player, _inventory);
            }
        }

        if (currentKeyState.IsKeyDown(Keys.Escape) && !_previousKeyState.IsKeyDown(Keys.Escape))
        {
            if (!_inventory.IsOpen && !_shopUI.IsOpen && !_enhancementUI.IsOpen)
            {
                 _currentState = GameState.Paused;
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
        _joystick.Update(currentMouseState, _previousMouseState);
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
        
        // Player update with joystick
        _player.Update(gameTime, MAP_WIDTH, MAP_HEIGHT, _enemyManager, _joystick.InputDirection);
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

        // --- 1. WORLD SPACE (Kamera Transformu ile) ---
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, null, null, null, _camera.GetViewMatrix());
        
        // --- ARKA PLAN ÇİZİMİ ---
        if (_currentState != GameState.Login)
        {
            Rectangle mapRect = new Rectangle(0, 0, MAP_WIDTH, MAP_HEIGHT);
            _spriteBatch.Draw(_backgroundTexture, Vector2.Zero, mapRect, Color.White);
        }

        if (_currentState == GameState.Login)
        {
            // Login textleri aslında UI Layer'da olmalı ama burada da kalabilir, 
            // fakat kamera transformundan etkilensin istemiyorsak UI batch'e taşımalıyız.
            // Ama login ekranında kamera 0,0'da olacağı için sorun olmaz.
            // Yine de title UI logic.
            
            // Biz Login'i UI batch'e taşıyalım, burada sadece entities çizilsin.
        }
        else
        {
            // Düşmanları çiz
            _enemyManager.Draw(_spriteBatch);
        
            // Satıcı NPC'yi çiz (Sadece Map 1)
            if (_currentMapIndex == 1)
            {
                _spriteBatch.Draw(_vendorTexture, _vendorPosition, Color.White);
                
                string vendorName = "SATICI";
                Vector2 namePos = new Vector2(_vendorPosition.X + 24 - _gameFont.MeasureString(vendorName).X * 0.3f, _vendorPosition.Y - 15);
                _spriteBatch.DrawString(_gameFont, vendorName, namePos, Color.Yellow, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
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
            _joystick.Draw(_spriteBatch);
            _spriteBatch.Draw(_statsIconTexture, _statsButtonRect, _isHoveringStats ? Color.White : new Color(200, 200, 200, 200));
            
            // Eğer Enhancement Mode'daysa bilgi yaz
            if (_inventory.IsEnhancementMode)
            {
               string modeText = "BIR ESYAYA TIKLA (YUKSELTME ICIN)";
               Vector2 mousePos = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
               _spriteBatch.DrawString(_gameFont, modeText, mousePos + new Vector2(15, 15), Color.Cyan);
            }
            
            // Enhancement UI (En üstte)
            _enhancementUI.Draw(_spriteBatch, _gameFont);
            _shopUI.Draw(_spriteBatch, _gameFont);
            
            // Satıcı etkileşim ipucu
            if (_nearVendor && !_shopUI.IsOpen && !_inventory.IsOpen)
            {
                string interactText = "[F] Saticiyla Konus";
                Vector2 textSize = _gameFont.MeasureString(interactText);
                Vector2 textPos = new Vector2(
                    (GraphicsDevice.Viewport.Width - textSize.X) / 2,
                    GraphicsDevice.Viewport.Height - 80
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
                // Tuş ipucu
                 _spriteBatch.DrawString(_gameFont, "[I]", new Vector2(_bagButtonRect.X, _bagButtonRect.Y - 20), Color.White);
            }
        }
        
        if (_currentState == GameState.Paused)
        {
            // Karartma
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), new Color(0, 0, 0, 150));
            
            // Başlık
            string title = "OYUN DURAKLATILDI";
            Vector2 titleSize = _gameFont.MeasureString(title);
            Vector2 titlePos = new Vector2(
                (GraphicsDevice.Viewport.Width - titleSize.X) / 2, 
                GraphicsDevice.Viewport.Height / 2 - 50
            );
            
            _spriteBatch.DrawString(_gameFont, title, titlePos + new Vector2(2, 2), Color.Black);
            _spriteBatch.DrawString(_gameFont, title, titlePos, Color.Gold);
            
            // Seçenekler
            string resumeText = "[ESC] Devam Et";
            string exitText = "[ENTER] Cikis";
            
            Vector2 resumeSize = _gameFont.MeasureString(resumeText);
            Vector2 exitSize = _gameFont.MeasureString(exitText);
            
            Vector2 resumePos = new Vector2((GraphicsDevice.Viewport.Width - resumeSize.X) / 2, titlePos.Y + 60);
            Vector2 exitPos = new Vector2((GraphicsDevice.Viewport.Width - exitSize.X) / 2, resumePos.Y + 40);
            
            _spriteBatch.DrawString(_gameFont, resumeText, resumePos, Color.White);
            _spriteBatch.DrawString(_gameFont, exitText, exitPos, Color.IndianRed);
            
            // Volume Display
            string volText = $"Muzik Sesi: %{(int)(_musicManager.MasterVolume * 100)}";
            Vector2 volSize = _gameFont.MeasureString(volText);
            Vector2 volPos = new Vector2((GraphicsDevice.Viewport.Width - volSize.X) / 2, exitPos.Y + 50);
            
            _spriteBatch.DrawString(_gameFont, volText, volPos, Color.LightGreen);
            
            string volHint = "[YUKARI/ASAGI] Ayarla";
            Vector2 hintSize = _gameFont.MeasureString(volHint);
            _spriteBatch.DrawString(_gameFont, volHint, 
                new Vector2((GraphicsDevice.Viewport.Width - hintSize.X) / 2, volPos.Y + 25), 
                Color.Gray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
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
        
        // --- ALTIN ---
        // Sol üst köşe, can barının üstü veya yanı
        // Sol üst köşe, can barının üstü veya yanı
        string goldText = $"{_player.Gold} G";
        _spriteBatch.DrawString(_gameFont, goldText, new Vector2(barX, barY + xpBarHeight + 35), Color.Gold);
        
        // Skill Bar
        _skillBarUI.Draw(_spriteBatch, _gameFont);
        
        // Map Bilgisi (Sağ Üst)
        string mapText = $"Bolge: {_currentMapIndex}";
        if (_currentMapIndex == 1) mapText += " (Guvenli)";
        else if (_currentMapIndex == 2) mapText += " (Goblin)";
        else if (_currentMapIndex == 3) mapText += " (Orumcek)";
        else if (_currentMapIndex == 4) mapText += " (Seytan)";
        
        Vector2 mapSize = _gameFont.MeasureString(mapText);
        _spriteBatch.DrawString(_gameFont, mapText, 
            new Vector2(_graphics.PreferredBackBufferWidth - mapSize.X - 20, 20), 
            Color.LightGray);
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
        ) { CustomText = $"OLDUN! -{_pendingDeathPenalty} Gold" });
    }
    
    private Texture2D CreateVendorTexture(GraphicsDevice gd)
    {
        int width = 48;
        int height = 64;
        Texture2D texture = new Texture2D(gd, width, height);
        Color[] data = new Color[width * height];
        
        // Renkler
        Color robeColor = new Color(80, 40, 120); // Mor cüppe
        Color skinColor = new Color(220, 180, 140);
        Color goldAccent = new Color(255, 200, 50);
        Color darkRobe = new Color(50, 25, 80);
        
        int centerX = width / 2;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;
                data[i] = Color.Transparent;
                
                // Kafa (Daire)
                if (y >= 5 && y < 20)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, 12));
                    if (dist < 8)
                    {
                        data[i] = skinColor;
                        // Gözler
                        if (y == 11 && (x == centerX - 3 || x == centerX + 3))
                            data[i] = Color.Black;
                        // Ağız
                        if (y == 15 && Math.Abs(x - centerX) < 2)
                            data[i] = new Color(150, 100, 100);
                    }
                }
                
                // Kapşon (Baş etrafında)
                if (y >= 2 && y < 18)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, 10));
                    if (dist >= 9 && dist < 13)
                    {
                        data[i] = darkRobe;
                    }
                }
                
                // Cüppe (Gövde)
                if (y >= 18 && y < height - 4)
                {
                    int robeWidth = 12 + (y - 18) / 3;
                    if (Math.Abs(x - centerX) < robeWidth)
                    {
                        data[i] = robeColor;
                        // Altın şerit
                        if (Math.Abs(x - centerX) < 2)
                            data[i] = goldAccent;
                        // Kenar
                        if (Math.Abs(x - centerX) >= robeWidth - 2)
                            data[i] = darkRobe;
                    }
                }
                
                // Eller (Yanlarda)
                if (y >= 30 && y < 40)
                {
                    if ((x >= 5 && x < 12) || (x >= width - 12 && x < width - 5))
                    {
                        data[i] = skinColor;
                    }
                }
            }
        }
        
        texture.SetData(data);
        return texture;
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
            // Map 4: Demon (Alevli/Kırmızı)
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
