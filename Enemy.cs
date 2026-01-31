using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace EternalJourney;

public enum EnemyType
{
    Demon,  // Güçlü, Tridentli
    Goblin,  // Zayıf, Hızlı
    Spider,  // Orta, Hızlı
    Skeleton // Yeni: Sprite Animasyonlu
}

public enum EnemyState
{
    Idle,
    Chasing,
    Returning,
    Dead
}

public class Enemy
{
    public EnemyType Type { get; private set; }
    
    // Pozisyon ve hareket
    public Vector2 Position { get; private set; }
    public Vector2 SpawnPosition { get; private set; }
    private Vector2 _velocity;
    private float _speed;
    
    // Boyut
    private int _width;
    private int _height;
    
    // Sağlık
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    
    // İstatistikler
    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }
    
    // AI
    public EnemyState State { get; private set; } = EnemyState.Idle;
    private float _aggroRadius;
    private float _leashRadius = 500f;
    private float _attackRadius;
    private float _attackCooldown;
    private float _attackTimer = 0f;
    
    // Devriye (Wander)
    private Vector2 _idleTarget;
    private float _idleWaitTimer = 0f;
    private float _wanderRadius = 150f;
    private bool _isWalkingToTarget = false;
    
    // Görsel
    private Texture2D _texture;
    private Texture2D _weaponTexture;
    private float _animationTimer = 0f;
    private int _facingDirection = 1;
    
    // Saldırı animasyonu
    private bool _isAttacking = false;
    private float _attackAnimTimer = 0f;
    
    // Hasar efekti
    private float _damageFlashTimer = 0f;
    private bool _showDamageFlash = false;
    
    // Smoke efekti (Sadece Demon)
    private float _smokeTimer = 0f;
    private Random _random = new Random();
    
    // SFX (Static for shared access)
    public static SoundEffect SfxDemonIdle;
    public static SoundEffect SfxGoblinIdle;
    public static SoundEffect SfxGoblinDeath;
    // Skeleton Assets
    public static Texture2D TexSkeletonIdle;
    public static Texture2D TexSkeletonWalk;
    public static Texture2D TexSkeletonAttack;
    public static Texture2D TexSkeletonDeath;
    public static Texture2D TexSkeletonHit;
    public static Texture2D TexSkeletonShield;
    
    public static void LoadSkeletonContent(GraphicsDevice gd)
    {
        try {
            using (var stream = System.IO.File.OpenRead("Content/Skeleton/Idle.png")) TexSkeletonIdle = Texture2D.FromStream(gd, stream);
            using (var stream = System.IO.File.OpenRead("Content/Skeleton/Walk.png")) TexSkeletonWalk = Texture2D.FromStream(gd, stream);
            using (var stream = System.IO.File.OpenRead("Content/Skeleton/Attack.png")) TexSkeletonAttack = Texture2D.FromStream(gd, stream);
            using (var stream = System.IO.File.OpenRead("Content/Skeleton/Death.png")) TexSkeletonDeath = Texture2D.FromStream(gd, stream);
            using (var stream = System.IO.File.OpenRead("Content/Skeleton/Take Hit.png")) TexSkeletonHit = Texture2D.FromStream(gd, stream);
            using (var stream = System.IO.File.OpenRead("Content/Skeleton/Shield.png")) TexSkeletonShield = Texture2D.FromStream(gd, stream);
        } catch (Exception e) {
            System.Diagnostics.Debug.WriteLine("Skeleton texture load error: " + e.Message);
        }
    }
    
    // Global ses kontrolü (idle sesleri için)
    private static float _globalIdleSoundTimer = 0f;
    private const float MIN_IDLE_SOUND_INTERVAL = 4.0f; // En az 4 saniye ara
    private static double _lastDeathSoundTime = 0; // Ölüm sesi için global cooldown
    
    public event Action<int> OnAttackPlayer;
    
    public Rectangle Bounds => new Rectangle(
        (int)Position.X - _width / 2, 
        (int)Position.Y - _height / 2, 
        _width, 
        _height
    );
    
    public Enemy(GraphicsDevice graphicsDevice, Vector2 spawnPosition, EnemyType type)
    {
        Position = spawnPosition;
        SpawnPosition = spawnPosition;
        Type = type;
        
        SetupStats();
        CreateTexture(graphicsDevice);
        if (Type == EnemyType.Demon)
            CreateTridentTexture(graphicsDevice);
        else if (Type == EnemyType.Spider)
        {
            // Silah texture'ı yok
             _weaponTexture = null;
        }
        else 
            CreateDaggerTexture(graphicsDevice);
    }
    
    private void SetupStats()
    {
        if (Type == EnemyType.Demon)
        {
            // Şeytan - Çok Güçlü
            _width = 44; // Biraz daha büyük
            _height = 54;
            MaxHealth = 1250; // 5x Buff (Pre: 250)
            CurrentHealth = MaxHealth;
            MinDamage = 15;
            MaxDamage = 25;
            _speed = 110f; // Biraz yavaş
            _aggroRadius = 250f;
            _attackRadius = 60f;
            _attackCooldown = 2.5f;
            _attackCooldown = 2.5f;
        }
        else if (Type == EnemyType.Spider)
        {
            // Örümcek - Hızlı ve Zehirli (Zehir logic yok ama hızlı)
            _width = 48;
            _height = 32; // Basık
            MaxHealth = 120;
            CurrentHealth = MaxHealth;
            MinDamage = 5;
            MaxDamage = 10;
            _speed = 140f; 
            _aggroRadius = 200f;
            _attackRadius = 45f;
            _attackCooldown = 1.2f;
        }
        else if (Type == EnemyType.Goblin)
        {
            // Goblin - Zayıf ve Hızlı
            _width = 32;
            _height = 36;
            MaxHealth = 40; // Düşük can
            CurrentHealth = MaxHealth;
            MinDamage = 1;
            MaxDamage = 3;
            _speed = 180f; // Hızlı
            _aggroRadius = 180f;
            _attackRadius = 40f;
            _attackCooldown = 1.0f; // Hızlı saldırı
        }
        else if (Type == EnemyType.Skeleton)
        {
            // Skeleton - Yüksek hasar
            // Hitbox boyutları (Sprite çizimi 150px olabilir ama fiziksel boyut küçük)
            _width = 50; 
            _height = 80;
            MaxHealth = 200;
            CurrentHealth = MaxHealth;
            MinDamage = 10;
            MaxDamage = 20;
            _speed = 90f; // Yavaş ama tehlikeli
            _aggroRadius = 220f;
            _attackRadius = 70f; // Silah menzili
            _attackCooldown = 1.8f;
        }
    }
    
    private void CreateTexture(GraphicsDevice graphicsDevice)
    {
        _texture = new Texture2D(graphicsDevice, _width, _height);
        Color[] colors = new Color[_width * _height];
        
        int centerX = _width / 2;
        
        if (Type == EnemyType.Demon)
        {
            // Şeytan görünümü - kırmızı/koyu renk
            Color bodyColor = new Color(180, 50, 50);
            Color darkColor = new Color(100, 30, 30);
            Color hornColor = new Color(60, 30, 30);
            Color eyeColor = new Color(255, 200, 0);
            
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int i = y * _width + x;
                    colors[i] = Color.Transparent;
                    
                    // Boynuzlar (üst kısım)
                    if (y < 12)
                    {
                        // Sol boynuz
                        if (x >= 5 && x <= 12)
                        {
                            int hornY = 12 - y;
                            int expectedX = 8 - hornY / 2;
                            if (Math.Abs(x - expectedX) < 3 - hornY / 4)
                            {
                                colors[i] = hornColor;
                            }
                        }
                        // Sağ boynuz
                        if (x >= _width - 12 && x <= _width - 5)
                        {
                            int hornY = 12 - y;
                            int expectedX = _width - 8 + hornY / 2;
                            if (Math.Abs(x - expectedX) < 3 - hornY / 4)
                            {
                                colors[i] = hornColor;
                            }
                        }
                    }
                    
                    // Kafa (yuvarlak)
                    if (y >= 8 && y < 25)
                    {
                        int headCenterY = 16;
                        int headRadius = 10;
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, headCenterY));
                        
                        if (dist < headRadius)
                        {
                            float gradient = 1f - dist / headRadius * 0.3f;
                            colors[i] = new Color(
                                (int)(bodyColor.R * gradient),
                                (int)(bodyColor.G * gradient),
                                (int)(bodyColor.B * gradient)
                            );
                            
                            // Gözler
                            if (y >= 14 && y <= 18)
                            {
                                // Sol göz
                                if (x >= centerX - 6 && x <= centerX - 3)
                                {
                                    colors[i] = eyeColor;
                                }
                                // Sağ göz
                                if (x >= centerX + 3 && x <= centerX + 6)
                                {
                                    colors[i] = eyeColor;
                                }
                            }
                        }
                    }
                    
                    // Gövde
                    if (y >= 24 && y < 40)
                    {
                        int bodyWidth = 14 - (y - 24) / 4;
                        if (Math.Abs(x - centerX) < bodyWidth)
                        {
                            float gradient = 1f - (float)Math.Abs(x - centerX) / bodyWidth * 0.3f;
                            colors[i] = new Color(
                                (int)(darkColor.R + 30 * gradient),
                                (int)(darkColor.G + 10 * gradient),
                                (int)(darkColor.B + 10 * gradient)
                            );
                        }
                    }
                    
                    // Bacaklar/Ayaklar
                    if (y >= 40)
                    {
                        // Sol ayak
                        if (x >= centerX - 10 && x <= centerX - 4)
                        {
                            colors[i] = darkColor;
                        }
                        // Sağ ayak
                        if (x >= centerX + 4 && x <= centerX + 10)
                        {
                            colors[i] = darkColor;
                        }
                    }
                }
            }
        }
        else if (Type == EnemyType.Spider)
        {
            // --- SPIDER TEXTURE (Gelişmiş) ---
            Color abdomenColor = new Color(20, 15, 25); // Koyu Mor/Siyah Gövde
            Color thoraxColor = new Color(30, 25, 35); // Kafa/Göğüs
            Color legColor = new Color(10, 10, 15); // Bacaklar
            Color eyeColor = new Color(200, 20, 20); // Gözler
            Color patternColor = new Color(60, 20, 60); // Sırt deseni
            
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int i = y * _width + x;
                    colors[i] = Color.Transparent;
                    
                    float dx = x - centerX;
                    float dy = y - _height / 2;
                    
                    // 1. ABDOMEN (Arka Gövde) - Büyük ve şişkin
                    // Hafif aşağıda olsun
                    float abY = dy - 4; 
                    if ((dx*dx)/100 + (abY*abY)/120 < 1.0f) 
                    {
                        colors[i] = abdomenColor;
                        
                        // Sırtta "Kum saati" veya haç benzeri desen
                        if (Math.Abs(dx) < 3 && Math.Abs(abY) < 6) colors[i] = patternColor;
                    }
                    
                    // 2. THORAX (Kafa/Göğüs) - Daha küçük, önde
                    float thY = dy + 8;
                    if ((dx*dx)/36 + (thY*thY)/36 < 1.0f)
                    {
                        colors[i] = thoraxColor;
                        
                        // Gözler (Çoklu)
                        if (thY > 1 && thY < 3)
                        {
                            if (Math.Abs(dx) == 2 || dx == 0) colors[i] = eyeColor;
                        }
                    }
                    
                    // 3. MANDIBLES (Kıskaçlar) - En önde
                    if (y >= _height/2 + 13 && y < _height/2 + 16)
                    {
                        if (Math.Abs(dx) >= 1 && Math.Abs(dx) <= 2) colors[i] = new Color(80, 20, 20);
                    }

                    // 4. BACAKLAR (8 Adet, Eklemli)
                    // Her iki tarafta 4'er bacak. Thorax'tan çıkmalı.
                    
                    // Sol Bacaklar
                    if (dx < -4)
                    {
                        // Basit matematiksel eğrilerle bacak çizimi
                        // Bacak 1 (En Ön)
                        if (Math.Abs(y - (_height/2 + 10) - (dx + 5)*0.5f) < 1.5f && dx > -14) colors[i] = legColor;
                        // Bacak 2
                        if (Math.Abs(y - (_height/2 + 6) - (dx + 5)*0.2f) < 1.5f && dx > -16) colors[i] = legColor;
                        // Bacak 3
                        if (Math.Abs(y - (_height/2 + 2) - (dx + 5)*-0.2f) < 1.5f && dx > -16) colors[i] = legColor;
                        // Bacak 4 (En Arka)
                        if (Math.Abs(y - (_height/2 - 2) - (dx + 5)*-0.8f) < 1.5f && dx > -14) colors[i] = legColor;
                        
                        // Eklem yerleri (Dizler) - koyu noktalar
                        if (dx == -12 && (y % 4 == 0)) colors[i] = Color.Black; 
                    }
                    
                    // Sağ Bacaklar (Simetrik)
                    if (dx > 4)
                    {
                        // Bacak 1 (En Ön)
                        if (Math.Abs(y - (_height/2 + 10) - (-dx + 5)*0.5f) < 1.5f && dx < 14) colors[i] = legColor;
                        // Bacak 2
                        if (Math.Abs(y - (_height/2 + 6) - (-dx + 5)*0.2f) < 1.5f && dx < 16) colors[i] = legColor;
                        // Bacak 3
                        if (Math.Abs(y - (_height/2 + 2) - (-dx + 5)*-0.2f) < 1.5f && dx < 16) colors[i] = legColor;
                        // Bacak 4 (En Arka)
                        if (Math.Abs(y - (_height/2 - 2) - (-dx + 5)*-0.8f) < 1.5f && dx < 14) colors[i] = legColor;
                        
                         // Eklem yerleri
                        if (dx == 12 && (y % 4 == 0)) colors[i] = Color.Black;
                    }
                }
            }
        }
        else
        {
            // --- GOBLIN TEXTURE (Yeşil, Küçük) ---
            Color skinColor = new Color(60, 160, 40); // Yeşil
            Color clothColor = new Color(100, 70, 40); // Kahverengi kıyafet
            
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int i = y * _width + x;
                    colors[i] = Color.Transparent;
                    
                    // Kulaklar (Sivri, yana doğru)
                    if (y >= 8 && y <= 14)
                    {
                        if (Math.Abs(x - centerX) > 8 && Math.Abs(x - centerX) < 16)
                             colors[i] = skinColor;
                    }

                    // Kafa
                    if (y >= 4 && y < 20)
                    {
                         if(Math.Abs(x-centerX) < 10)
                             colors[i] = skinColor;
                         
                         // Gözler (Kırmızımsı)
                         if (y >= 10 && y <= 12 && (Math.Abs(x-centerX) > 3 && Math.Abs(x-centerX) < 6))
                             colors[i] = new Color(200, 50, 50);
                    }
                    
                    // Gövde (Kıyafetli)
                    if (y >= 20 && y < 30)
                    {
                        if(Math.Abs(x-centerX) < 8)
                            colors[i] = clothColor;
                    }
                    
                    // Bacaklar
                    if (y >= 30)
                    {
                         if(Math.Abs(x-centerX) < 8 && Math.Abs(x-centerX) > 1)
                             colors[i] = skinColor;
                    }
                }
            }
        }
        
        _texture.SetData(colors);
    }
    
    private void CreateTridentTexture(GraphicsDevice graphicsDevice)
    {
        // Demon Silahı
        int w = 14; int h = 55;
        _weaponTexture = new Texture2D(graphicsDevice, w, h);
        Color[] c = new Color[w*h];
        Color metal = new Color(50, 50, 70);
        Color tip = new Color(255, 50, 50); // Kanlı uç
        
        for(int i=0; i<w*h; i++) 
        {
            int y = i/w; int x = i%w;
            if (x == w/2 || (y < 20 && (x==2 || x==w-3))) {
                c[i] = metal;
                if (y < 5) c[i] = tip;
            }
            else if (y >= 15 && y <= 17 && x > 2 && x < w-3) c[i] = metal;
            else c[i] = Color.Transparent;
        }
        _weaponTexture.SetData(c);
    }
    
    private void CreateDaggerTexture(GraphicsDevice graphicsDevice)
    {
        // Goblin Silahı
        int w = 8; int h = 20;
        _weaponTexture = new Texture2D(graphicsDevice, w, h);
        Color[] c = new Color[w*h];
        Color blade = new Color(200, 200, 200);
        
        for(int i=0; i<w*h; i++)
        {
            int y = i/w; int x = i%w;
            if (x >= 2 && x <= 5 && y < 14) c[i] = blade;
            else if (y >= 14 && x >= 3 && x <= 4) c[i] = new Color(80, 50, 20); // Sap
            else c[i] = Color.Transparent;
        }
        _weaponTexture.SetData(c);
    }
    
    public void ApplySeparationForce(Vector2 force)
    {
        Position += force;
    }

    public void Update(GameTime gameTime, Vector2 playerPosition, Rectangle playerBounds)
    {
        if (IsDead) return; // Ölü ise güncelleme yapma

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        _smokeTimer += deltaTime;
        

        
        // Static timer azalt
        if (_globalIdleSoundTimer > 0) _globalIdleSoundTimer -= deltaTime;
        
        if (_showDamageFlash)
        {
            _damageFlashTimer -= deltaTime;
            if (_damageFlashTimer <= 0) _showDamageFlash = false;
        }
        
        if (_isAttacking)
        {
            _attackAnimTimer += deltaTime * 10f;
            if (_attackAnimTimer > MathF.PI)
            {
                _isAttacking = false;
                _attackAnimTimer = 0f;
            }
        }
        
        float distanceToPlayer = Vector2.Distance(Position, playerPosition);
        float distanceToSpawn = Vector2.Distance(Position, SpawnPosition);
        
        if (_attackTimer > 0) _attackTimer -= deltaTime;
        
        switch (State)
        {
            case EnemyState.Idle:
                // Oyuncu menzile girdi mi?
                if (distanceToPlayer < _aggroRadius) 
                {
                    State = EnemyState.Chasing;
                    _isWalkingToTarget = false;
                }
                else
                {
                    // Devriye Mantığı (Wander)
                    if (_isWalkingToTarget)
                    {
                        // Hedefe git
                        float distToTarget = Vector2.Distance(Position, _idleTarget);
                        if (distToTarget < 5f)
                        {
                            // Hedefe vardık, bekle
                            _isWalkingToTarget = false;
                            _idleWaitTimer = 1f + (float)_random.NextDouble() * 2f; // 1-3 sn bekle
                        }
                        else
                        {
                            // Yürü
                            Vector2 dir = _idleTarget - Position;
                            dir.Normalize();
                            _velocity = dir * (_speed * 0.4f); // Yavaş yürü
                            Position += _velocity * deltaTime;
                            _facingDirection = dir.X > 0 ? 1 : -1;
                            _animationTimer += deltaTime * 3f; // Yavaş animasyon
                        }
                    }
                    else
                    {
                        // Bekleme
                        _idleWaitTimer -= deltaTime;
                        if (_idleWaitTimer <= 0)
                        {
                            // Yeni hedef seç (Spawn noktası etrafında)
                            float angle = (float)(_random.NextDouble() * Math.PI * 2);
                            float dist = (float)(_random.NextDouble() * _wanderRadius);
                            _idleTarget = SpawnPosition + new Vector2(MathF.Cos(angle) * dist, MathF.Sin(angle) * dist);
                            _isWalkingToTarget = true;
                        }
                        // Dururken de hafif animasyon olsun (nefes alma)
                        _animationTimer += deltaTime * 1f;
                        
                        // IDLE SOUNDS (Global Timer Kontrollü)
                        if (_globalIdleSoundTimer <= 0)
                        {
                            bool playedSound = false;
                            
                            if (Type == EnemyType.Demon && SfxDemonIdle != null && _random.NextDouble() < 0.002) // Daha da nadir
                            {
                                float distToPlayer = Vector2.Distance(Position, playerPosition); // Uzaksa çalma
                                if (distToPlayer < 800) 
                                {
                                    // Mesafeye göre ses kısıklığı
                                    float volume = Math.Clamp(1f - (distToPlayer / 800f), 0.1f, 0.4f);
                                    SfxDemonIdle.Play(volume, -0.2f, 0f);
                                    playedSound = true;
                                }
                            }
                            else if (Type == EnemyType.Goblin && SfxGoblinIdle != null && _random.NextDouble() < 0.005)
                            {
                                float distToPlayer = Vector2.Distance(Position, playerPosition);
                                if (distToPlayer < 800)
                                {
                                    float volume = Math.Clamp(1f - (distToPlayer / 800f), 0.1f, 0.3f);
                                    SfxGoblinIdle.Play(volume, 0.2f, 0f);
                                    playedSound = true;
                                }
                            }
                            
                            if (playedSound)
                            {
                                _globalIdleSoundTimer = MIN_IDLE_SOUND_INTERVAL + (float)_random.NextDouble() * 2f; // 4-6 sn bekle
                            }
                        }
                    }
                }
                break;
                
            case EnemyState.Chasing:
                if (distanceToSpawn > _leashRadius) State = EnemyState.Returning;
                else if (distanceToPlayer < _attackRadius)
                {
                    _velocity = Vector2.Zero;
                    if (_attackTimer <= 0 && !_isAttacking) PerformAttack();
                }
                else
                {
                    Vector2 direction = playerPosition - Position;
                    if (direction != Vector2.Zero)
                    {
                        direction.Normalize();
                        _velocity = direction * _speed;
                        _facingDirection = direction.X > 0 ? 1 : -1;
                    }
                    Position += _velocity * deltaTime;
                }
                _animationTimer += deltaTime * (Type == EnemyType.Goblin ? 8f : 5f);
                break;
                
            case EnemyState.Returning:
                if (distanceToSpawn < 10f)
                {
                    Position = SpawnPosition;
                    State = EnemyState.Idle;
                    CurrentHealth = MaxHealth;
                }
                else
                {
                    Vector2 direction = SpawnPosition - Position;
                    if (direction != Vector2.Zero)
                    {
                        direction.Normalize();
                        _velocity = direction * _speed * 1.5f;
                        _facingDirection = direction.X > 0 ? 1 : -1;
                    }
                    Position += _velocity * deltaTime;
                }
                _animationTimer += deltaTime * 3f;
                break;
        }
    }
    
    private void PerformAttack()
    {
        _isAttacking = true;
        _attackAnimTimer = 0f;
        _attackTimer = _attackCooldown;
        int damage = _random.Next(MinDamage, MaxDamage + 1);
        OnAttackPlayer?.Invoke(damage);
    }
    
    private double _deathTime = 0;

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        
        CurrentHealth -= damage;
        _showDamageFlash = true;
        _damageFlashTimer = 0.15f;
        
        if (CurrentHealth <= 0) 
        {
            CurrentHealth = 0;
            _deathTime = Game1.TotalTime;
            
            // Death Sound
            if (Type == EnemyType.Goblin && SfxGoblinDeath != null)
            {
                if (Game1.TotalTime - _lastDeathSoundTime > 0.1) // 100ms cooldown
                {
                    SfxGoblinDeath.Play(0.5f, 0f, 0f);
                    _lastDeathSoundTime = Game1.TotalTime;
                }
            }
        }
    }
    
    public bool ShouldEmitSmoke()
    {
        if (Type == EnemyType.Demon && State == EnemyState.Chasing && _smokeTimer > 0.1f)
        {
            _smokeTimer = 0f;
            return true;
        }
        return false;
    }
    
    public bool IsAttacking() => _isAttacking;

    public void Draw(SpriteBatch spriteBatch)
    {
        // Ölüleri normalde çizme, ama Skeletonun animasyonu var
        if (IsDead && Type != EnemyType.Skeleton) return; 
        
        // Skeleton öldükten bir süre sonra (ceset kaybolsun mu? Şimdilik kalsın veya yavaşça sönsün)
        if (IsDead && Type == EnemyType.Skeleton)
        {
            float timeDead = (float)(Game1.TotalTime - _deathTime);
            if (timeDead > 5.0f) return; // 5 saniye sonra çizme
        }
        
        float offsetY = MathF.Sin(_animationTimer) * 2f;
        Vector2 drawPos = new Vector2(Position.X - _width / 2, Position.Y - _height / 2 + offsetY);
        
        // Gölge
        spriteBatch.Draw(
            _texture,
            new Vector2(drawPos.X + 3, Position.Y + _height / 2 - 5),
            null,
            new Color(0, 0, 0, 60),
            0f,
            Vector2.Zero,
            new Vector2(1f, 0.2f),
            SpriteEffects.None,
            0f
        );
        
        Color tint = _showDamageFlash ? Color.Red : Color.White;
        SpriteEffects flip = _facingDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        
        if (Type == EnemyType.Skeleton)
        {
             // Position (Merkez) gönderiyoruz, çünkü DrawSkeleton origin'i merkeze ayarlıyor
             DrawSkeleton(spriteBatch, Position, flip);
             return; 
        }
        
        spriteBatch.Draw(_texture, drawPos, null, tint, 0f, Vector2.Zero, 1f, flip, 0f);
        
        if (_weaponTexture != null)
            DrawWeapon(spriteBatch, drawPos, flip);
    }
    
    private void DrawWeapon(SpriteBatch spriteBatch, Vector2 enemyPos, SpriteEffects flip)
    {
        float handOffsetX = _facingDirection == 1 ? _width - 10 : -5;
        float handOffsetY = _height / 2;
        
        Vector2 weaponPos = new Vector2(enemyPos.X + handOffsetX, enemyPos.Y + handOffsetY);
        
        float baseRotation = _facingDirection == 1 ? 0.2f : -0.2f;
        float attackRotation = 0f;
        
        if (_isAttacking)
            attackRotation = MathF.Sin(_attackAnimTimer) * 1.5f * _facingDirection;
        
        Vector2 origin = new Vector2(_weaponTexture.Width / 2, _weaponTexture.Height - 5);
        
        spriteBatch.Draw(_weaponTexture, weaponPos, null, Color.White, baseRotation + attackRotation, origin, 1f, flip, 0f);
    }

    public void DrawSkeleton(SpriteBatch spriteBatch, Vector2 position, SpriteEffects flip)
    {
        Texture2D textureToDraw = TexSkeletonIdle;
        int frameCount = 4; // Varsayılan 4 frame
        
        // State'e göre texture seçimi
        if (IsDead) 
        {
            textureToDraw = TexSkeletonDeath;
            frameCount = 4; // Texture boyutuna göre değişebilir ama 4 varsayalım
        }
        else if (_isAttacking) 
        {
            textureToDraw = TexSkeletonAttack;
            frameCount = 8;
        }
        else if (State == EnemyState.Chasing || State == EnemyState.Returning || _isWalkingToTarget) 
        {
            textureToDraw = TexSkeletonWalk;
            frameCount = 4;
        }
        else if (_showDamageFlash)
        {
            textureToDraw = TexSkeletonHit;
            frameCount = 4;
        }
        
        if (textureToDraw == null) return;
        
        // Frame Hesabı
        int frameWidth = textureToDraw.Width / frameCount;
        // int frameHeight = 150; // Unused 
        
        // Eğer texture çok büyükse (örn. birden fazla satır varsa) sadece ilk satırı (150px) alıyoruz
        if (frameWidth > 150) frameWidth = 150; 
        
        // Animasyon hızı frame sayısına göre
        float speed = 8f; // 8 fps
        int currentFrame = (int)(Game1.TotalTime * speed) % frameCount;
        
        // Eğer saldırıyorsa özel timer kullan
        if (_isAttacking)
        {
            float n = _attackAnimTimer / MathF.PI; // 0 to 1
            if (n > 1) n = 1;
            currentFrame = (int)(n * (frameCount - 1));
        }
        else if (IsDead)
        {
            // Ölüm animasyonu: Oynat ve son karede dur
            float timeDead = (float)(Game1.TotalTime - _deathTime);
            currentFrame = (int)(timeDead * 8f); // 8 FPS hızında oynat
            if (currentFrame >= frameCount) currentFrame = frameCount - 1;
        }
        
        // CROP: Ghost leg ve artifactleri temizlemek için source rect'i daraltıyoruz
        // Sol sağdan 20px, alttan 30px (eğer 150 yükseklikse 120'ye düşür)
        int cleanWidth = frameWidth - 40; 
        int cleanHeight = 120; // 150px içinde sadece üst 120px'i al
        
        // Source rect'i ortadan al
        Rectangle sourceRect = new Rectangle(
            currentFrame * frameWidth + 20, // Soldan 20px içerden başla
            0, // Üstten başla
            cleanWidth, 
            cleanHeight
        );
        
        // Origin'i yeni boyutlara göre ayarla
        Vector2 origin = new Vector2(cleanWidth / 2f, cleanHeight / 2f); 
        
        Color tint = _showDamageFlash ? Color.Red : Color.White;
        
        spriteBatch.Draw(textureToDraw, position + new Vector2(0, 5), sourceRect, tint, 0f, origin, 1.5f, flip, 0f);
    }

    public float GetHealthPercent() => (float)CurrentHealth / MaxHealth;
    public Vector2 GetHealthBarPosition() => new Vector2(Position.X - _width / 2, Position.Y - _height / 2 - 8);
    public int GetWidth() => _width;
}

public class EnemyGroup
{
    public List<Enemy> Enemies { get; private set; } = new List<Enemy>();
    public Vector2 SpawnCenter { get; private set; }
    public bool IsWiped => Enemies.TrueForAll(e => e.IsDead);
    
    private float _respawnTimer = 0f;
    private float _respawnDelay = 10f;
    private bool _waitingRespawn = false;
    private GraphicsDevice _graphicsDevice;
    private EnemyType _type;
    private Texture2D _circleTexture; // Respawn göstergesi için
    
    public event Action<int> OnEnemyAttackPlayer;
    
    public EnemyGroup(GraphicsDevice graphicsDevice, Vector2 spawnCenter, EnemyType type, int count = 3)
    {
        _graphicsDevice = graphicsDevice;
        SpawnCenter = spawnCenter;
        _type = type;
        
        CreateCircleTexture();
        SpawnEnemies(count);
    }
    
    private void CreateCircleTexture()
    {
        int size = 64;
        _circleTexture = new Texture2D(_graphicsDevice, size, size);
        Color[] data = new Color[size*size];
        float center = size / 2f;
        for(int i=0; i<data.Length; i++)
        {
            int x = i % size; int y = i / size;
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
            if (Math.Abs(dist - (center - 2)) < 2f) 
                data[i] = new Color(255, 255, 255, 100);
            else 
                data[i] = Color.Transparent;
        }
        _circleTexture.SetData(data);
    }
    
    private void SpawnEnemies(int count)
    {
        Enemies.Clear();
        Random rand = new Random();
        for (int i = 0; i < count; i++)
        {
            float angle = (float)(i * Math.PI * 2 / count);
            float distance = 30f + rand.Next(20);
            Vector2 offset = new Vector2(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance);
            
            Enemy enemy = new Enemy(_graphicsDevice, SpawnCenter + offset, _type);
            enemy.OnAttackPlayer += (damage) => OnEnemyAttackPlayer?.Invoke(damage);
            Enemies.Add(enemy);
        }
        _waitingRespawn = false;
    }
    
    public void Update(GameTime gameTime, Vector2 playerPosition, Rectangle playerBounds)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        if (IsWiped)
        {
            if (!_waitingRespawn)
            {
                _waitingRespawn = true;
                _respawnTimer = _respawnDelay;
            }
            _respawnTimer -= deltaTime;
            if (_respawnTimer <= 0) SpawnEnemies(3);
        }
        else
        {
            // Collision Avoidance (İç içe girmeyi engelleme)
            for (int i = 0; i < Enemies.Count; i++)
            {
                var e1 = Enemies[i];
                if (e1.IsDead) continue;
                
                Vector2 separation = Vector2.Zero;
                int neighbors = 0;
                
                for (int j = 0; j < Enemies.Count; j++)
                {
                    if (i == j) continue;
                    var e2 = Enemies[j];
                    if (e2.IsDead) continue;
                    
                    float dist = Vector2.Distance(e1.Position, e2.Position);
                    if (dist < 40f && dist > 0) // Çok yakınsa
                    {
                        Vector2 push = e1.Position - e2.Position;
                        push.Normalize();
                        separation += push / dist; // Yakın olan daha çok iter
                        neighbors++;
                    }
                }
                
                if (neighbors > 0)
                {
                    e1.ApplySeparationForce(separation * 300f * deltaTime); // İtme gücü
                }
                
                e1.Update(gameTime, playerPosition, playerBounds);
            }
        }
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        // Respawn indikatörü
        if (IsWiped && _waitingRespawn)
        {
            float progress = 1f - (_respawnTimer / _respawnDelay);
            float scale = 0.5f + (progress * 0.5f); // Büyüyen daire efekti
            float alpha = 0.5f + (MathF.Sin((float)Game1.TotalTime * 5f) * 0.2f); // Yanıp sönme
            
            spriteBatch.Draw(_circleTexture, SpawnCenter, null, new Color(1f, 1f, 1f, alpha), 
                (float)Game1.TotalTime, new Vector2(32, 32), scale, SpriteEffects.None, 0f);
        }
        
        foreach (var enemy in Enemies) enemy.Draw(spriteBatch);
    }
}

public class EnemyManager
{
    private List<EnemyGroup> _groups = new List<EnemyGroup>();
    private GraphicsDevice _graphicsDevice;
    private Texture2D _pixelTexture;
    
    // Basit bir particle sistemi burada tutulabilir veya Game1'den alınabilir
    // Şimdilik smoke efektini basitleştirmek için dışarıdan almayalım, 
    // ama Demon sınıfındaki `ShouldEmitSmoke` kontrol edilebilir.
    
    public event Action<int> OnPlayerDamaged;
    
    public EnemyManager(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }
    
    public void SpawnGroup(Vector2 position, EnemyType type, int count = 3)
    {
        var group = new EnemyGroup(_graphicsDevice, position, type, count);
        group.OnEnemyAttackPlayer += (damage) => OnPlayerDamaged?.Invoke(damage);
        _groups.Add(group);
    }

    public void ClearGroups()
    {
        _groups.Clear();
    }
    
    public void Update(GameTime gameTime, Vector2 playerPosition, Rectangle playerBounds)
    {
        foreach (var group in _groups)
        {
            group.Update(gameTime, playerPosition, playerBounds);
        }
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var group in _groups)
        {
            group.Draw(spriteBatch);
        }
        
        // Sağlık Barları
        foreach (var group in _groups)
        {
            if (group.IsWiped) continue;
            
            foreach (var enemy in group.Enemies)
            {
                if (!enemy.IsDead)
                {
                    Vector2 pos = enemy.GetHealthBarPosition();
                    int w = enemy.GetWidth();
                    float hp = enemy.GetHealthPercent();
                    
                    // BG
                    spriteBatch.Draw(_pixelTexture, new Rectangle((int)pos.X, (int)pos.Y, w, 4), Color.Black);
                    // HP
                    Color c = hp > 0.5f ? Color.Green : (hp > 0.2f ? Color.Orange : Color.Red);
                    spriteBatch.Draw(_pixelTexture, new Rectangle((int)pos.X, (int)pos.Y, (int)(w * hp), 4), c);
                }
            }
        }
    }
    
    // Otomatik saldırı için sorgular
    public Enemy GetNearestEnemy(Vector2 pos, float maxDist)
    {
        Enemy best = null;
        float bestDist = maxDist;
        foreach(var g in _groups) {
            foreach(var e in g.Enemies) {
                if(e.IsDead) continue;
                float d = Vector2.Distance(pos, e.Position);
                if(d < bestDist) { bestDist = d; best = e; }
            }
        }
        return best;
    }
    
    // AOE SALDIRI İÇİN
    public List<Enemy> GetEnemiesInArea(Vector2 center, float radius, int facingDir)
    {
        List<Enemy> targets = new List<Enemy>();
        foreach(var g in _groups) {
            foreach(var e in g.Enemies) {
                if(e.IsDead) continue;
                
                // Mesafe kontrolü
                float d = Vector2.Distance(center, e.Position);
                if(d <= radius) 
                {
                    // Yön kontrolü (arkadaki düşmanlara vurmamak için)
                    // Basitçe: Düşman sağdaysa ve biz sağa bakıyorsak (veya tam tersi)
                    float dx = e.Position.X - center.X;
                    if (facingDir == 0 || (facingDir == 1 && dx > -20) || (facingDir == -1 && dx < 20))
                    {
                        targets.Add(e);
                    }
                }
            }
        }
        return targets;
    }
    
    public List<Enemy> GetAllEnemies()
    {
        List<Enemy> all = new List<Enemy>();
        foreach (var group in _groups)
        {
            foreach (var enemy in group.Enemies)
            {
                if (!enemy.IsDead) all.Add(enemy);
            }
        }
        return all;
    }
}
