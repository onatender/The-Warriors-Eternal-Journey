using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace EternalJourney;

public class Player
{
    // Pozisyon ve hareket
    private Vector2 _position;
    private Vector2 _velocity;
    private float _speed = 300f;
    
    // Boyut
    private int _width = 48; 
    private int _height = 64;
    
    // Görsel
    private Texture2D _texture;
    private Texture2D _legTexture; 
    private Texture2D _armTexture; // Yeni Kol Texture
    private Texture2D _weaponTexture;
    private Texture2D _sweepTexture; 
    private Texture2D _pixelTexture; // Parçacıklar için
    private GraphicsDevice _graphicsDevice;
    
    // Equipped items
    private Item _equippedWeapon;
    private Item _equippedArmor;
    
    // Animasyon efekti için
    private float _animationTimer = 0f;
    private bool _isMoving = false;
    private int _facingDirection = 1; // 1 = sağ, -1 = sol
    
    // Saldırı sistemi
    private float _attackTimer = 0f;
    private float _attackCooldown = 0f;
    private bool _isAttacking = false;
    public float GetAttackRadius() => _attackRadius;
    public Enemy GetCurrentTarget() => _currentTarget;
    
    // --- SKILL SYSTEM FIELDS ---
    public SkillManager SkillManager { get; private set; }
    
    // States
    private bool _isSpinning = false;
    private float _spinTimer = 0f;
    private float _spinDuration = 2.0f; // Increased to 2.0s
    private float _spinDamageTimer = 0f;
    
    private bool _isDashing = false;
    private Enemy _dashTarget;
    private List<Enemy> _dashedEnemies = new List<Enemy>();
    private int _dashCount = 0;
    private const int MAX_DASH_CHAIN = 4;
    private float _dashSpeed = 1500f; // Very fast
    
    public void InitializeSkills(GraphicsDevice gd)
    {
        SkillManager = new SkillManager(gd);
    }
    
    // SFX
    private SoundEffect _sfxSlice1;
    private SoundEffect _sfxSlice2;
    private SoundEffect _sfxWalk;
    private SoundEffectInstance _walkSoundInstance;

    public void SetCombatSounds(SoundEffect s1, SoundEffect s2)
    {
        _sfxSlice1 = s1;
        _sfxSlice2 = s2;
    }
    
    public void SetWalkSound(SoundEffect walkSound)
    {
        _sfxWalk = walkSound;
        if (_sfxWalk != null)
        {
            _walkSoundInstance = _sfxWalk.CreateInstance();
            _walkSoundInstance.IsLooped = true;
            _walkSoundInstance.Volume = 0.3f; // Düşük ses seviyesi
        }
    }
    
    // --- PARÇACIK SİSTEMİ ---
    private struct PlayerParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Size;
        public float Life;
        public float Decay;
        public bool IsBehind; // Karakterin arkasında mı?
        public float Rotation; // Added
        public Vector2 Scale; // Added
    }
    private List<PlayerParticle> _particles = new List<PlayerParticle>();
    
    public void GainExperience(int amount)
    {
        if (Level >= MAX_LEVEL) return;
        
        Experience += amount;
        
        // Level Up Kontrolü
        while (Experience >= MaxExperience && Level < MAX_LEVEL)
        {
            Experience -= MaxExperience;
            Level++; // MaxExperience setter içinde güncellenir
            MaxHealth += 10; 
            CurrentHealth = MaxHealth; 
            OnLevelUp?.Invoke();
        }
    }
    private Enemy _currentTarget = null;
    private Random _random = new Random();
    
    // Otomatik saldırı ayarları
    private float _attackRadius = 90f; 
    private float _baseAttackSpeed = 1.0f; 
    
    // Combat Status
    private float _timeSinceLastCombat = 0f;
    private float _regenTimer = 0f;
    
    public Vector2 Position => _position;
    public void SetPosition(Vector2 newPos) { _position = newPos; }
    
    public void StopMoving()
    {
        _velocity = Vector2.Zero;
        _isMoving = false;
        if (_walkSoundInstance != null && _walkSoundInstance.State == SoundState.Playing)
        {
            _walkSoundInstance.Stop();
        }
    }
    
    public Vector2 Center => new Vector2(_position.X + _width / 2, _position.Y + _height / 2);
    public Rectangle Bounds => new Rectangle((int)_position.X, (int)_position.Y, _width, _height);
    
    // İstatistikler
    private int _level = 1;
    public int Level 
    { 
        get => _level; 
        set 
        {
            _level = value;
            MaxExperience = (long)(100 * Math.Pow(_level, 1.2));
        }
    }
    
    public static SoundEffect SfxCoinPickup;
    
    public void GainGold(int amount, bool silent = false)
    {
        Gold += amount;
        if (!silent && amount > 0) SfxCoinPickup?.Play();
    }
    
    public void LoseGold(int amount)
    {
        Gold -= amount;
        if (Gold < 0) Gold = 0;
    }
    
    public int CurrentHealth { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    
    // XP Sistemi
    public long Experience { get; set; } = 0;
    public long MaxExperience { get; private set; } = 100; 
    public const int MAX_LEVEL = 99;
    
    // Altın Sistemi
    public int Gold { get; set; } = 0;
    

    
    // Ekipman - Kalkan ve Kask
    private Item _equippedShield = null;
    private Item _equippedHelmet = null;
    
    public void EquipShield(Item shield)
    {
        _equippedShield = shield;
    }
    
    public void EquipHelmet(Item helmet)
    {
        _equippedHelmet = helmet;
    }
    
    public Item GetEquippedShield() => _equippedShield;
    public Item GetEquippedHelmet() => _equippedHelmet;
    
    // Bloklama Kontrolü - Dışarıdan çağrılacak
    public bool TryBlock()
    {
        if (_equippedShield == null) return false;
        
        // Bloklama şansı = Kalkanın BlockChance + (Seviye * 2)
        int blockChance = GetBlockChance();
        return _random.Next(100) < blockChance;
    }

    public int GetBlockChance()
    {
        if (_equippedShield == null) return 0;
        return _equippedShield.BlockChance + (_equippedShield.EnhancementLevel * 2);
    }
    
    // Toplam Savunma Hesapla (Zırh + Kalkan + Kask)
    public int GetTotalDefense()
    {
        int defense = 0;
        if (_equippedArmor != null) defense += _equippedArmor.Defense;
        if (_equippedShield != null) defense += _equippedShield.Defense;
        if (_equippedHelmet != null) defense += _equippedHelmet.Defense;
        return defense;
    }
    
    // Saldırı event
    public event Action<Enemy, int> OnAttackHit;
    public event Action OnLevelUp;
    
    public Player(GraphicsDevice graphicsDevice, Vector2 startPosition)
    {
        _graphicsDevice = graphicsDevice;
        _position = startPosition;
        _velocity = Vector2.Zero;
        
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        CreateTexture(graphicsDevice);
        CreateLegTexture(graphicsDevice); 
        CreateArmTexture(graphicsDevice); // Kol texture oluştur
        CreateWeaponTexture(graphicsDevice, new Color(139, 90, 43));
        CreateSweepTexture(graphicsDevice);
    }
    
    // Kol Texture Oluşturucu
    // Kol Texture Oluşturucu
    private void CreateArmTexture(GraphicsDevice graphicsDevice)
    {
        int w = 6; int h = 16; // Kalın ve biraz daha uzun
        _armTexture = new Texture2D(graphicsDevice, w, h);
        Color[] data = new Color[w*h];
        Color skinColor = new Color(245, 210, 180);
        
        for(int i=0; i<w*h; i++) 
        {
            data[i] = skinColor;
            // Kenar çizgisi
            if (i < w || i >= w*(h-1) || i%w == 0 || i%w == w-1)
                data[i] = Color.Lerp(skinColor, Color.Sienna, 0.5f);
        }
        
        _armTexture.SetData(data);
    }
    
    // Bacak Texture Oluşturucu
    private void CreateLegTexture(GraphicsDevice graphicsDevice)
    {
        int w = 10; int h = 26;
        _legTexture = new Texture2D(graphicsDevice, w, h);
        Color[] data = new Color[w*h];
        Color skinColor = new Color(245, 210, 180);
        Color underwearColor = new Color(200, 200, 200); 
        
        for(int i=0; i<w*h; i++) {
            int y = i/w;
            data[i] = skinColor;
            
            // Üst kısım (Don/Pantolon)
            if (y < 10) {
                 data[i] = underwearColor;
                 if (_equippedArmor != null) {
                     if (_equippedArmor.Id == 11) data[i] = new Color(30, 10, 10);
                     else data[i] = new Color(90, 60, 40);
                 }
            }
            // Alt kısım (Bot)
            if (y > 18 && _equippedArmor != null) {
                 if (_equippedArmor.Id == 11) data[i] = new Color(20, 5, 5);
                 else data[i] = new Color(50, 40, 30);
            }
        }
        _legTexture.SetData(data);
    }
    
    private void CreateTexture(GraphicsDevice graphicsDevice)
    {
        _width = 48;
        _height = 64;
        
        _texture = new Texture2D(graphicsDevice, _width, _height);
        Color[] colorData = new Color[_width * _height];
        
        // Renk paleti
        Color skinLight = new Color(255, 220, 190);
        Color skinMid = new Color(235, 195, 160);
        Color skinDark = new Color(200, 160, 130);
        Color hairDark = new Color(45, 30, 20);
        Color hairMid = new Color(65, 45, 30);
        Color eyeColor = new Color(40, 80, 120);
        
        // Varsayılan giysi renkleri (zırh yoksa)
        Color shirtMain = new Color(80, 60, 50);
        Color shirtDark = new Color(60, 45, 35);
        Color shirtLight = new Color(100, 75, 60);
        
        // Zırh varsa renkleri değiştir
        if (_equippedArmor != null)
        {
            if (_equippedArmor.Id == 11) // Ejderha Zırhı
            {
                shirtMain = new Color(50, 15, 15);
                shirtDark = new Color(30, 10, 10);
                shirtLight = new Color(120, 30, 30);
            }
            else if (_equippedArmor.Id == 10) // Demir Zırh
            {
                shirtMain = new Color(140, 145, 160);
                shirtDark = new Color(100, 105, 120);
                shirtLight = new Color(180, 185, 200);
            }
            else // Deri/Kumaş
            {
                shirtMain = new Color(110, 80, 55);
                shirtDark = new Color(85, 60, 40);
                shirtLight = new Color(140, 100, 70);
            }
        }
        
        int centerX = _width / 2;
        
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int index = y * _width + x;
                colorData[index] = Color.Transparent;
                
                int dx = x - centerX;
                
                // ===== KAFA (y: 2-22) =====
                if (y >= 2 && y < 22)
                {
                    // Saç (üst kısım)
                    if (y < 10)
                    {
                        // Saç şekli - daha geniş üstte
                        int hairWidth = 11 - (y - 2) / 3;
                        if (Math.Abs(dx) < hairWidth)
                        {
                            colorData[index] = (dx < 0) ? hairDark : hairMid;
                            // Saç parlaklık
                            if (y < 5 && dx > 1 && dx < 4) colorData[index] = hairMid;
                        }
                    }
                    
                    // Yüz
                    if (y >= 8 && y < 22)
                    {
                        int faceWidth = y < 12 ? 9 : (y < 18 ? 8 : 7 - (y - 18) / 2);
                        if (Math.Abs(dx) < faceWidth)
                        {
                            // Temel ten rengi
                            colorData[index] = skinMid;
                            
                            // Sol taraf gölge
                            if (dx < -3) colorData[index] = skinDark;
                            // Sağ taraf parlak
                            if (dx > 2 && dx < 5) colorData[index] = skinLight;
                            
                            // Gözler (y: 12-14)
                            if (y >= 12 && y <= 14)
                            {
                                // Sol göz
                                if (dx >= -5 && dx <= -3)
                                {
                                    colorData[index] = (y == 13) ? eyeColor : Color.White;
                                    if (y == 13 && dx == -4) colorData[index] = Color.Black; // Göz bebeği
                                }
                                // Sağ göz  
                                if (dx >= 2 && dx <= 4)
                                {
                                    colorData[index] = (y == 13) ? eyeColor : Color.White;
                                    if (y == 13 && dx == 3) colorData[index] = Color.Black; // Göz bebeği
                                }
                            }
                            
                            // Burun (y: 15-17)
                            if (y >= 15 && y <= 17 && dx >= -1 && dx <= 1)
                            {
                                colorData[index] = skinDark;
                                if (dx == 0 && y == 16) colorData[index] = skinMid;
                            }
                            
                            // Ağız (y: 19)
                            if (y == 19 && Math.Abs(dx) < 3)
                            {
                                colorData[index] = new Color(180, 120, 110);
                            }
                        }
                    }
                }
                
                // ===== BOYUN (y: 22-25) =====
                if (y >= 22 && y < 25)
                {
                    if (Math.Abs(dx) < 5)
                    {
                        colorData[index] = skinDark;
                    }
                }
                
                // ===== GÖVDE (y: 25-48) =====
                if (y >= 25 && y < 48)
                {
                    // Omuz genişliği üstte daha geniş
                    int bodyWidth = y < 30 ? 12 : (y < 40 ? 11 : 10);
                    
                    if (Math.Abs(dx) < bodyWidth)
                    {
                        colorData[index] = shirtMain;
                        
                        // Sol gölge
                        if (dx < -bodyWidth + 3) colorData[index] = shirtDark;
                        // Sağ parlaklık
                        if (dx > bodyWidth - 4) colorData[index] = shirtLight;
                        
                        // Orta dikey çizgi (giysi detayı)
                        if (Math.Abs(dx) < 2 && y > 28)
                        {
                            colorData[index] = shirtDark;
                        }
                        
                        // Kemer (y: 42-45)
                        if (y >= 42 && y < 46)
                        {
                            colorData[index] = new Color(60, 45, 30);
                            if (y == 43) colorData[index] = new Color(80, 60, 40);
                            // Kemer tokası
                            if (Math.Abs(dx) < 3 && y >= 43 && y <= 44)
                            {
                                colorData[index] = new Color(180, 160, 80);
                            }
                        }
                    }
                }
                
                // ===== BACAKLAR (y: 48-62) - Basit placeholder =====
                // (Bacaklar ayrı texture ile çiziliyor, bu sadece dolgu)
            }
        }
        
        _texture.SetData(colorData);
    }
    
    private void CreateWeaponTexture(GraphicsDevice graphicsDevice, Color bladeColor)
    {
        int w = 12; int h = 48;
        _weaponTexture = new Texture2D(graphicsDevice, w, h);
        Color[] c = new Color[w*h];
        
        Color silver = new Color(200, 200, 210);
        Color darkSilver = new Color(140, 140, 150);
        Color handleColor = new Color(80, 50, 30);
        Color guardColor = new Color(180, 160, 50); 
        
        // Efsanevi kontrolü
        bool isLegendary = _equippedWeapon != null && _equippedWeapon.Id == 10;
        if (isLegendary)
        {
            silver = new Color(255, 100, 50); // Alev rengi
            darkSilver = new Color(150, 50, 20);
            guardColor = Color.Gold;
        }
        
        for (int i = 0; i < w*h; i++)
        {
            int x = i % w; int y = i / w;
            int cx = w / 2;
            
            if (y >= h - 10 && Math.Abs(x - cx) <= 2)
            {
                c[i] = handleColor;
                if (y == h-1) c[i] = guardColor; 
            }
            else if (y >= h - 14 && y < h - 10)
            {
                if (Math.Abs(x - cx) <= 5) c[i] = guardColor;
            }
            else if (y < h - 14)
            {
                if (Math.Abs(x - cx) <= 3)
                {
                    c[i] = silver;
                    if (x == cx) c[i] = isLegendary ? Color.Yellow : new Color(240, 240, 255); 
                    if (Math.Abs(x - cx) == 3) c[i] = darkSilver; 
                    
                    if (y < 4 && Math.Abs(x - cx) > (y/2)) c[i] = Color.Transparent; 
                }
            }
        }
        _weaponTexture.SetData(c);
    }
    
    private void CreateSweepTexture(GraphicsDevice graphicsDevice)
    {
        int size = 64;
        _sweepTexture = new Texture2D(graphicsDevice, size, size);
        Color[] data = new Color[size*size];
        float cx = size/2f; float cy = size/2f;
        
        // Efsanevi ise kırmızı sweep
        Color sweepColor = (_equippedWeapon != null && _equippedWeapon.Id == 10) ? Color.OrangeRed : Color.White;
        
        for(int y=0; y<size; y++) {
            for(int x=0; x<size; x++) {
                int i = y*size+x;
                float dist = Vector2.Distance(new Vector2(x,y), new Vector2(cx, cy));
                
                if (dist > 20 && dist < 30 && y < cy) {
                    float alpha = (float)(x) / size; 
                    data[i] = sweepColor * alpha;
                }
                else data[i] = Color.Transparent;
            }
        }
        _sweepTexture.SetData(data);
    }

    public void UpdateCombat(GameTime gameTime, EnemyManager enemyManager)
    {
        if (_equippedWeapon == null) return;
        
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Enemy nearestEnemy = enemyManager.GetNearestEnemy(Center, _attackRadius);
        
        if (nearestEnemy != null && !nearestEnemy.IsDead)
        {
            if (nearestEnemy.Position.X > Center.X) _facingDirection = 1;
            else _facingDirection = -1;
            
            if (_attackCooldown <= 0 && !_isAttacking)
            {
                PerformAttack(enemyManager);
                _timeSinceLastCombat = 0f; 
            }
        }
    }
    
    public void Heal(int amount)
    {
        CurrentHealth += amount;
        if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth;
        
        // Healing Effect (Green Particles)
        for(int i=0; i<15; i++)
        {
            Vector2 pos = Center + new Vector2(_random.Next(-20, 20), _random.Next(-30, 30));
            _particles.Add(new PlayerParticle
            {
                Position = pos,
                Velocity = new Vector2(_random.Next(-20, 20), _random.Next(-50, -10)), // Upward float
                Color = Color.LightGreen,
                Size = _random.Next(3, 6),
                Life = 1.0f,
                Decay = 1.0f,
                IsBehind = false
            });
        }
    }
    
    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth < 0) CurrentHealth = 0;
        _timeSinceLastCombat = 0f;
    }
    
    private void PerformAttack(EnemyManager enemyManager)
    {
        _isAttacking = true;
        _attackTimer = 0f;
        
        // Play Attack Sound
        if (_random.Next(2) == 0) _sfxSlice1?.Play(0.4f, 0f, 0f);
        else _sfxSlice2?.Play(0.4f, 0f, 0f);
        
        float attackSpeed = _equippedWeapon.AttackSpeed / 50f;
        _attackCooldown = 1f / (_baseAttackSpeed * attackSpeed);
        
        List<Enemy> targets = enemyManager.GetEnemiesInArea(Center, _attackRadius, _facingDirection);
        foreach (var target in targets)
        {
            int damage = _random.Next(_equippedWeapon.MinDamage, _equippedWeapon.MaxDamage + 1);
            target.TakeDamage(damage);
            OnAttackHit?.Invoke(target, damage);
        }
        
        // Saldırı anında alev efekti (Efsanevi Kılıç)
        if (_equippedWeapon != null && _equippedWeapon.Id == 10)
        {
            // Geniş bir alana parçacık saç
            for(int i=0; i<20; i++)
            {
                Vector2 pos = Center + new Vector2(_facingDirection * 40, 0) + new Vector2(_random.Next(-20, 20), _random.Next(-30, 30));
                _particles.Add(new PlayerParticle
                {
                    Position = pos,
                    Velocity = new Vector2(_facingDirection * _random.Next(50, 150), _random.Next(-50, 50)),
                    Color = Color.OrangeRed,
                    Size = _random.Next(2, 6),
                    Life = 0.5f,
                    Decay = 2.0f,
                    IsBehind = false
                });
            }
        }
    }
    
    public void EquipWeapon(Item weapon)
    {
        _equippedWeapon = weapon;
        if (weapon != null)
        {
            CreateWeaponTexture(_graphicsDevice, Color.White);
            CreateSweepTexture(_graphicsDevice); // Rengi güncelle
        }
    }
    
    public void EquipArmor(Item armor)
    {
        _equippedArmor = armor;
        CreateTexture(_graphicsDevice); 
        CreateLegTexture(_graphicsDevice); // Bacakları güncelle
    }

    // --- SKILL LOGIC ---
    public void UpdateSkills(GameTime gameTime, EnemyManager enemyManager)
    {
        if (_equippedWeapon == null)
        {
            // For testing: Equip default sword if none
            // This ensures skills work even if save data is weird
            // But ideally we should have a weapon from start.
            // Let's just return for now, but maybe log it?
            return; 
        }
        
        SkillManager.Update(gameTime);
        
        KeyboardState kState = Keyboard.GetState();
        
        // Skill 1: Whirlwind (Key 1 or Q)
        if ((kState.IsKeyDown(Keys.D1) || kState.IsKeyDown(Keys.Q)) && !_isSpinning && !_isDashing)
        {
            var skill = SkillManager.GetSkill(SkillType.Whirlwind);
            if (skill != null && skill.IsReady)
            {
                skill.Use();
                StartSpin();
            }
        }
        
        // Skill 2: Dash Strike (Key 2 or E)
        if ((kState.IsKeyDown(Keys.D2) || kState.IsKeyDown(Keys.E)) && !_isSpinning && !_isDashing)
        {
             var skill = SkillManager.GetSkill(SkillType.DashStrike);
             if (skill != null && skill.IsReady)
             {
                 // Try to find target
                 Enemy target = enemyManager.GetNearestEnemy(Center, 400f);
                 if (target != null)
                 {
                     skill.Use();
                     StartDash(target);
                 }
             }
        }
    }
    
    private void StartSpin()
    {
        _isSpinning = true;
        _spinTimer = 0f;
        // Start cleanly, damage will trigger after first rotation completes (~0.3s)
        // OR set to threshold to trigger instantly? User prefers instant usually.
        // Let's set to 0. It takes 0.3s to do a 360 spin. It makes sense to hit AFTER the swing passes.
        // But if user complains about "no damage", maybe instant is better?
        // User said: "kılıç dönerken her turda hasar vermeli" -> imply continuous.
        // I will stick to 0 for now. The bug was in Enemy.cs.
        _spinDamageTimer = 0f; 
    }
    
    private void UpdateSpin(GameTime gameTime, EnemyManager enemyManager)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _spinTimer += dt;
        _spinDamageTimer += dt;
        
        // Visual Rotation Speed
        float spinSpeed = 20f;
        float timePerRotation = MathHelper.TwoPi / spinSpeed; // ~0.31s

        // Damage Tick every rotation
        if (_spinDamageTimer >= timePerRotation)
        {
            _spinDamageTimer -= timePerRotation; // Keep exact sync
            
            // AoE Damage
            var targets = enemyManager.GetEnemiesInArea(Center, 120f, 0); // Increased range slightly
             foreach (var target in targets)
            {
                // Rebalanced: Normal Damage per hit (1.0f)
                int damage = (int)(_random.Next(_equippedWeapon.MinDamage, _equippedWeapon.MaxDamage) * 1.0f); 
                target.TakeDamage(damage);
                OnAttackHit?.Invoke(target, damage);
                
                // Hit effect
                 _particles.Add(new PlayerParticle
                 {
                    Position = target.Position + new Vector2(_random.Next(-10,10), _random.Next(-20,0)), // Higher up for visibility
                    Color = Color.White,
                    Size = 3,
                    Life = 0.3f,
                    Decay = 3f
                 });
            }
            
            // Spin Sound Effect (looping slice?)
            if (_random.Next(2)==0) _sfxSlice1?.Play(0.2f, 0.5f, 0f); // Higher pitch for speed
        }
        
        // Particles
        for(int i=0; i<2; i++)
        {
             float angle = _spinTimer * 20f + i * MathHelper.Pi;
             Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 40f;
             _particles.Add(new PlayerParticle
             {
                Position = Center + offset,
                Velocity = -offset * 2f, // inwards
                Color = Color.Cyan,
                Size = 4,
                Life = 0.4f,
                Decay = 2f
             });
        }
        
        if (_spinTimer >= _spinDuration)
        {
            _isSpinning = false;
        }
    }
    
    private void StartDash(Enemy target)
    {
        _isDashing = true;
        _dashTarget = target;
        _dashCount = 0;
        _dashedEnemies.Clear();
        _dashedEnemies.Add(target);
    }
    
    private void UpdateDash(GameTime gameTime, EnemyManager enemyManager)
    {
         if (_dashTarget == null || _dashTarget.IsDead)
         {
             // Target lost, try to find next or stop
             FindNextDashTarget(enemyManager);
             if (_dashTarget == null) 
             {
                 _isDashing = false;
                 return;
             }
         }
         
         float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
         Vector2 dir = _dashTarget.Position - _position;
         float dist = dir.Length();
         
         if (dist < 20f) // Reached (increased hit radius slightly)
         {
             // Deal Damage
             int damage = (int)(_random.Next(_equippedWeapon.MinDamage, _equippedWeapon.MaxDamage) * 2.5f); // Was 1.5f, now 250% damage
             _dashTarget.TakeDamage(damage);
             OnAttackHit?.Invoke(_dashTarget, damage);
             _sfxSlice2?.Play(0.6f, 0.2f, 0f);
             
             // Visual Hit Impact
             _isAttacking = true; 
             _attackTimer = MathF.PI / 2; // Mid-swing visual
             
             // Chain
             _dashCount++;
             if (_dashCount < MAX_DASH_CHAIN)
             {
                 FindNextDashTarget(enemyManager);
             }
             else
             {
                 _isDashing = false;
                 _isAttacking = false; // Reset attack visual
             }
         }
         else
         {
             // Move
             dir.Normalize();
             _position += dir * _dashSpeed * dt;
             
             // Wind/Smoke Trail Effect
             if (_random.NextDouble() < 0.6) // Frequent
             {
                 _particles.Add(new PlayerParticle
                 {
                    Position = _position + new Vector2(_random.Next(0, 20), _height - 10), // Near feet/ground
                    Velocity = new Vector2(-dir.X * 20, -10), // Slight uplift
                    Color = new Color(200, 200, 200, 100), // Grayish Smoke
                    Size = _random.Next(10, 20),
                    Scale = Vector2.One, 
                    Rotation = (float)_random.NextDouble() * MathHelper.TwoPi,
                    Life = 0.8f, 
                    Decay = 1.0f,
                    IsBehind = true
                 });
                 // Add a second expanding ring? Or just simple smoke for now.
             }
         }
    }
    
    private void FindNextDashTarget(EnemyManager enemyManager)
    {
        var enemies = enemyManager.GetAllEnemies(); // Assuming method exists or we use area
        Enemy best = null;
        float minDist = 300f; // Chain range
        
        foreach(var enemy in enemies)
        {
            if (enemy.IsDead || _dashedEnemies.Contains(enemy)) continue;
            
            float d = Vector2.Distance(_position, enemy.Position);
            if (d < minDist)
            {
                minDist = d;
                best = enemy;
            }
        }
        
        _dashTarget = best;
        if (best != null) _dashedEnemies.Add(best);
        else _isDashing = false;
    }

    public void Update(GameTime gameTime, int mapWidth, int mapHeight, EnemyManager enemyManager, Vector2 joystickInput = default)
    {
        // Skill State Updates
        if (_isDashing)
        {
             UpdateDash(gameTime, enemyManager);
             return; 
        }
        if (_isSpinning)
        {
            UpdateSpin(gameTime, enemyManager);
            _speed = 150f; // Slower movement while spinning
        }
        else
        {
            _speed = 300f; // Normal speed
        }
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Input Handling
        _velocity = joystickInput;
        
        KeyboardState keyState = Keyboard.GetState();
        if (keyState.IsKeyDown(Keys.W) || keyState.IsKeyDown(Keys.Up)) _velocity.Y -= 1;
        if (keyState.IsKeyDown(Keys.S) || keyState.IsKeyDown(Keys.Down)) _velocity.Y += 1;
        if (keyState.IsKeyDown(Keys.A) || keyState.IsKeyDown(Keys.Left)) { _velocity.X -= 1; }
        if (keyState.IsKeyDown(Keys.D) || keyState.IsKeyDown(Keys.Right)) { _velocity.X += 1; }
        
        if (_velocity != Vector2.Zero)
        {
            if (_velocity.X > 0) _facingDirection = 1;
            else if (_velocity.X < 0) _facingDirection = -1;
            
            _isMoving = true;
            _velocity.Normalize();
        }
        else
        {
            _isMoving = false;
        }
        
        // Yürüyüş Sesi Kontrolü
        if (_walkSoundInstance != null)
        {
            if (_isMoving && _walkSoundInstance.State != SoundState.Playing)
            {
                _walkSoundInstance.Play();
            }
            else if (!_isMoving && _walkSoundInstance.State == SoundState.Playing)
            {
                _walkSoundInstance.Stop();
            }
        }
        
        if (_velocity != Vector2.Zero) _velocity.Normalize();
        
        _position += _velocity * _speed * deltaTime;
        _position.X = MathHelper.Clamp(_position.X, 0, mapWidth - _width);
        // Y eksenini kısıtlamıyoruz ki harita değiştirebilelim
        // _position.Y = MathHelper.Clamp(_position.Y, 0, screenHeight - _height);
        
        if (_isMoving) _animationTimer += deltaTime * 10f;
        if (_attackCooldown > 0) _attackCooldown -= deltaTime;
        
        _timeSinceLastCombat += deltaTime;
        if (_timeSinceLastCombat >= 10f && CurrentHealth < MaxHealth)
        {
            _regenTimer += deltaTime;
            if (_regenTimer >= 1.0f) 
            {
                CurrentHealth += 1;
                if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth;
                _regenTimer = 0f;
            }
        }
        else _regenTimer = 0f;
        
        if (_isAttacking)
        {
            _attackTimer += deltaTime * 15f; 
            if (_attackTimer > MathF.PI) { _isAttacking = false; _attackTimer = 0f; }
        }
        
        // --- EFEKT ÜRETİMİ ---
        
        // 1. Zırh Dumanı (ID 11)
        if (_equippedArmor != null && _equippedArmor.Id == 11)
        {
            if (_random.NextDouble() < 0.3) // Her frame %30 şans
            {
                _particles.Add(new PlayerParticle
                {
                    Position = new Vector2(_position.X + _random.Next(0, _width), _position.Y + _random.Next(10, _height)),
                    Velocity = new Vector2(_random.Next(-10, 11), -_random.Next(20, 50)),
                    Color = _random.Next(2) == 0 ? new Color(50, 0, 0, 100) : new Color(20, 20, 20, 150), // Kırmızı/Siyah
                    Size = _random.Next(4, 10),
                    Life = 1.0f,
                    Decay = 1.0f,
                    IsBehind = true // Arkaya çiz
                });
            }
        }
        
        // 2. Kılıç Alevi (ID 10) - Duruşta
        if (_equippedWeapon != null && _equippedWeapon.Id == 10 && !_isAttacking)
        {
            float handOffsetX = _facingDirection == 1 ? _width - 8 : -4;
            float handOffsetY = _height / 2 + 5;
            Vector2 weaponPos = new Vector2(_position.X + handOffsetX, _position.Y + handOffsetY);
            
            if (_random.NextDouble() < 0.2)
            {
                _particles.Add(new PlayerParticle
                {
                    Position = weaponPos + new Vector2(_random.Next(-5, 6), _random.Next(-5, 6)),
                    Velocity = new Vector2(0, -_random.Next(30, 80)),
                    Color = Color.Orange,
                    Size = _random.Next(2, 5),
                    Life = 0.5f,
                    Decay = 2.0f,
                    IsBehind = false
                });
            }
        }
        
        // Parçacıkları Güncelle
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Position += p.Velocity * deltaTime;
            p.Life -= p.Decay * deltaTime;
            if (p.Life <= 0) _particles.RemoveAt(i);
            else _particles[i] = p;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // 1. Arkadaki Parçacıklar
        foreach(var p in _particles) {
            if(p.IsBehind) 
            {
                 Vector2 origin = new Vector2(p.Size / 2f, p.Size / 2f);
                 // If Scale is zero (old particles), use Vector2.One * p.Size adjustment? 
                 // Actually Size was used as width/height. Textures are 1x1 pixel? No, _pixelTexture is 1x1.
                 // So we scale 1x1 pixel by Size * Scale.
                 // Size was width before.
                 Vector2 scale = p.Scale == Vector2.Zero ? new Vector2(p.Size) : p.Scale * p.Size;
                 
                 spriteBatch.Draw(_pixelTexture, p.Position, null, p.Color * p.Life, p.Rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }
    
        // Animasyon Değerleri
        float bobOffset = 0f;
        float armRotBack = 0f; // Arka kol
        
        // Bacak rotasyonları
        float legRotLeft = 0f;
        float legRotRight = 0f;
        
        if (_isMoving)
        {
            bobOffset = MathF.Sin(_animationTimer) * 2f;
            
            // Kollar ve Bacaklar
            legRotLeft = MathF.Sin(_animationTimer * 0.8f) * 0.6f;
            legRotRight = MathF.Sin(_animationTimer * 0.8f + MathF.PI) * 0.6f;
            
            armRotBack = MathF.Sin(_animationTimer * 0.8f) * 0.5f; // Yürürken arka kol sallanır
        }

        Vector2 bodyPos = new Vector2(_position.X, _position.Y + bobOffset);
        SpriteEffects flip = _facingDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        
        // --- ARKA KOLU ÇİZ (Eğer kılıç yoksa iki kol da sallanır, ama kılıç varsa bu boştaki kol) ---
        if (_armTexture != null)
        {
            // Omuz hizası, bedenin biraz arkasında (görsel olarak arkada, X olarak ters tarafta)
            float backShoulderX = _facingDirection == 1 ? 12 : _width - 12; 
            Vector2 backArmPos = bodyPos + new Vector2(backShoulderX, 22);
            Vector2 armOrigin = new Vector2(_armTexture.Width/2, 2);
            
            // Koyu çiz
            spriteBatch.Draw(_armTexture, backArmPos, null, Color.Gray, armRotBack * _facingDirection, armOrigin, 1f, SpriteEffects.None, 0f);
        }
        
        // --- BACAKLARI ÇİZ ---
        if (_legTexture != null)
        {
            Vector2 legOrigin = new Vector2(_legTexture.Width / 2, 2); 
            Vector2 legPosLeft = bodyPos + new Vector2(_width/2 - 6, 35);
            Vector2 legPosRight = bodyPos + new Vector2(_width/2 + 6, 35);
            
            // Sol bacak (Arka)
            spriteBatch.Draw(_legTexture, legPosLeft, null, Color.Gray, legRotLeft * _facingDirection, legOrigin, 1f, SpriteEffects.None, 0f);
            // Sağ bacak (Ön)
            spriteBatch.Draw(_legTexture, legPosRight, null, Color.White, legRotRight * _facingDirection, legOrigin, 1f, SpriteEffects.None, 0f);
        }
        
        // Gölge
        spriteBatch.Draw(_texture, new Vector2(_position.X + 4, _position.Y + _height - 5), null, new Color(0, 0, 0, 80), 0f, Vector2.Zero, new Vector2(0.8f, 0.2f), SpriteEffects.None, 0f);
        
        // Gövde
        spriteBatch.Draw(_texture, bodyPos, null, Color.White, 0f, Vector2.Zero, 1f, flip, 0f);
        
        // --- SİLAHI ve ÖN KOLU ÇİZ ---
        if (_equippedWeapon != null && _weaponTexture != null)
        {
            DrawWeapon(spriteBatch, bodyPos);
        }
        else
        {
            // Silah yoksa ön kolu boş çiz
            if (_armTexture != null)
            {
                float frontShoulderX = _facingDirection == 1 ? _width - 12 : 12;
                Vector2 frontArmPos = bodyPos + new Vector2(frontShoulderX, 22);
                Vector2 armOrigin = new Vector2(_armTexture.Width/2, 2);
                float armRotFront = _isMoving ? MathF.Sin(_animationTimer * 0.8f + MathF.PI) * 0.5f : 0f;
                
                spriteBatch.Draw(_armTexture, frontArmPos, null, Color.White, armRotFront * _facingDirection, armOrigin, 1f, SpriteEffects.None, 0f);
            }
        }
        
        // 2. Öndeki Parçacıklar
        foreach(var p in _particles) {
            if(!p.IsBehind) 
            {
                 Vector2 scale = p.Scale == Vector2.Zero ? new Vector2(p.Size) : p.Scale * p.Size;
                 spriteBatch.Draw(_pixelTexture, p.Position, null, p.Color * p.Life, p.Rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }
    }
    
    private void DrawWeapon(SpriteBatch spriteBatch, Vector2 bodyPos)
    {
        // Omuz Pozisyonu (Gövde kenarı) 
        // Genişlik 48, Orta 24, Gövde yarıçapı ~12 => Sağ omuz 36, Sol omuz 12
        float shoulderOffsetX = _facingDirection == 1 ? 36 : 12;
        float shoulderOffsetY = 26; 
        Vector2 shoulderPos = bodyPos + new Vector2(shoulderOffsetX, shoulderOffsetY);
        
        // --- KOL ROTASYONU ---
        // Silahı vücuttan uzakta tutması için kolu dışarı/aşağı doğru açıyoruz
        // 0 = Aşağı, -1.0 = Sağ-Aşağı (Sağ el için), 1.0 = Sol-Aşağı (Sol el için)
        float baseRotation = _facingDirection == 1 ? -1.0f : 1.0f; 
        
        // Saldırı animasyonu (Vurunca kol hareket etsin)
        float animationRot = 0f;
        if (_isAttacking)
        {
            float t = _attackTimer / MathF.PI; 
            float startAngle = _facingDirection == 1 ? -2.5f : 2.5f; // Yukarıdan
            float endAngle = _facingDirection == 1 ? 1.5f : -1.5f;   // Aşağıya
            baseRotation = MathHelper.Lerp(startAngle, endAngle, t);
            
            // Sweep Efekti - EKLENDİ
            if (_sweepTexture != null)
            {
                 // Sweep konumu silaha göre
                 Vector2 sweepOffset = new Vector2(MathF.Sin(baseRotation), -MathF.Cos(baseRotation)) * 36f; 
                 // Rotasyon düzeltmesi
                 float sweepRot = baseRotation + (_facingDirection == 1 ? -MathF.PI/4 : MathF.PI/4);

                 Vector2 sweepPos = shoulderPos + new Vector2(_facingDirection * 15, 0); // Omuzdan biraz önde
                 spriteBatch.Draw(_sweepTexture, sweepPos, null, Color.White * 0.8f, sweepRot, new Vector2(32,32), 2.0f, SpriteEffects.None, 0f);
            }
        }
        else if (_isMoving)
        {
            animationRot = MathF.Sin(_animationTimer) * 0.3f;
        }
        else
        {
            animationRot = MathF.Sin(_animationTimer * 0.2f) * 0.05f;
        }
        
        float finalRotation = baseRotation + animationRot;

        // --- KOLU ÇİZ ---
        Vector2 armOrigin = new Vector2(_armTexture.Width/2, 2); 
        
        if (_armTexture != null)
        {
            spriteBatch.Draw(_armTexture, shoulderPos, null, Color.White, finalRotation, armOrigin, 1f, SpriteEffects.None, 0f);
        }
        
        // --- SİLAHI ÇİZ ---
        Matrix mat = Matrix.CreateRotationZ(finalRotation);
        Vector2 armEndOffset = Vector2.Transform(new Vector2(0, 14), mat); 
        Vector2 handPos = shoulderPos + armEndOffset;
        
        // Silahın sapı (Origin) - Alt orta
        Vector2 weaponOrigin = new Vector2(_weaponTexture.Width / 2, _weaponTexture.Height - 6);
        
        // --- SİLAH ROTASYONU ---
        // Kılıç her zaman yukarı baksın (Hafif dışarı eğimli)
        // Kolun açısı ne olursa olsun kılıcı dik tutmaya çalışsın (saldırı hariç)
        float weaponRot;
        
        if (_isSpinning)
        {
             // Spin effect (Matches logic: 20f)
             weaponRot = _spinTimer * 20f; 
        }
        else if (_isAttacking)
        {
            // Saldırırken kılıç kolu takip etsin (kesme hareketi için)
            weaponRot = finalRotation;
        }
        else
        {
            // Normal duruşta kılıç yukarı baksın (0 = Yukarı/Aşağı? Texture dikey olduğu için 0 dikey)
            // Hafif dışarı eğim verelim: Sağ için +0.2, Sol için -0.2
            weaponRot = _facingDirection == 1 ? 0.2f : -0.2f;
            
            // Koşarken kılıç da hafif sallansın
            weaponRot += animationRot * 0.5f;
        }
        
        spriteBatch.Draw(_weaponTexture, handPos, null, Color.White, weaponRot, weaponOrigin, 1f, SpriteEffects.None, 0f);
    }
    
    public Item GetEquippedWeapon() => _equippedWeapon;
    public Item GetEquippedArmor() => _equippedArmor;

    public int GetMinDamage() => _equippedWeapon?.MinDamage ?? 1;
    public int GetMaxDamage() => _equippedWeapon?.MaxDamage ?? 3;
}
