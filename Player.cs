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
            // Level değişince MaxXP'yi güncelle
            MaxExperience = (long)(100 * Math.Pow(_level, 1.2));
        }
    }
    
    public int CurrentHealth { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    
    // XP Sistemi
    public long Experience { get; private set; } = 0;
    public long MaxExperience { get; private set; } = 100; 
    public const int MAX_LEVEL = 99;
    
    // Altın Sistemi
    public int Gold { get; private set; } = 0;
    
    public void GainGold(int amount)
    {
        Gold += amount;
    }
    
    public void LoseGold(int amount)
    {
        Gold -= amount;
        if (Gold < 0) Gold = 0;
    }
    
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
        int blockChance = _equippedShield.BlockChance + (_equippedShield.EnhancementLevel * 2);
        return _random.Next(100) < blockChance;
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
        _width = 40;
        _height = 60;
        
        _texture = new Texture2D(graphicsDevice, _width, _height);
        Color[] colorData = new Color[_width * _height];
        
        Color skinColor = new Color(245, 210, 180);
        Color hairColor = new Color(50, 30, 20);
        Color underwearColor = new Color(200, 200, 200); 
        
        int centerX = _width / 2;
        
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int index = y * _width + x;
                colorData[index] = Color.Transparent;
                
                // Kafa
                if (y >= 4 && y < 18)
                {
                    if (Math.Abs(x - centerX) < 9)
                    {
                        colorData[index] = skinColor;
                        if (y < 8) colorData[index] = hairColor; 
                        if (y >= 10 && y <= 12 && (Math.Abs(x - centerX) > 2 && Math.Abs(x - centerX) < 6))
                             colorData[index] = Color.White; 
                    }
                }
                
                // Gövde 
                if (y >= 18 && y < 35)
                {
                    if (Math.Abs(x - centerX) < 10)
                    {
                         colorData[index] = skinColor;
                         // Zırh
                         if (_equippedArmor != null)
                         {
                             // ID 11 (Ejderha Zırhı) için koyu kırmızı/siyah
                             if (_equippedArmor.Id == 11)
                             {
                                 colorData[index] = new Color(40, 10, 10); // Koyu zırh
                                 if (Math.Abs(x - centerX) < 3) colorData[index] = new Color(100, 20, 20); // Orta detay
                             }
                             else
                             {
                                 colorData[index] = new Color(120, 80, 50); 
                                 if (Math.Abs(x - centerX) < 2) colorData[index] = new Color(100, 60, 40); 
                             }
                         }
                    }
                    // Kollar (Sadece Sol Kol - Arka Kol)
                    // Sağ kolu (kılıç tutan) buraya çizmiyoruz, DrawWeapon'da çizeceğiz
                    // Facing right (1) ise sol kol arkada kalır, görünebilir. Facing left ise tam tersi.
                    // Basitlik için: Gövde texture'ında sadece "Diğer" kol kalsın veya ikisini de silelim, weapon tarafını dinamik çizelim.
                    // Şimdilik texture üzerinde kolların olduğu kısmı siliyoruz, dinamik kol ekleyeceğiz.
                    // (Mevcut kodda x >= 10 kısmı koldu, onu siliyoruz)
                }
                
                // Bacaklar ARTIK BURADA ÇİZİLMİYOR (Ayrı texture ve animasyon var)
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
    
    public void Update(GameTime gameTime, int screenWidth, int screenHeight)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        KeyboardState keyState = Keyboard.GetState();
        
        _velocity = Vector2.Zero;
        if (keyState.IsKeyDown(Keys.W) || keyState.IsKeyDown(Keys.Up)) _velocity.Y -= 1;
        if (keyState.IsKeyDown(Keys.S) || keyState.IsKeyDown(Keys.Down)) _velocity.Y += 1;
        if (keyState.IsKeyDown(Keys.A) || keyState.IsKeyDown(Keys.Left)) { _velocity.X -= 1; _facingDirection = -1; }
        if (keyState.IsKeyDown(Keys.D) || keyState.IsKeyDown(Keys.Right)) { _velocity.X += 1; _facingDirection = 1; }
        
        _isMoving = _velocity != Vector2.Zero;
        
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
        _position.X = MathHelper.Clamp(_position.X, 0, screenWidth - _width);
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
                spriteBatch.Draw(_pixelTexture, new Rectangle((int)p.Position.X, (int)p.Position.Y, (int)p.Size, (int)p.Size), p.Color * p.Life);
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
                spriteBatch.Draw(_pixelTexture, new Rectangle((int)p.Position.X, (int)p.Position.Y, (int)p.Size, (int)p.Size), p.Color * p.Life);
        }
    }
    
    private void DrawWeapon(SpriteBatch spriteBatch, Vector2 bodyPos)
    {
        // Omuz Pozisyonu (Gövdeye göre ön omuz)
        float shoulderOffsetX = _facingDirection == 1 ? _width - 10 : 10;
        float shoulderOffsetY = 22; 
        Vector2 shoulderPos = bodyPos + new Vector2(shoulderOffsetX, shoulderOffsetY);
        
        // Temel Rotasyon (Silahı öne doğru tutsun)
        float baseRotation = _facingDirection == 1 ? -0.5f : 0.5f; 
        
        // Saldırı animasyonu
        float animationRot = 0f;
        
        if (_isAttacking)
        {
            // Vurma hareketi (Yukarıdan aşağıya veya savurma)
            // Timer 0 -> PI. Sinus kullanırsak 0 -> 1 -> 0 olur.
            // Bize savurma lazım: -Arkadan -> +Öne
            
            // Cosinüs: 1 -> -1 (PI süresince)
            float t = _attackTimer / MathF.PI; // 0.0 -> 1.0
            
            // Swing: Başlangıç açısı -> Bitiş açısı
            // Facing 1: -2.5 (Geri-Yukarı) -> 1.5 (İleri-Aşağı)
            float startAngle = -2.5f;
            float endAngle = 1.5f;
            
            if (_facingDirection == -1) // Ters
            {
                startAngle = 2.5f;
                endAngle = -1.5f;
            }
            
            // Lerp
            baseRotation = MathHelper.Lerp(startAngle, endAngle, t);
            
            // Sweep Efekti
            if (_sweepTexture != null)
            {
                 // Sweep konumu silaha göre
                 Vector2 sweepOffset = new Vector2(MathF.Sin(baseRotation), -MathF.Cos(baseRotation)) * 30f; 
                 // Rotasyon düzeltmesi
                 float sweepRot = baseRotation + (_facingDirection == 1 ? -MathF.PI/4 : MathF.PI/4);

                 Vector2 sweepPos = shoulderPos + new Vector2(_facingDirection * 10, 0); // Omuzdan biraz önde
                 spriteBatch.Draw(_sweepTexture, sweepPos, null, Color.White * 0.8f, sweepRot, new Vector2(32,32), 2.5f, SpriteEffects.None, 0f);
            }
        }
        else if (_isMoving)
        {
            // Koşarken sallasın
            animationRot = MathF.Sin(_animationTimer) * 0.3f;
        }
        else
        {
            // Idle
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
        // Silah kolun ucunda
        // Kol uzunluğu 16px (Texture yüksekliği)
        
        Matrix mat = Matrix.CreateRotationZ(finalRotation);
        Vector2 armEndOffset = Vector2.Transform(new Vector2(0, 14), mat); // Kolun ucu
             
        Vector2 handPos = shoulderPos + armEndOffset;
        
        // Silahın sapı (Origin)
        Vector2 weaponOrigin = new Vector2(_weaponTexture.Width / 2, _weaponTexture.Height - 6);
        
        // Silah rotasyonu kolla aynı + biraz açı (kılıç dik değil, uzantısı gibi)
        float weaponRot = finalRotation + (_facingDirection == 1 ? -0.2f : 0.2f);
        
        spriteBatch.Draw(_weaponTexture, handPos, null, Color.White, weaponRot, weaponOrigin, 1f, SpriteEffects.None, 0f);
    }
    
    public Item GetEquippedWeapon() => _equippedWeapon;
    public Item GetEquippedArmor() => _equippedArmor;
}
