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
    private int _baseSuccessChance;
    
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
        _baseSuccessChance = Math.Max(10, 100 - (level * 10)); 
        
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
        int[] ids = { 20, 21, 22, -1 }; // -1 = Boş slot
        int startIndex = 0;
        
        if (currentId == 20) startIndex = 1;
        else if (currentId == 21) startIndex = 2;
        else if (currentId == 22) startIndex = 3;
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
            _resultMessage = "Yetersiz Altin!";
            _resultColor = Color.Red;
            CreateParticles(_windowBounds.Center.ToVector2(), Color.Gray, 10);
            return;
        }
        
        if (stoneCount < _costStones)
        {
            _resultMessage = "Yetersiz Tas!";
            _resultColor = Color.Red;
            return;
        }
        
        // Harcama yap (Taş ve Altın)
        _player.GainGold(-_costGold, true); // Silent: true (Ses çıkmasın)
        _inventory.RemoveItem(99, _costStones); 
        
        // Şans Hesapla
        int chance = _baseSuccessChance;
        if (_selectedCharm != null)
        {
             if (_selectedCharm.Id == 20) chance += 10;
             else if (_selectedCharm.Id == 21) chance += 25;
             else if (_selectedCharm.Id == 22) chance += 50;
        }
        
        // Zar at
        int roll = _rng.Next(1, 101);
        _isAnimating = true;
        _animationTimer = 0f;
        
        if (roll <= chance)
        {
            // BAŞARILI
            _sfxSuccess?.Play();
            _targetItem.UpgradeSuccess();
            _resultMessage = "YUKSELTME BASARILI!";
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
                    _resultMessage = "BASARISIZ (KORUNDU!)";
                    _resultColor = Color.Orange;
                    CreateParticles(_windowBounds.Center.ToVector2(), Color.Purple, 20);
                }
                else if (_selectedProtection.Id == 30) // Normal
                {
                    protectedItem = true;
                    _targetItem.Downgrade(); // Seviye düşür
                    _resultMessage = "BASARISIZ (SEVIYE DUSTU)";
                    _resultColor = Color.Orange;
                    CreateParticles(_windowBounds.Center.ToVector2(), Color.Gray, 20);
                }
            }
            
            if (!protectedItem)
            {
                // YOK OLDU
                _resultMessage = "ESYA YOK OLDU!";
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
        
        spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, _screenWidth, _screenHeight), new Color(0, 0, 0, 200));
        
        spriteBatch.Draw(_pixelTexture, _windowBounds, new Color(30, 30, 40));
        DrawBorder(spriteBatch, _windowBounds, 2, Color.Goldenrod);
        
        // Başlık
        string title = "ESYA GUCLENDIRME";
        Vector2 titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title, new Vector2(_windowBounds.Center.X - titleSize.X/2, _windowBounds.Y + 20), Color.Goldenrod);
        
        // --- ITEMLARI ÇİZ ---
        int iconY = _windowBounds.Y + 80;
        int iconSize = 64;
        Rectangle leftIconRect = new Rectangle(_windowBounds.Center.X - 90, iconY, iconSize, iconSize);
        Rectangle rightIconRect = new Rectangle(_windowBounds.Center.X + 26, iconY, iconSize, iconSize);
        
        // Mevcut Eşya
        if (_targetItem.Icon != null) 
        {
            spriteBatch.Draw(_targetItem.Icon, leftIconRect, Color.White);
            string levelText = $"+{_targetItem.EnhancementLevel}";
            Vector2 levelSz = font.MeasureString(levelText) * 0.8f;
            spriteBatch.DrawString(font, levelText, new Vector2(leftIconRect.Center.X - levelSz.X/2, leftIconRect.Bottom + 5), Color.White, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        }
        
        // Ok Göstergesi
        spriteBatch.DrawString(font, ">>", new Vector2(_windowBounds.Center.X - 10, iconY + 20), Color.Gray);
        
        // Sonraki Seviye
        if (_nextLevelItem.Icon != null) 
        {
            spriteBatch.Draw(_nextLevelItem.Icon, rightIconRect, Color.White * 0.8f);
            string nextLevelText = $"+{_nextLevelItem.EnhancementLevel}";
            Vector2 nextLevelSz = font.MeasureString(nextLevelText) * 0.9f;
            spriteBatch.DrawString(font, nextLevelText, new Vector2(rightIconRect.Center.X - nextLevelSz.X/2, rightIconRect.Bottom + 5), Color.Lime, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
        }

        // --- İSTATİSTİK DEĞİŞİMİ ---
        int statY = iconY + 110;
        string statInfo = (_targetItem.Type == ItemType.Weapon) ? "Hasar Artısı: +%20" : "Defans Artısı: +%20";
        Vector2 statSize = font.MeasureString(statInfo) * 0.9f;
        spriteBatch.DrawString(font, statInfo, new Vector2(_windowBounds.Center.X - statSize.X/2, statY), Color.LightGray, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
        
        // --- MALİYETLER ---
        int costY = statY + 40;
        string stoneText = $"Gereken Tas: {_costStones} ({_inventory.GetItemCount(99)})";
        string goldText = $"Gereken Altın: {_costGold} ({_player.Gold})";
        
        Vector2 stoneSz = font.MeasureString(stoneText) * 0.8f;
        Vector2 goldSz = font.MeasureString(goldText) * 0.8f;
        
        Color stoneCol = _inventory.GetItemCount(99) >= _costStones ? Color.White : Color.Red;
        Color goldCol = _player.Gold >= _costGold ? Color.White : Color.Red;
        
        spriteBatch.DrawString(font, stoneText, new Vector2(_windowBounds.Center.X - stoneSz.X/2, costY), stoneCol, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        spriteBatch.DrawString(font, goldText, new Vector2(_windowBounds.Center.X - goldSz.X/2, costY + 25), goldCol, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        
        // --- SLOTLAR (TILSIM / MUSKA) ---
        // Coordinates already calculated in constructor or UpdateLayout
        // We just ensure they are where they should be in Draw if we moved the window (not likely here)

        // TILSIM
        spriteBatch.Draw(_pixelTexture, _charmSlotRect, new Color(40, 40, 50));
        DrawBorder(spriteBatch, _charmSlotRect, 1, Color.Gray);
        if (_selectedCharm != null)
        {
            spriteBatch.Draw(_selectedCharm.Icon, _charmSlotRect, _selectedCharm.IconColor);
            string cLabel = _selectedCharm.Name.Split(' ')[0]; // Sadece "Sans" kısmı veya kısa isim
            if (_selectedCharm.Id == 20) cLabel = "+%10";
            else if (_selectedCharm.Id == 21) cLabel = "+%25";
            else if (_selectedCharm.Id == 22) cLabel = "+%50";
            
            Vector2 cSz = font.MeasureString(cLabel) * 0.7f;
            spriteBatch.DrawString(font, cLabel, new Vector2(_charmSlotRect.Center.X - cSz.X/2, _charmSlotRect.Bottom + 5), Color.Lime, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
        }
        else
        {
            string emptyText = "TILSIM";
            Vector2 eSz = font.MeasureString(emptyText) * 0.6f;
            spriteBatch.DrawString(font, emptyText, _charmSlotRect.Center.ToVector2() - eSz/2, Color.Gray, 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);
        }

        // MUSKA
        spriteBatch.Draw(_pixelTexture, _protectSlotRect, new Color(40, 40, 50));
        DrawBorder(spriteBatch, _protectSlotRect, 1, Color.Gray);
        if (_selectedProtection != null)
        {
            spriteBatch.Draw(_selectedProtection.Icon, _protectSlotRect, _selectedProtection.IconColor);
            string pLabel = "MUSKA";
            if (_selectedProtection.Id == 31) pLabel = "KORUMA";
            
            Vector2 pSz = font.MeasureString(pLabel) * 0.7f;
            spriteBatch.DrawString(font, pLabel, new Vector2(_protectSlotRect.Center.X - pSz.X/2, _protectSlotRect.Bottom + 5), Color.Orange, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
        }
        else
        {
            string emptyText = "MUSKA";
            Vector2 eSz = font.MeasureString(emptyText) * 0.6f;
            spriteBatch.DrawString(font, emptyText, _protectSlotRect.Center.ToVector2() - eSz/2, Color.Gray, 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);
        }

        // --- ŞANS BİLGİSİ ---
        int totalChance = _baseSuccessChance;
        if (_selectedCharm != null)
        {
             if (_selectedCharm.Id == 20) totalChance += 10;
             else if (_selectedCharm.Id == 21) totalChance += 25;
             else if (_selectedCharm.Id == 22) totalChance += 50;
        }
        string chanceText = $"Basarı Sansı: %{totalChance}";
        Vector2 chanceSz = font.MeasureString(chanceText);
        spriteBatch.DrawString(font, chanceText, new Vector2(_windowBounds.Center.X - chanceSz.X/2, _windowBounds.Bottom - 100), totalChance >= 50 ? Color.Lime : Color.Gold);

        // --- BUTON ---
        if (!_isAnimating)
        {
            Color btnColor = _isHoveringButton ? new Color(50, 150, 50) : new Color(30, 100, 30);
            spriteBatch.Draw(_pixelTexture, _buttonRect, btnColor);
            DrawBorder(spriteBatch, _buttonRect, 1, Color.Lime * 0.5f);
            
            string btnText = "YUKSELT";
            Vector2 btnSz = font.MeasureString(btnText);
            spriteBatch.DrawString(font, btnText, _buttonRect.Center.ToVector2() - btnSz/2, Color.White);
        }
        
        // --- PARÇACIKLAR ---
        foreach(var p in _particles)
        {
            spriteBatch.Draw(_pixelTexture, new Rectangle((int)p.Position.X, (int)p.Position.Y, (int)p.Size, (int)p.Size), p.Color * p.Life);
        }
        
        // --- SONUÇ MESAJI ---
        if (_isAnimating && !string.IsNullOrEmpty(_resultMessage))
        {
            float scale = 1.0f + (float)Math.Sin(_animationTimer * 6) * 0.15f;
            Vector2 msgSz = font.MeasureString(_resultMessage);
            Vector2 center = new Vector2(_windowBounds.Center.X, _windowBounds.Center.Y);
            Vector2 origin = msgSz / 2;
            
            // Arka plan kutusu
            int bgW = (int)(msgSz.X * scale + 60);
            int bgH = (int)(msgSz.Y * scale + 40);
            spriteBatch.Draw(_pixelTexture, new Rectangle((int)center.X - bgW/2, (int)center.Y - bgH/2, bgW, bgH), new Color(0, 0, 0, 220));
            DrawBorder(spriteBatch, new Rectangle((int)center.X - bgW/2, (int)center.Y - bgH/2, bgW, bgH), 2, _resultColor);
            
            spriteBatch.DrawString(font, _resultMessage, center + new Vector2(3, 3), Color.Black * 0.5f, 0f, origin, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, _resultMessage, center, _resultColor, 0f, origin, scale, SpriteEffects.None, 0f);
        }
    }
    
    private void DrawBorder(SpriteBatch sb, Rectangle rect, int thickness, Color color)
    {
        sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        sb.Draw(_pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
