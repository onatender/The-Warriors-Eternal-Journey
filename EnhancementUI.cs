using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EternalJourney;

public class EnhancementUI
{
    private GraphicsDevice _graphicsDevice;
    private int _screenWidth;
    private int _screenHeight;
    private Texture2D _pixelTexture;
    
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
        int slotY = _windowBounds.Bottom - 140;
        _charmSlotRect = new Rectangle(_windowBounds.X + 80, slotY, slotSize, slotSize);
        _protectSlotRect = new Rectangle(_windowBounds.Right - 130, slotY, slotSize, slotSize);
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
        // Mevcut seçimi temizle veya değiştir
        // Sıra: Yok -> %10 (20) -> %25 (21) -> %50 (22) -> Yok
        
        int currentId = _selectedCharm?.Id ?? -1;
        _selectedCharm = null;
        
        if (currentId == -1) TrySetCharm(20);
        else if (currentId == 20) TrySetCharm(21);
        else if (currentId == 21) TrySetCharm(22);
        else if (currentId == 22) _selectedCharm = null; // Başa dön
        
        // Eğer bir sonraki yoksa, zincirleme dene
        if (_selectedCharm == null && currentId != 22 && currentId != -1)
        { 
             // Örn: 20 seçiliydi, 21 denedik yok, 22 dene
             if (currentId == 20) TrySetCharm(22);
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
        // Sıra: Yok -> Muska (30) -> Büyülü Muska (31) -> Yok
        int currentId = _selectedProtection?.Id ?? -1;
        _selectedProtection = null;
        
        if (currentId == -1) TrySetProtection(30);
        else if (currentId == 30) TrySetProtection(31);
        else if (currentId == 31) _selectedProtection = null;
        
        if (_selectedProtection == null && currentId == -1) TrySetProtection(31); // 30 yoksa 31 dene
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
        _player.GainGold(-_costGold);
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
            _targetItem.UpgradeSuccess();
            _resultMessage = "YUKSELTME BASARILI!";
            _resultColor = Color.Gold;
            
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
            else if (_resultColor == Color.Gold)
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
        
        // Item İkon
        int iconY = _windowBounds.Y + 80;
        int iconSize = 64;
        Rectangle leftIconRect = new Rectangle(_windowBounds.Center.X - 80, iconY, iconSize, iconSize);
        Rectangle rightIconRect = new Rectangle(_windowBounds.Center.X + 20, iconY, iconSize, iconSize);
        
        if (_targetItem.Icon != null) spriteBatch.Draw(_targetItem.Icon, leftIconRect, Color.White);
        spriteBatch.DrawString(font, $"+{_targetItem.EnhancementLevel}", new Vector2(leftIconRect.X, leftIconRect.Bottom+5), Color.White);
        
        if (_nextLevelItem.Icon != null) spriteBatch.Draw(_nextLevelItem.Icon, rightIconRect, Color.White * 0.7f);
        spriteBatch.DrawString(font, $"+{_nextLevelItem.EnhancementLevel}", new Vector2(rightIconRect.X, rightIconRect.Bottom+5), Color.Lime);
        
        spriteBatch.DrawString(font, ">>", new Vector2(_windowBounds.Center.X - 10, iconY + 20), Color.Gray);

        // İstatistikler 
        int statY = iconY + 70;
        string statInfo = (_targetItem.Type == ItemType.Weapon) ? $"Hasar: +%20" : $"Defans: +%20";
        Vector2 statSize = font.MeasureString(statInfo);
        spriteBatch.DrawString(font, statInfo, new Vector2(_windowBounds.Center.X - statSize.X/2, statY), Color.LightGray);
        
        // Maliyetler
        int infoY = statY + 30;
        string costText = $"Tas: {_costStones} ({_inventory.GetItemCount(99)}) | Altin: {_costGold}";
        Color costColor = (_inventory.GetItemCount(99) >= _costStones && _player.Gold >= _costGold) ? Color.White : Color.Red;
        Vector2 costSz = font.MeasureString(costText);
        spriteBatch.DrawString(font, costText, new Vector2(_windowBounds.Center.X - costSz.X/2, infoY), costColor);
        
        // --- SLOTLAR ---
        // Charm Slot
        spriteBatch.Draw(_pixelTexture, _charmSlotRect, new Color(50, 50, 60));
        DrawBorder(spriteBatch, _charmSlotRect, 1, Color.Gray);
        if (_selectedCharm != null)
        {
            if (_selectedCharm.Icon != null) spriteBatch.Draw(_selectedCharm.Icon, _charmSlotRect, Color.White);
            spriteBatch.DrawString(font, "TILSIM", new Vector2(_charmSlotRect.X, _charmSlotRect.Bottom + 2), Color.LightGreen, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
        }
        else
        {
            spriteBatch.DrawString(font, "TILSIM", new Vector2(_charmSlotRect.X + 5, _charmSlotRect.Y + 15), Color.Gray, 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);
        }
        
        // Protect Slot
        spriteBatch.Draw(_pixelTexture, _protectSlotRect, new Color(50, 50, 60));
        DrawBorder(spriteBatch, _protectSlotRect, 1, Color.Gray);
        if (_selectedProtection != null)
        {
            if (_selectedProtection.Icon != null) spriteBatch.Draw(_selectedProtection.Icon, _protectSlotRect, Color.White);
            spriteBatch.DrawString(font, "MUSKA", new Vector2(_protectSlotRect.X, _protectSlotRect.Bottom + 2), Color.Orange, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
        }
        else
        {
            spriteBatch.DrawString(font, "MUSKA", new Vector2(_protectSlotRect.X + 5, _protectSlotRect.Y + 15), Color.Gray, 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);
        }

        // Şans
        int totalChance = _baseSuccessChance;
        if (_selectedCharm != null)
        {
             if (_selectedCharm.Id == 20) totalChance += 10;
             else if (_selectedCharm.Id == 21) totalChance += 25;
             else if (_selectedCharm.Id == 22) totalChance += 50;
        }
        string chanceText = $"Sans: %{totalChance}";
        Vector2 chanceSz = font.MeasureString(chanceText);
        spriteBatch.DrawString(font, chanceText, new Vector2(_windowBounds.Center.X - chanceSz.X/2, infoY + 40), totalChance >= 50 ? Color.Lime : Color.Orange);

        // Buton
        if (!_isAnimating)
        {
            Color btnColor = _isHoveringButton ? new Color(50, 150, 50) : new Color(30, 100, 30);
            spriteBatch.Draw(_pixelTexture, _buttonRect, btnColor);
            
            string btnText = "YUKSELT";
            Vector2 btnSz = font.MeasureString(btnText);
            spriteBatch.DrawString(font, btnText, _buttonRect.Center.ToVector2() - btnSz/2, Color.White);
        }
        
        // --- PARÇACIKLAR ---
        foreach(var p in _particles)
        {
            spriteBatch.Draw(_pixelTexture, 
                new Rectangle((int)p.Position.X, (int)p.Position.Y, (int)p.Size, (int)p.Size), 
                p.Color * p.Life);
        }
        
        // Sonuç Mesajı
        if (_isAnimating && !string.IsNullOrEmpty(_resultMessage))
        {
            float scale = 1.0f + (float)Math.Sin(_animationTimer * 5) * 0.2f;
            Vector2 msgSz = font.MeasureString(_resultMessage);
            Vector2 center = new Vector2(_windowBounds.Center.X, _windowBounds.Center.Y);
            Vector2 origin = msgSz / 2;
            spriteBatch.DrawString(font, _resultMessage, center + new Vector2(2, 2), Color.Black * 0.5f, 0f, origin, scale, SpriteEffects.None, 0f);
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
