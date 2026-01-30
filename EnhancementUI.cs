using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace EternalJourney;

public class EnhancementUI
{
    private GraphicsDevice _graphicsDevice;
    private int _screenWidth;
    private int _screenHeight;
    private Texture2D _pixelTexture;
    private SoundEffect _sfxSuccess;
    
    // Durum
    public bool IsOpen { get; private set; } = false;
    private Item _targetItem; // Referans (Inventory'deki asıl item)
    private Item _nextLevelItem; // Önizleme
    private Player _player;
    private Inventory _inventory;
    
    // UI
    private Rectangle _windowBounds;
    private Rectangle _buttonRect;
    private bool _isHoveringButton;
    
    // Animasyon
    private bool _isAnimating;
    private float _animationTimer;
    private float _idleTimer;
    private string _resultMessage = "";
    private Color _resultColor = Color.White;
    
    // Parçacık Sistemi
    private struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Size;
        public float Life; // 0.0 - 1.0
        public float Decay;
    }
    private System.Collections.Generic.List<Particle> _particles = new System.Collections.Generic.List<Particle>();
    private Random _rng = new Random();
    private MouseState _prevMouse;
    
    // Slotlar
    private Rectangle _charmSlotRect;
    private Rectangle _protectSlotRect;
    private Item _selectedCharm = null;
    private Item _selectedProtection = null;
    
    // Hesaplamalar
    private int _costGold;
    private int _costStones;
    private float _baseSuccessChance;
    
    public event Action OnUpgradeCompleted; 
    
    public EnhancementUI(GraphicsDevice graphicsDevice, int width, int height)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = width;
        _screenHeight = height;
        
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        int winW = 450;
        int winH = 550;
        _windowBounds = new Rectangle((width - winW)/2, (height - winH)/2, winW, winH);
        
        // Buton konumu
        _buttonRect = new Rectangle(_windowBounds.X + 75, _windowBounds.Bottom - 70, winW - 150, 40);
        
        // Slot Konumları
        int slotSize = 50;
        int slotY = winH - 160; // 140 -> 160 (Synchronized with Draw)
        _charmSlotRect = new Rectangle(_windowBounds.X + 80, _windowBounds.Y + slotY, slotSize, slotSize);
        _protectSlotRect = new Rectangle(_windowBounds.Right - 130, _windowBounds.Y + slotY, slotSize, slotSize);
    }
    
    public void SetSFX(SoundEffect success)
    {
        _sfxSuccess = success;
    }
    
    public void Open(Item item, Player player, Inventory inventory)
    {
        _targetItem = item;
        _player = player;
        _inventory = inventory;
        
        // Hesaplamalar
        int level = _targetItem.EnhancementLevel;
        _costStones = (int)Math.Pow(2, level); 
        _costGold = 100 * (level + 1);
        _baseSuccessChance = GetBaseSuccessChance(level); 
        
        _nextLevelItem = _targetItem.Clone();
        _nextLevelItem.UpgradeSuccess(); 
        
        _resultMessage = "";
        _isAnimating = false;
        _particles.Clear();
        _selectedCharm = null;
        _selectedProtection = null;
        
        IsOpen = true;
    }
    
    public void Close()
    {
        IsOpen = false;
        _targetItem = null;
    }
    
    public void Update(GameTime gameTime)
    {
        if (!IsOpen) return;
        
        MouseState mouse = Mouse.GetState();
        Point mousePos = new Point(mouse.X, mouse.Y);
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _idleTimer += dt;
        
        // Parçacıkları güncelle
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Position += p.Velocity * dt;
            p.Velocity.Y += 200f * dt; // Yerçekimi
            p.Life -= p.Decay * dt;
            if (p.Life <= 0) _particles.RemoveAt(i);
            else _particles[i] = p; // Struct olduğu için geri atama
        }
        
        if (_isAnimating)
        {
            UpdateAnimation(gameTime);
            _prevMouse = mouse;
            return;
        }
        
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }
        
        _isHoveringButton = _buttonRect.Contains(mousePos);
        
        // Tıklama İşlemleri
        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            if (_charmSlotRect.Contains(mousePos)) CycleCharm();
            else if (_protectSlotRect.Contains(mousePos)) CycleProtection();
            else if (_isHoveringButton) TryUpgrade();
        }
        
        _prevMouse = mouse;
    }
    
    private void CycleCharm()
    {
        int currentId = _selectedCharm?.Id ?? -1;
        
        // Sıradaki ID sırasını belirle
        int[] ids = { 20, 21, 22, 32, -1 }; // -1 = Boş slot
        int startIndex = 0;
        
        if (currentId == 20) startIndex = 1;
        else if (currentId == 21) startIndex = 2;
        else if (currentId == 22) startIndex = 3;
        else if (currentId == 32) startIndex = 4;
        else startIndex = 0;

        for (int i = 0; i < ids.Length; i++)
        {
            int index = (startIndex + i) % ids.Length;
            int targetId = ids[index];

            if (targetId == -1)
            {
                _selectedCharm = null;
                return;
            }
            else if (_inventory.GetItemCount(targetId) > 0)
            {
                _selectedCharm = ItemDatabase.GetItem(targetId);
                return;
            }
        }
    }
    
    private void TrySetCharm(int id)
    {
        if (_inventory.GetItemCount(id) > 0)
        {
            _selectedCharm = ItemDatabase.GetItem(id);
        }
        else
        {
            // Eğer istenen yoksa bir sonrakini dene (Cycle mantığı içinde handle edilebilir ama basit tutalım)
        }
    }
    
    private void CycleProtection()
    {
        int currentId = _selectedProtection?.Id ?? -1;
        int[] ids = { 30, 31, -1 };
        int startIndex = 0;

        if (currentId == 30) startIndex = 1;
        else if (currentId == 31) startIndex = 2;
        else startIndex = 0;

        for (int i = 0; i < ids.Length; i++)
        {
            int index = (startIndex + i) % ids.Length;
            int targetId = ids[index];

            if (targetId == -1)
            {
                _selectedProtection = null;
                return;
            }
            else if (_inventory.GetItemCount(targetId) > 0)
            {
                _selectedProtection = ItemDatabase.GetItem(targetId);
                return;
            }
        }
    }
    
    private void TrySetProtection(int id)
    {
        if (_inventory.GetItemCount(id) > 0)
        {
            _selectedProtection = ItemDatabase.GetItem(id);
        }
    }
    
    private void TryUpgrade()
    {
        int stoneCount = _inventory.GetItemCount(99); 
        
        if (_player.Gold < _costGold)
        {
            _resultMessage = "Yetersiz Altın!";
            _resultColor = Color.Red;
            CreateParticles(_windowBounds.Center.ToVector2(), Color.Gray, 10);
            return;
        }
        
        if (stoneCount < _costStones)
        {
            _resultMessage = "Yetersiz Taş!";
            _resultColor = Color.Red;
            return;
        }
        
        // Harcama yap (Taş ve Altın)
        _player.GainGold(-_costGold, true); // Silent: true (Ses çıkmasın)
        _inventory.RemoveItem(99, _costStones); 
        
        // Şans Hesapla
        float chance = _baseSuccessChance;
        
        // Eğer İlahi Muska seçiliyse %100 yap
        if (_selectedProtection != null && _selectedProtection.Id == 32)
        {
            chance = 100f;
        }
        else if (_selectedCharm != null)
        {
             if (_selectedCharm.Id == 20) chance += 10;
             else if (_selectedCharm.Id == 21) chance += 25;
             else if (_selectedCharm.Id == 22) chance += 50;
        }
        
        // Zar at (0.0 - 100.0)
        double roll = _rng.NextDouble() * 100.0;
        _isAnimating = true;
        _animationTimer = 0f;
        
        if (roll < chance)
        {
            // BAŞARILI
            _sfxSuccess?.Play();
            _targetItem.UpgradeSuccess();
            _resultMessage = "YÜKSELTME BAŞARILI!";
            _resultColor = Color.LightGreen; // Gold yerine açık yeşil daha okunur olabilir veya Gold kalsın ama arka plan ekleyeceğiz
            
            CreateParticles(_windowBounds.Center.ToVector2(), Color.Gold, 50);
            CreateParticles(_windowBounds.Center.ToVector2(), Color.Cyan, 30);
            
            // Tılsım ve Muska harca (İşlemde kullanıldı)
            if (_selectedCharm != null) _inventory.RemoveItem(_selectedCharm.Id, 1);
            if (_selectedProtection != null) _inventory.RemoveItem(_selectedProtection.Id, 1);
        }
        else
        {
            // BAŞARISIZ
            // Tılsım harca (şansı kullandık)
            if (_selectedCharm != null) _inventory.RemoveItem(_selectedCharm.Id, 1);
            
            bool protectedItem = false;
            
            if (_selectedProtection != null)
            {
                // Muska harca (koruma devreye girdi)
                _inventory.RemoveItem(_selectedProtection.Id, 1);
                
                if (_selectedProtection.Id == 31) // Büyülü
                {
                    protectedItem = true;
                    _resultMessage = "BAŞARISIZ (KORUNDU!)";
                    _resultColor = Color.Orange;
                    CreateParticles(_windowBounds.Center.ToVector2(), Color.Purple, 20);
                }
                else if (_selectedProtection.Id == 30) // Normal
                {
                    protectedItem = true;
                    _targetItem.Downgrade(); // Seviye düşür
                    _resultMessage = "BAŞARISIZ (SEVİYE DÜŞTÜ)";
                    _resultColor = Color.Orange;
                    CreateParticles(_windowBounds.Center.ToVector2(), Color.Gray, 20);
                }
            }
            
            if (!protectedItem)
            {
                // YOK OLDU
                _resultMessage = "EŞYA YOK OLDU!";
                _resultColor = Color.Red;
                _inventory.RemoveItemInstance(_targetItem); // SİL
                CreateParticles(_windowBounds.Center.ToVector2(), Color.Black, 50);
                CreateParticles(_windowBounds.Center.ToVector2(), Color.Red, 30);
                // Burada pencereyi hemen kapatacağız animasyon sonunda
            }
        }
    }
    
    private void CreateParticles(Vector2 center, Color color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (float)(_rng.NextDouble() * Math.PI * 2);
            float speed = _rng.Next(50, 300);
            
            _particles.Add(new Particle
            {
                Position = center,
                Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                Color = color,
                Size = _rng.Next(2, 6),
                Life = 1.0f,
                Decay = (float)(0.5 + _rng.NextDouble())
            });
        }
    }
    
    private void UpdateAnimation(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _animationTimer += dt;
        
        if (_animationTimer > 2.0f)
        {
            _isAnimating = false;
            
            if (_resultMessage.Contains("YOK OLDU"))
            {
                Close(); // Eşya gitti, kapat
            }
            else if (_resultColor == Color.LightGreen || _resultColor == Color.Gold) // Başarılı
            {
                 Close(); // Başarılı, kapat (sonucu görsünler diye beklettik zaten)
                 OnUpgradeCompleted?.Invoke();
            }
            else
            {
                // Başarısız ama eşya duruyor (korundu veya dowgrade)
                // UI'ı güncelle
                _nextLevelItem = _targetItem.Clone();
                _nextLevelItem.UpgradeSuccess();
                // Seçimleri temizle (harcandıysa zaten TryUpgrade sildi ama referansları null yapalım eğer inventoryden bittiyse)
                if (_selectedCharm != null && _inventory.GetItemCount(_selectedCharm.Id) <= 0) _selectedCharm = null;
                if (_selectedProtection != null && _inventory.GetItemCount(_selectedProtection.Id) <= 0) _selectedProtection = null;
            }
        }
    }
    
    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        if (!IsOpen) return;
        
        // Arkaplan karartma
        spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, _screenWidth, _screenHeight), new Color(0, 0, 0, 200));
        
        // Ana Pencere
        spriteBatch.Draw(_pixelTexture, _windowBounds, new Color(30, 32, 45));
        DrawBorder(spriteBatch, _windowBounds, 2, new Color(180, 160, 100)); // Altın rengi çerçeve
        
        // Üst Başlık Şeridi
        Rectangle headerBar = new Rectangle(_windowBounds.X, _windowBounds.Y, _windowBounds.Width, 45);
        spriteBatch.Draw(_pixelTexture, headerBar, new Color(45, 48, 65));
        DrawBorder(spriteBatch, headerBar, 1, new Color(200, 180, 120, 100));

        string title = "EŞYA GÜÇLENDİRME";
        Vector2 titleSize = font.MeasureString(title) * 0.9f;
        spriteBatch.DrawString(font, title, new Vector2(_windowBounds.Center.X - titleSize.X/2, _windowBounds.Y + 12), new Color(255, 230, 150), 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
        
        // --- ITEM ÖNİZLEME (ÜST) ---
        int iconY = _windowBounds.Y + 70;
        int iconSize = 64;
        Rectangle leftIconRect = new Rectangle(_windowBounds.Center.X - 95, iconY, iconSize, iconSize);
        Rectangle rightIconRect = new Rectangle(_windowBounds.Center.X + 31, iconY, iconSize, iconSize);
        
        // Sol İkon (Mevcut)
        spriteBatch.Draw(_pixelTexture, leftIconRect, new Color(40, 42, 60));
        DrawBorder(spriteBatch, leftIconRect, 1, Color.Gray * 0.5f);
        if (_targetItem.Icon != null) 
        {
            spriteBatch.Draw(_targetItem.Icon, leftIconRect, _targetItem.GetTintColor());
            string levelText = $"+{_targetItem.EnhancementLevel}";
            Vector2 levelSz = font.MeasureString(levelText) * 0.8f;
            if (_targetItem.Id == 10 || _targetItem.Id == 32)
            {
                DrawDivineEffect(spriteBatch, leftIconRect);
            }

            spriteBatch.DrawString(font, levelText, new Vector2(leftIconRect.Center.X - levelSz.X/2, leftIconRect.Bottom + 5), Color.White, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        }
        
        // Ok Göstergesi
        DrawGraphicArrow(spriteBatch, new Rectangle(_windowBounds.Center.X - 20, iconY + 15, 40, 30), Color.Goldenrod);
        
        // Sağ İkon (Gelecek)
        spriteBatch.Draw(_pixelTexture, rightIconRect, new Color(40, 42, 60));
        DrawBorder(spriteBatch, rightIconRect, 1, Color.Gray * 0.5f);
        if (_nextLevelItem.Icon != null) 
        {
            spriteBatch.Draw(_nextLevelItem.Icon, rightIconRect, _nextLevelItem.GetTintColor() * 0.9f);

            if (_nextLevelItem.Id == 10 || _nextLevelItem.Id == 32)
            {
                DrawDivineEffect(spriteBatch, rightIconRect);
            }

            string nextLevelText = $"+{_nextLevelItem.EnhancementLevel}";
            Vector2 nextLevelSz = font.MeasureString(nextLevelText) * 0.9f;
            spriteBatch.DrawString(font, nextLevelText, new Vector2(rightIconRect.Center.X - nextLevelSz.X/2, rightIconRect.Bottom + 5), Color.Lime, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
        }

        // --- ÖZELLİK KARŞILAŞTIRMASI (ORTA) ---
        int statY = iconY + 115;
        
        // Özellikleri topla
        var stats = new System.Collections.Generic.List<(string name, string oldV, string newV)>();
        if (_targetItem.Type == ItemType.Weapon)
        {
            stats.Add(("Hasar", $"{_targetItem.MinDamage}-{_targetItem.MaxDamage}", $"{_nextLevelItem.MinDamage}-{_nextLevelItem.MaxDamage}"));
            stats.Add(("Hız", _targetItem.AttackSpeed.ToString(), _nextLevelItem.AttackSpeed.ToString()));
        }
        else if (_targetItem.Type == ItemType.Armor || _targetItem.Type == ItemType.Helmet)
        {
            stats.Add(("Savunma", _targetItem.Defense.ToString(), _nextLevelItem.Defense.ToString()));
            stats.Add(("Can", $"+{_targetItem.Health}", $"+{_nextLevelItem.Health}"));
        }
        else if (_targetItem.Type == ItemType.Shield)
        {
            stats.Add(("Savunma", _targetItem.Defense.ToString(), _nextLevelItem.Defense.ToString()));
            stats.Add(("Blok", $"%{_targetItem.BlockChance}", $"%{_nextLevelItem.BlockChance}"));
        }

        foreach (var stat in stats)
        {
            string labelStr = $"{stat.name}: ";
            float scale = 0.85f;
            float labelW = font.MeasureString(labelStr).X * scale;
            float valOldW = font.MeasureString(stat.oldV).X * scale;
            float arrowW = 30; // Grafik ok genişliği
            float valNewW = font.MeasureString(stat.newV).X * scale;
            
            float totalW = labelW + valOldW + arrowW + 10 + valNewW;
            float startX = _windowBounds.Center.X - totalW / 2;

            // Label
            spriteBatch.DrawString(font, labelStr, new Vector2(startX, statY), Color.LightGray, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            // Eski Değer
            spriteBatch.DrawString(font, stat.oldV, new Vector2(startX + labelW, statY), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            // Grafik Ok
            DrawGraphicArrow(spriteBatch, new Rectangle((int)(startX + labelW + valOldW + 5), statY + 2, (int)arrowW, 16), Color.Lime * 0.7f);
            // Yeni Değer
            spriteBatch.DrawString(font, stat.newV, new Vector2(startX + labelW + valOldW + arrowW + 10, statY), Color.Lime, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            
            statY += 28;
        }

        // --- GEREKSİNİMLER (ALT ORTA) ---
        int costY = statY + 20;
        
        // Gereksinim Paneli
        Rectangle costBox = new Rectangle(_windowBounds.X + 40, costY, _windowBounds.Width - 80, 75);
        spriteBatch.Draw(_pixelTexture, costBox, new Color(20, 22, 30, 150));
        DrawBorder(spriteBatch, costBox, 1, Color.White * 0.1f);

        string stoneReq = $"Gereken Taş: {_costStones} (Sende: {_inventory.GetItemCount(99)})";
        string goldReq = $"Gereken Altın: {_costGold} (Sende: {_player.Gold})";
        
        Color stoneCol = _inventory.GetItemCount(99) >= _costStones ? Color.White : Color.Red;
        Color goldCol = _player.Gold >= _costGold ? Color.White : Color.Red;

        float reqScale = 0.8f;
        spriteBatch.DrawString(font, stoneReq, new Vector2(_windowBounds.Center.X - font.MeasureString(stoneReq).X * reqScale / 2, costY + 15), stoneCol, 0, Vector2.Zero, reqScale, SpriteEffects.None, 0);
        spriteBatch.DrawString(font, goldReq, new Vector2(_windowBounds.Center.X - font.MeasureString(goldReq).X * reqScale / 2, costY + 42), goldCol, 0, Vector2.Zero, reqScale, SpriteEffects.None, 0);

        // --- TILSIM VE MUSKA (ALT) ---
        int slotY = costY + 100;
        // Slot pozisyonlarını güncelle (Ortala)
        _charmSlotRect = new Rectangle(_windowBounds.Center.X - 65, slotY, 55, 55);
        _protectSlotRect = new Rectangle(_windowBounds.Center.X + 10, slotY, 55, 55);

        // Tilsim Slot
        spriteBatch.Draw(_pixelTexture, _charmSlotRect, new Color(40, 42, 60));
        DrawBorder(spriteBatch, _charmSlotRect, 1, _selectedCharm != null ? Color.Cyan : Color.Gray);
        if (_selectedCharm != null)
        {
            spriteBatch.Draw(_selectedCharm.Icon, _charmSlotRect, _selectedCharm.GetTintColor());

            if (_selectedCharm.Id == 32 || _selectedCharm.Id == 10)
            {
                DrawDivineEffect(spriteBatch, _charmSlotRect);
            }

            string label = _selectedCharm.Id switch { 20 => "+%10", 21 => "+%25", 22 => "+%50", 32 => "İLAHİ", _ => "" };
            Vector2 sz = font.MeasureString(label) * 0.7f;
            spriteBatch.DrawString(font, label, new Vector2(_charmSlotRect.Center.X - sz.X/2, _charmSlotRect.Bottom + 4), Color.Cyan, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
        }
        else
        {
            string label = "TILSIM";
            Vector2 sz = font.MeasureString(label) * 0.5f;
            spriteBatch.DrawString(font, label, _charmSlotRect.Center.ToVector2() - sz/2, Color.DimGray, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
        }

        // Muska Slot
        spriteBatch.Draw(_pixelTexture, _protectSlotRect, new Color(40, 42, 60));
        DrawBorder(spriteBatch, _protectSlotRect, 1, _selectedProtection != null ? Color.Orange : Color.Gray);
        if (_selectedProtection != null)
        {
            spriteBatch.Draw(_selectedProtection.Icon, _protectSlotRect, _selectedProtection.GetTintColor());
            
            string label = _selectedProtection.Id == 31 ? "KORUMA" : "MUSKA";
            Vector2 sz = font.MeasureString(label) * 0.7f;
            spriteBatch.DrawString(font, label, new Vector2(_protectSlotRect.Center.X - sz.X/2, _protectSlotRect.Bottom + 4), Color.Orange, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
        }
        else
        {
            string label = "MUSKA";
            Vector2 sz = font.MeasureString(label) * 0.5f;
            spriteBatch.DrawString(font, label, _protectSlotRect.Center.ToVector2() - sz/2, Color.DimGray, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
        }

        // --- SONUÇ VE BUTON (EN ALT) ---
        int bottomY = _windowBounds.Bottom - 110;

        float totalChance = _baseSuccessChance;
        if (_selectedCharm != null && _selectedCharm.Id == 32)
        {
            totalChance = 100f;
        }
        else if (_selectedCharm != null)
        {
             if (_selectedCharm.Id == 20) totalChance += 10;
             else if (_selectedCharm.Id == 21) totalChance += 25;
             else if (_selectedCharm.Id == 22) totalChance += 50;
        }
        
        string chanceText = $"Başarı Şansı: %{(totalChance > 100 ? 100 : totalChance):0.#}";
        Vector2 chanceSz = font.MeasureString(chanceText) * 0.9f;
        spriteBatch.DrawString(font, chanceText, new Vector2(_windowBounds.Center.X - chanceSz.X/2, bottomY), totalChance >= 50 ? Color.Lime : Color.Gold, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);

        _buttonRect = new Rectangle(_windowBounds.X + 80, _windowBounds.Bottom - 60, _windowBounds.Width - 160, 40);
        if (!_isAnimating)
        {
            Color btnColor = _isHoveringButton ? new Color(60, 160, 60) : new Color(40, 100, 40);
            spriteBatch.Draw(_pixelTexture, _buttonRect, btnColor);
            DrawBorder(spriteBatch, _buttonRect, 1, Color.Lime * 0.4f);
            
            string btnText = "YÜKSELT";
            Vector2 btnSz = font.MeasureString(btnText);
            spriteBatch.DrawString(font, btnText, _buttonRect.Center.ToVector2() - btnSz/2, Color.White);
        }
        
        // Parçacıklar
        foreach(var p in _particles)
        {
            spriteBatch.Draw(_pixelTexture, new Rectangle((int)p.Position.X, (int)p.Position.Y, (int)p.Size, (int)p.Size), p.Color * p.Life);
        }
        
        // --- SONUÇ ANİMASYONU ---
        if (_isAnimating && !string.IsNullOrEmpty(_resultMessage))
        {
            float scale = 1.1f + (float)Math.Sin(_animationTimer * 8) * 0.1f;
            Vector2 msgSz = font.MeasureString(_resultMessage);
            Vector2 center = new Vector2(_windowBounds.Center.X, _windowBounds.Center.Y);
            Vector2 origin = msgSz / 2;
            
            Rectangle resultBox = new Rectangle((int)center.X - (int)(msgSz.X * scale / 2 + 30), (int)center.Y - (int)(msgSz.Y * scale / 2 + 20), (int)(msgSz.X * scale + 60), (int)(msgSz.Y * scale + 40));
            spriteBatch.Draw(_pixelTexture, resultBox, new Color(10, 10, 15, 230));
            DrawBorder(spriteBatch, resultBox, 2, _resultColor);
            
            spriteBatch.DrawString(font, _resultMessage, center, _resultColor, 0f, origin, scale, SpriteEffects.None, 0f);
        }
    }
    
    private void DrawGraphicArrow(SpriteBatch sb, Rectangle rect, Color color)
    {
        // Gövde
        int stemHeight = rect.Height / 3;
        sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y + stemHeight, rect.Width / 2, stemHeight), color);
        
        // Baş (Üçgenimsi)
        int headWidth = rect.Width / 2;
        for (int i = 0; i < headWidth; i++)
        {
            int h = (int)(rect.Height * (1.0f - (float)i / headWidth));
            sb.Draw(_pixelTexture, new Rectangle(rect.X + rect.Width / 2 + i, rect.Center.Y - h / 2, 1, h), color);
        }
    }

    private float GetBaseSuccessChance(int currentLevel)
    {
        // 100, 90, 80, 70, 60, 50, 40, 30, 20, 10, 5, 4, 3, 2, 1, 0.5
        return currentLevel switch
        {
            0 => 100f,
            1 => 90f,
            2 => 80f,
            3 => 70f,
            4 => 60f,
            5 => 50f,
            6 => 40f,
            7 => 30f,
            8 => 20f,
            9 => 10f,
            10 => 5f,
            11 => 4f,
            12 => 3f,
            13 => 2f,
            14 => 1f,
            _ => 0.5f // +15 ve sonrası için %0.5
        };
    }

    private void DrawBorder(SpriteBatch sb, Rectangle rect, int thickness, Color color)
    {
        sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        sb.Draw(_pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    private void DrawDivineEffect(SpriteBatch sb, Rectangle rect)
    {
        float time = _idleTimer;
        Vector2 center = rect.Center.ToVector2();
        
        // 1. Çakan Yıldırımlar
        float phase = time % 0.6f;
        if (phase < 0.12f)
        {
            Random rnd = new Random((int)(time * 15f) + rect.X + rect.Y);
            Vector2 p1 = new Vector2(rect.X + rnd.Next(5, rect.Width - 5), rect.Y + 5);
            Vector2 p2 = p1 + new Vector2(rnd.Next(-12, 13), rnd.Next(10, 18));
            Vector2 p3 = p2 + new Vector2(rnd.Next(-12, 13), rnd.Next(10, 18));
            
            Color strikeCol = rnd.Next(2) == 0 ? Color.White : Color.LightCyan;
            float strikeAlpha = 0.8f * (1.0f - (phase / 0.12f));
            
            DrawLine(sb, p1, p2, strikeCol * strikeAlpha, 2);
            DrawLine(sb, p2, p3, strikeCol * strikeAlpha, 2);
            
            for(int i=0; i<3; i++) {
                sb.Draw(_pixelTexture, new Rectangle((int)p3.X + rnd.Next(-5,6), (int)p3.Y + rnd.Next(-5,6), 2, 2), Color.Gold * strikeAlpha);
            }
        }
        
        // 3. Kutsal Parçacıklar
        for (int i = 0; i < 4; i++)
        {
            float seed = i * 123.45f;
            float pTime = (time * 1.5f + i * 0.25f) % 1.0f;
            float pX = (float)Math.Sin(seed + time * 2f) * (rect.Width / 3f);
            float pY = (rect.Height / 4f) - pTime * 30f;
            float alpha = 1f - pTime;
            
            sb.Draw(_pixelTexture, new Rectangle((int)(center.X + pX), (int)(center.Y + pY), 2, 2), Color.White * alpha * 0.5f);
        }
    }

    private void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, int thickness = 1)
    {
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);
        sb.Draw(_pixelTexture,
            new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness),
            null, color, angle, Vector2.Zero, SpriteEffects.None, 0);
    }
}
