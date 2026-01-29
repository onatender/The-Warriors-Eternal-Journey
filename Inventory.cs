using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace EternalJourney;

public class InventorySlot
{
    public Item Item { get; set; } = null;
    public int Quantity { get; set; } = 0;
    
    public bool IsEmpty => Item == null;
    
    public void Clear()
    {
        Item = null;
        Quantity = 0;
    }
}

public class Inventory
{
    // Grid ayarları
    private const int GRID_SIZE = 8;
    private const int PAGE_COUNT = 4;
    private const int SLOT_SIZE = 50;
    private const int SLOT_PADDING = 4;
    
    // Envanter durumu
    public bool IsOpen { get; private set; } = false;
    
    public void Open()
    {
        IsOpen = true;
    }
    
    public void Close()
    {
        IsOpen = false;
        // Seçim iptali vs. eklenmesi gerekirse buraya
        _selectedSlot = null;
        _hoveredSlot = new Point(-1, -1);
    }
    
    private int _currentPage = 0;
    
    // Slotlar - 4 sayfa x 64 slot
    private InventorySlot[,,] _slots;
    
    // Equipment slotları
    public InventorySlot WeaponSlot { get; private set; } = new InventorySlot();
    public InventorySlot ArmorSlot { get; private set; } = new InventorySlot();
    public InventorySlot ShieldSlot { get; private set; } = new InventorySlot();
    public InventorySlot HelmetSlot { get; private set; } = new InventorySlot();
    
    // Görsel
    private Texture2D _slotTexture;
    private Texture2D _slotHoverTexture;
    private Texture2D _equipSlotTexture;
    private Texture2D _backgroundTexture;
    private Texture2D _pageButtonTexture;
    private Texture2D _weaponSlotIcon;
    private Texture2D _armorSlotIcon;
    private Texture2D _shieldSlotIcon;
    private Texture2D _helmetSlotIcon;
    
    // SFX
    private SoundEffect _sfxCoinPickup;
    private SoundEffect _sfxCoinBuy;
    private SoundEffect _sfxCoinSell;
    private SoundEffect _sfxCoinDrop;
    
    public void SetCoinSounds(SoundEffect pickup, SoundEffect buy, SoundEffect sell, SoundEffect drop)
    {
        _sfxCoinPickup = pickup;
        _sfxCoinBuy = buy;
        _sfxCoinSell = sell;
        _sfxCoinDrop = drop;
    }
    
    // Pozisyon
    private Vector2 _position;
    private Rectangle _bounds;
    
    // Mouse hover ve seçim
    private Point _hoveredSlot = new Point(-1, -1);
    private int _hoveredEquipSlot = -1; // 0 = weapon, 1 = armor, 2 = shield, 3 = helmet, -1 = none
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyState;
    
    // Seçili item (taşıma için)
    private InventorySlot _selectedSlot = null;
    private bool _isFromEquipment = false;
    private int _selectedEquipIndex = -1;
    private Point _selectedGridPos = new Point(-1, -1);
    
    // Sayfa butonları
    private Rectangle _prevPageButton;
    private Rectangle _nextPageButton;
    private bool _hoveringPrev;
    private bool _hoveringNext;
    
    // Ekran boyutu
    private int _screenWidth;
    private int _screenHeight;
    
    // Equipment slot pozisyonları
    private Rectangle _weaponSlotRect;
    private Rectangle _armorSlotRect;
    private Rectangle _shieldSlotRect;
    private Rectangle _helmetSlotRect;
    
    // Tooltip
    private Item _tooltipItem = null;
    private Vector2 _tooltipPosition;
    
    // Event - equipment değiştiğinde
    public event Action<Item> OnWeaponEquipped;
    public event Action<Item> OnArmorEquipped;
    public event Action<Item> OnShieldEquipped;
    public event Action<Item> OnHelmetEquipped;
    public event Action<Item> OnEnhancementTargetSelected;
    
    public bool IsEnhancementMode { get; set; } = false;
    
    private Player _player;
    
    public void SetPlayer(Player player)
    {
        _player = player;
    }
    
    // ShopUI için slot erişimi
    public InventorySlot GetSlot(int page, int y, int x)
    {
        if (page >= 0 && page < PAGE_COUNT && y >= 0 && y < GRID_SIZE && x >= 0 && x < GRID_SIZE)
            return _slots[page, y, x];
        return null;
    }
    
    public Inventory(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        
        // Slotları oluştur
        _slots = new InventorySlot[PAGE_COUNT, GRID_SIZE, GRID_SIZE];
        for (int p = 0; p < PAGE_COUNT; p++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                for (int x = 0; x < GRID_SIZE; x++)
                {
                    _slots[p, y, x] = new InventorySlot();
                }
            }
        }
        
        // Item database'i initialize et
        ItemDatabase.Initialize(graphicsDevice);
        
        // Texture'ları oluştur
        CreateTextures(graphicsDevice);
        
        // Pozisyonu hesapla (ekranın ortası)
        CalculatePosition();
        
        // Başlangıç item'ı ekle - Artık TitleScreen/Game1.cs start kısmında yapılıyor.
        // AddItem(1, 1);
    }
    
    private void CreateTextures(GraphicsDevice graphicsDevice)
    {
        // Normal slot texture
        _slotTexture = new Texture2D(graphicsDevice, SLOT_SIZE, SLOT_SIZE);
        Color[] slotColors = new Color[SLOT_SIZE * SLOT_SIZE];
        for (int y = 0; y < SLOT_SIZE; y++)
        {
            for (int x = 0; x < SLOT_SIZE; x++)
            {
                int i = y * SLOT_SIZE + x;
                if (x == 0 || y == 0 || x == SLOT_SIZE - 1 || y == SLOT_SIZE - 1)
                {
                    slotColors[i] = new Color(80, 80, 100);
                }
                else
                {
                    float gradient = 1f - (float)y / SLOT_SIZE * 0.3f;
                    slotColors[i] = new Color(
                        (int)(40 * gradient),
                        (int)(42 * gradient),
                        (int)(55 * gradient)
                    );
                }
            }
        }
        _slotTexture.SetData(slotColors);
        
        // Hover texture
        _slotHoverTexture = new Texture2D(graphicsDevice, SLOT_SIZE, SLOT_SIZE);
        Color[] hoverColors = new Color[SLOT_SIZE * SLOT_SIZE];
        for (int y = 0; y < SLOT_SIZE; y++)
        {
            for (int x = 0; x < SLOT_SIZE; x++)
            {
                int i = y * SLOT_SIZE + x;
                if (x == 0 || y == 0 || x == SLOT_SIZE - 1 || y == SLOT_SIZE - 1)
                {
                    hoverColors[i] = new Color(200, 180, 100);
                }
                else if (x == 1 || y == 1 || x == SLOT_SIZE - 2 || y == SLOT_SIZE - 2)
                {
                    hoverColors[i] = new Color(150, 130, 70);
                }
                else
                {
                    float gradient = 1f - (float)y / SLOT_SIZE * 0.2f;
                    hoverColors[i] = new Color(
                        (int)(55 * gradient),
                        (int)(55 * gradient),
                        (int)(70 * gradient)
                    );
                }
            }
        }
        _slotHoverTexture.SetData(hoverColors);
        
        // Equipment slot texture (daha büyük, 60x60)
        int equipSize = 60;
        _equipSlotTexture = new Texture2D(graphicsDevice, equipSize, equipSize);
        Color[] equipColors = new Color[equipSize * equipSize];
        for (int y = 0; y < equipSize; y++)
        {
            for (int x = 0; x < equipSize; x++)
            {
                int i = y * equipSize + x;
                if (x < 2 || y < 2 || x >= equipSize - 2 || y >= equipSize - 2)
                {
                    equipColors[i] = new Color(100, 80, 60); // Altın kenarlık
                }
                else
                {
                    float gradient = 1f - (float)y / equipSize * 0.3f;
                    equipColors[i] = new Color(
                        (int)(35 * gradient),
                        (int)(38 * gradient),
                        (int)(50 * gradient)
                    );
                }
            }
        }
        _equipSlotTexture.SetData(equipColors);
        
        // Background texture
        _backgroundTexture = new Texture2D(graphicsDevice, 1, 1);
        _backgroundTexture.SetData(new[] { Color.White });
        
        // Page button texture
        int btnSize = 30;
        _pageButtonTexture = new Texture2D(graphicsDevice, btnSize, btnSize);
        Color[] btnColors = new Color[btnSize * btnSize];
        for (int y = 0; y < btnSize; y++)
        {
            for (int x = 0; x < btnSize; x++)
            {
                int i = y * btnSize + x;
                float centerX = btnSize / 2f;
                float centerY = btnSize / 2f;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                
                if (dist < btnSize / 2f - 1)
                {
                    float gradient = 1f - dist / (btnSize / 2f);
                    btnColors[i] = new Color(
                        (int)(60 + 40 * gradient),
                        (int)(60 + 40 * gradient),
                        (int)(80 + 50 * gradient)
                    );
                }
                else if (dist < btnSize / 2f)
                {
                    btnColors[i] = new Color(100, 100, 120);
                }
                else
                {
                    btnColors[i] = Color.Transparent;
                }
            }
        }
        _pageButtonTexture.SetData(btnColors);
        
        // Weapon slot icon (kılıç silüeti)
        _weaponSlotIcon = CreateWeaponSlotIcon(graphicsDevice);
        
        // Armor slot icon (zırh silüeti)
        _armorSlotIcon = CreateArmorSlotIcon(graphicsDevice);
        
        // Shield slot icon (kalkan silüeti)
        _shieldSlotIcon = CreateShieldSlotIcon(graphicsDevice);
        
        // Helmet slot icon (kask silüeti)
        _helmetSlotIcon = CreateHelmetSlotIcon(graphicsDevice);
    }
    
    private Texture2D CreateWeaponSlotIcon(GraphicsDevice graphicsDevice)
    {
        int size = 50;
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] colors = new Color[size * size];
        Color iconColor = new Color(60, 60, 80);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                colors[i] = Color.Transparent;
                
                // Kılıç silüeti
                int diff = Math.Abs((size - 1 - y) - x);
                if (diff < 3 && x > 8 && x < size - 10 && y > 8 && y < size - 10)
                {
                    colors[i] = iconColor;
                }
                
                // Kabza
                if (x >= size - 15 && x <= size - 8 && y >= size - 15 && y <= size - 8)
                {
                    colors[i] = iconColor;
                }
            }
        }
        
        texture.SetData(colors);
        return texture;
    }
    
    private Texture2D CreateArmorSlotIcon(GraphicsDevice graphicsDevice)
    {
        int size = 50;
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] colors = new Color[size * size];
        Color iconColor = new Color(60, 60, 80);
        
        int centerX = size / 2;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                colors[i] = Color.Transparent;
                
                // Zırh silüeti
                if (y >= 8 && y < 20)
                {
                    int width = 18 - (y - 8) / 2;
                    if (Math.Abs(x - centerX) < width)
                    {
                        colors[i] = iconColor;
                    }
                }
                
                if (y >= 20 && y < size - 8)
                {
                    int width = 12;
                    if (Math.Abs(x - centerX) < width)
                    {
                        colors[i] = iconColor;
                    }
                }
            }
        }
        
        texture.SetData(colors);
        return texture;
    }
    
    private Texture2D CreateShieldSlotIcon(GraphicsDevice graphicsDevice)
    {
        int size = 50;
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] colors = new Color[size * size];
        Color iconColor = new Color(60, 60, 80);
        
        int centerX = size / 2;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                colors[i] = Color.Transparent;
                
                // Kalkan silüeti (Üstte geniş, altta sivri)
                int maxWidth = 16;
                int width = maxWidth - (y * y) / 100;
                
                if (y >= 6 && y < size - 6 && width > 0 && Math.Abs(x - centerX) < width)
                {
                    colors[i] = iconColor;
                }
            }
        }
        
        texture.SetData(colors);
        return texture;
    }
    
    private Texture2D CreateHelmetSlotIcon(GraphicsDevice graphicsDevice)
    {
        int size = 50;
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] colors = new Color[size * size];
        Color iconColor = new Color(60, 60, 80);
        
        int centerX = size / 2;
        int headCenterY = 22;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                colors[i] = Color.Transparent;
                
                // Kask silüeti (Yarım küre)
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, headCenterY));
                
                if (y >= 8 && y < 38 && dist < 16)
                {
                    colors[i] = iconColor;
                    
                    // Yüz açıklığı
                    if (y > 18 && y < 32 && Math.Abs(x - centerX) < 5)
                    {
                        colors[i] = Color.Transparent;
                    }
                }
            }
        }
        
        texture.SetData(colors);
        return texture;
    }
    
    private void CalculatePosition()
    {
        int gridWidth = GRID_SIZE * (SLOT_SIZE + SLOT_PADDING) - SLOT_PADDING;
        int gridHeight = GRID_SIZE * (SLOT_SIZE + SLOT_PADDING) - SLOT_PADDING;
        int equipWidth = 80; // Equipment panel genişliği
        int totalWidth = gridWidth + equipWidth + 60; // Grid + Equipment + Padding
        int totalHeight = gridHeight + 100;
        
        _position = new Vector2(
            (_screenWidth - totalWidth) / 2f,
            (_screenHeight - totalHeight) / 2f
        );
        
        _bounds = new Rectangle(
            (int)_position.X,
            (int)_position.Y,
            totalWidth,
            totalHeight
        );
        
        // Sayfa butonları
        int buttonY = (int)_position.Y + totalHeight - 45;
        _prevPageButton = new Rectangle((int)_position.X + 20, buttonY, 30, 30);
        _nextPageButton = new Rectangle((int)_position.X + gridWidth + 30, buttonY, 30, 30);
        
        // Equipment slot pozisyonları (sağ tarafta)
        int equipX = (int)_position.X + gridWidth + 50;
        int equipY = (int)_position.Y + 60;
        
        _weaponSlotRect = new Rectangle(equipX, equipY, 60, 60);
        _armorSlotRect = new Rectangle(equipX, equipY + 70, 60, 60);
        _shieldSlotRect = new Rectangle(equipX, equipY + 140, 60, 60);
        _helmetSlotRect = new Rectangle(equipX, equipY + 210, 60, 60);
    }
    
    public void UpdateScreenSize(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        CalculatePosition();
    }
    
    public void Update(GameTime gameTime, KeyboardState currentKeyState, MouseState currentMouseState)
    {
        // I tuşu ile aç/kapa
        if (currentKeyState.IsKeyDown(Keys.I) && !_previousKeyState.IsKeyDown(Keys.I))
        {
            IsOpen = !IsOpen;
            _selectedSlot = null; // Seçimi sıfırla
            if (IsOpen) _player?.StopMoving();
        }
        
        if (IsOpen)
        {
            Point mousePos = currentMouseState.Position;
            
            // Tooltip sıfırla
            _tooltipItem = null;
            
            // Grid slot hover kontrolü
            _hoveredSlot = new Point(-1, -1);
            int gridStartX = (int)_position.X + 20;
            int gridStartY = (int)_position.Y + 50;
            
            for (int y = 0; y < GRID_SIZE; y++)
            {
                for (int x = 0; x < GRID_SIZE; x++)
                {
                    Rectangle slotRect = new Rectangle(
                        gridStartX + x * (SLOT_SIZE + SLOT_PADDING),
                        gridStartY + y * (SLOT_SIZE + SLOT_PADDING),
                        SLOT_SIZE,
                        SLOT_SIZE
                    );
                    
                    if (slotRect.Contains(mousePos))
                    {
                        _hoveredSlot = new Point(x, y);
                        
                        // Tooltip göster
                        if (!_slots[_currentPage, y, x].IsEmpty)
                        {
                            _tooltipItem = _slots[_currentPage, y, x].Item;
                            _tooltipPosition = new Vector2(mousePos.X + 15, mousePos.Y + 15);
                        }
                        break;
                    }
                }
            }
            
            // Equipment slot hover kontrolü
            _hoveredEquipSlot = -1;
            if (_weaponSlotRect.Contains(mousePos))
            {
                _hoveredEquipSlot = 0;
                if (!WeaponSlot.IsEmpty)
                {
                    _tooltipItem = WeaponSlot.Item;
                    _tooltipPosition = new Vector2(mousePos.X + 15, mousePos.Y + 15);
                }
            }
            else if (_armorSlotRect.Contains(mousePos))
            {
                _hoveredEquipSlot = 1;
                if (!ArmorSlot.IsEmpty)
                {
                    _tooltipItem = ArmorSlot.Item;
                    _tooltipPosition = new Vector2(mousePos.X + 15, mousePos.Y + 15);
                }
            }
            else if (_shieldSlotRect.Contains(mousePos))
            {
                _hoveredEquipSlot = 2;
                if (!ShieldSlot.IsEmpty)
                {
                    _tooltipItem = ShieldSlot.Item;
                    _tooltipPosition = new Vector2(mousePos.X + 15, mousePos.Y + 15);
                }
            }
            else if (_helmetSlotRect.Contains(mousePos))
            {
                _hoveredEquipSlot = 3;
                if (!HelmetSlot.IsEmpty)
                {
                    _tooltipItem = HelmetSlot.Item;
                    _tooltipPosition = new Vector2(mousePos.X + 15, mousePos.Y + 15);
                }
            }
            
            // Sayfa butonları hover
            _hoveringPrev = _prevPageButton.Contains(mousePos);
            _hoveringNext = _nextPageButton.Contains(mousePos);
            
            // Mouse tıklama
            if (currentMouseState.LeftButton == ButtonState.Pressed && 
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                HandleLeftClick(mousePos);
            }
            
            // Sağ tık - eşya giy/çıkar
            if (currentMouseState.RightButton == ButtonState.Pressed && 
                _previousMouseState.RightButton == ButtonState.Released)
            {
                HandleRightClick(mousePos);
            }
            
            // Sayfa değiştirme - klavye
            if (currentKeyState.IsKeyDown(Keys.Left) && !_previousKeyState.IsKeyDown(Keys.Left) && _currentPage > 0)
            {
                _currentPage--;
            }
            if (currentKeyState.IsKeyDown(Keys.Right) && !_previousKeyState.IsKeyDown(Keys.Right) && _currentPage < PAGE_COUNT - 1)
            {
                _currentPage++;
            }
        }
        
        _previousKeyState = currentKeyState;
        _previousMouseState = currentMouseState;
    }
    
    private void HandleLeftClick(Point mousePos)
    {
        // Sayfa butonları
        if (_hoveringPrev && _currentPage > 0)
        {
            _currentPage--;
            return;
        }
        if (_hoveringNext && _currentPage < PAGE_COUNT - 1)
        {
            _currentPage++;
            return;
        }
        
        // Enhancement Mode Kontrolü
        if (IsEnhancementMode)
        {
            Item targetItem = null;
            
            // Grid slotu mu?
            if (_hoveredSlot.X >= 0)
            {
                var slot = _slots[_currentPage, _hoveredSlot.Y, _hoveredSlot.X];
                if (!slot.IsEmpty) targetItem = slot.Item;
            }
            // Ekipman slotu mu?
            else if (_hoveredEquipSlot >= 0)
            {
                InventorySlot slot = _hoveredEquipSlot switch
                {
                    0 => WeaponSlot,
                    1 => ArmorSlot,
                    2 => ShieldSlot,
                    3 => HelmetSlot,
                    _ => null
                };
                if (slot != null && !slot.IsEmpty) targetItem = slot.Item;
            }
            
            // Eğer geçerli bir hedefse tetikle
            if (targetItem != null && (targetItem.Type == ItemType.Weapon || targetItem.Type == ItemType.Armor || targetItem.Type == ItemType.Shield || targetItem.Type == ItemType.Helmet))
            {
                OnEnhancementTargetSelected?.Invoke(targetItem);
                IsEnhancementMode = false; // Modu kapat
                return;
            }
            // Boş yere tıkladıysa iptal et
            else if (targetItem == null)
            {
                IsEnhancementMode = false;
                return;
            }
        }
        
        // Grid slot tıklama
        if (_hoveredSlot.X >= 0)
        {
            var slot = _slots[_currentPage, _hoveredSlot.Y, _hoveredSlot.X];
            
            if (_selectedSlot == null)
            {
                // Slot seç
                if (!slot.IsEmpty)
                {
                    _selectedSlot = slot;
                    _selectedGridPos = _hoveredSlot;
                    _isFromEquipment = false;
                }
            }
            else
            {
                // Slot'a bırak veya değiştir
                
                // Eğer ekipmandan geliyorsa ve hedef slot doluysa, hedef slotun tipi uygun mu?
                bool canSwap = true;
                if (_isFromEquipment && !slot.IsEmpty)
                {
                   if (_selectedEquipIndex == 0 && slot.Item.Type != ItemType.Weapon) canSwap = false;
                   if (_selectedEquipIndex == 1 && slot.Item.Type != ItemType.Armor) canSwap = false;
                }
                
                if (canSwap)
                {
                    SwapOrMove(_selectedSlot, slot);
                    
                    // Event tetikle (Ekipman değişti)
                    if (_isFromEquipment)
                    {
                         if (_selectedEquipIndex == 0) OnWeaponEquipped?.Invoke(WeaponSlot.Item);
                         else if (_selectedEquipIndex == 1) OnArmorEquipped?.Invoke(ArmorSlot.Item);
                    }
                    _selectedSlot = null;
                }
            }
        }
        // Equipment slot tıklama
        else if (_hoveredEquipSlot >= 0)
        {
            var equipSlot = _hoveredEquipSlot == 0 ? WeaponSlot : ArmorSlot;
            
            if (_selectedSlot == null)
            {
                if (!equipSlot.IsEmpty)
                {
                    _selectedSlot = equipSlot;
                    _selectedEquipIndex = _hoveredEquipSlot;
                    _isFromEquipment = true;
                }
            }
            else
            {
                // Equipment slot'una yerleştir (sadece uygun item)
                if (!_isFromEquipment && _selectedSlot.Item != null)
                {
                    if ((_hoveredEquipSlot == 0 && _selectedSlot.Item.Type == ItemType.Weapon) ||
                        (_hoveredEquipSlot == 1 && _selectedSlot.Item.Type == ItemType.Armor))
                    {
                        SwapOrMove(_selectedSlot, equipSlot);
                        
                        // Event tetikle
                        if (_hoveredEquipSlot == 0)
                            OnWeaponEquipped?.Invoke(WeaponSlot.Item);
                        else
                            OnArmorEquipped?.Invoke(ArmorSlot.Item);
                    }
                }
                _selectedSlot = null;
            }
        }
        else
        {
            // Boş alana tıklandı, seçimi iptal et
            _selectedSlot = null;
        }
    }
    
    private void HandleRightClick(Point mousePos)
    {
        // Grid slot - sağ tık ile eşya giy
        if (_hoveredSlot.X >= 0)
        {
            var slot = _slots[_currentPage, _hoveredSlot.Y, _hoveredSlot.X];
            if (!slot.IsEmpty && slot.Item != null)
            {
                // Güçlendirme Taşına sağ tık -> Modu aç
                if (slot.Item.Id == 99)
                {
                    IsEnhancementMode = !IsEnhancementMode; // Toggle
                    return;
                }
                
                if (slot.Item.Type == ItemType.Weapon)
                {
                    // Silahı giy
                    SwapOrMove(slot, WeaponSlot);
                    OnWeaponEquipped?.Invoke(WeaponSlot.Item);
                }
                else if (slot.Item.Type == ItemType.Armor)
                {
                    // Zırhı giy
                    SwapOrMove(slot, ArmorSlot);
                    OnArmorEquipped?.Invoke(ArmorSlot.Item);
                }
                else if (slot.Item.Type == ItemType.Consumable)
                {
                    // Potions
                    if (_player != null)
                    {
                        _player.Heal(slot.Item.Health);
                        // Reduce quantity
                        slot.Quantity--;
                        if (slot.Quantity <= 0)
                        {
                            slot.Item = null;
                        }
                    }
                }
                else if (slot.Item.Type == ItemType.Shield)
                {
                    // Kalkanı giy
                    SwapOrMove(slot, ShieldSlot);
                    OnShieldEquipped?.Invoke(ShieldSlot.Item);
                }
                else if (slot.Item.Type == ItemType.Helmet)
                {
                    // Kaskı giy
                    SwapOrMove(slot, HelmetSlot);
                    OnHelmetEquipped?.Invoke(HelmetSlot.Item);
                }
            }
        }
        // Equipment slot - sağ tık ile eşya çıkar
        else if (_hoveredEquipSlot >= 0)
        {
            InventorySlot equipSlot = _hoveredEquipSlot switch
            {
                0 => WeaponSlot,
                1 => ArmorSlot,
                2 => ShieldSlot,
                3 => HelmetSlot,
                _ => null
            };
            
            if (equipSlot != null && !equipSlot.IsEmpty)
            {
                // Boş slot bul ve oraya taşı
                for (int p = 0; p < PAGE_COUNT; p++)
                {
                    for (int y = 0; y < GRID_SIZE; y++)
                    {
                        for (int x = 0; x < GRID_SIZE; x++)
                        {
                            if (_slots[p, y, x].IsEmpty)
                            {
                                _slots[p, y, x].Item = equipSlot.Item;
                                _slots[p, y, x].Quantity = 1;
                                equipSlot.Clear();
                                
                                // Event tetikle
                                switch (_hoveredEquipSlot)
                                {
                                    case 0: OnWeaponEquipped?.Invoke(null); break;
                                    case 1: OnArmorEquipped?.Invoke(null); break;
                                    case 2: OnShieldEquipped?.Invoke(null); break;
                                    case 3: OnHelmetEquipped?.Invoke(null); break;
                                }
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
    
    private void SwapOrMove(InventorySlot from, InventorySlot to)
    {
        if (to.IsEmpty)
        {
            to.Item = from.Item;
            to.Quantity = from.Quantity;
            from.Clear();
        }
        else
        {
            // Swap
            var tempItem = to.Item;
            var tempQty = to.Quantity;
            to.Item = from.Item;
            to.Quantity = from.Quantity;
            from.Item = tempItem;
            from.Quantity = tempQty;
        }
    }
    
    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        if (!IsOpen) return;
        
        // Karartma efekti
        spriteBatch.Draw(_backgroundTexture, 
            new Rectangle(0, 0, _screenWidth, _screenHeight), 
            new Color(0, 0, 0, 180));
        
        // Ana panel
        DrawPanel(spriteBatch, _bounds);
        
        // Başlık
        string title = "ENVANTER";
        Vector2 titleSize = font.MeasureString(title);
        Vector2 titlePos = new Vector2(
            _position.X + 20 + (GRID_SIZE * (SLOT_SIZE + SLOT_PADDING) - titleSize.X) / 2,
            _position.Y + 15
        );
        spriteBatch.DrawString(font, title, titlePos + new Vector2(2, 2), new Color(0, 0, 0, 150));
        spriteBatch.DrawString(font, title, titlePos, new Color(220, 200, 150));
        
        // Grid çiz
        int gridStartX = (int)_position.X + 20;
        int gridStartY = (int)_position.Y + 50;
        
        for (int y = 0; y < GRID_SIZE; y++)
        {
            for (int x = 0; x < GRID_SIZE; x++)
            {
                Rectangle slotRect = new Rectangle(
                    gridStartX + x * (SLOT_SIZE + SLOT_PADDING),
                    gridStartY + y * (SLOT_SIZE + SLOT_PADDING),
                    SLOT_SIZE,
                    SLOT_SIZE
                );
                
                bool isHovered = _hoveredSlot.X == x && _hoveredSlot.Y == y;
                bool isSelected = _selectedSlot == _slots[_currentPage, y, x] && !_isFromEquipment;
                
                // Slot arka planı
                Color slotTint = isSelected ? new Color(100, 255, 100) : Color.White;
                spriteBatch.Draw(
                    isHovered || isSelected ? _slotHoverTexture : _slotTexture,
                    slotRect,
                    slotTint
                );
                
                // Item ikonu
                var slot = _slots[_currentPage, y, x];
                if (!slot.IsEmpty && slot.Item?.Icon != null)
                {
                    Rectangle iconRect = new Rectangle(
                        slotRect.X + 5,
                        slotRect.Y + 5,
                        SLOT_SIZE - 10,
                        SLOT_SIZE - 10
                    );
                    spriteBatch.Draw(slot.Item.Icon, iconRect, slot.Item.GetRarityColor());
                    
                    // Miktar Yazısı
                    if (slot.Quantity > 1)
                    {
                        string qtyText = slot.Quantity.ToString();
                        Vector2 qtySize = font.MeasureString(qtyText);
                        Vector2 qtyPos = new Vector2(
                            slotRect.Right - qtySize.X * 0.7f - 2, 
                            slotRect.Bottom - qtySize.Y * 0.7f - 2
                        );
                        spriteBatch.DrawString(font, qtyText, qtyPos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                        spriteBatch.DrawString(font, qtyText, qtyPos, Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    }
                }
            }
        }
        
        // Equipment Panel başlığı
        string equipTitle = "ESYA";
        Vector2 equipTitlePos = new Vector2(_weaponSlotRect.X - 5, _position.Y + 15);
        spriteBatch.DrawString(font, equipTitle, equipTitlePos + new Vector2(1, 1), new Color(0, 0, 0, 150));
        spriteBatch.DrawString(font, equipTitle, equipTitlePos, new Color(180, 160, 120));
        
        // Weapon slot
        DrawEquipmentSlot(spriteBatch, _weaponSlotRect, WeaponSlot, _weaponSlotIcon, 
            _hoveredEquipSlot == 0, _selectedSlot == WeaponSlot);
        
        // Armor slot
        DrawEquipmentSlot(spriteBatch, _armorSlotRect, ArmorSlot, _armorSlotIcon,
            _hoveredEquipSlot == 1, _selectedSlot == ArmorSlot);
        
        // Shield slot
        DrawEquipmentSlot(spriteBatch, _shieldSlotRect, ShieldSlot, _shieldSlotIcon,
            _hoveredEquipSlot == 2, _selectedSlot == ShieldSlot);
        
        // Helmet slot
        DrawEquipmentSlot(spriteBatch, _helmetSlotRect, HelmetSlot, _helmetSlotIcon,
            _hoveredEquipSlot == 3, _selectedSlot == HelmetSlot);
        
        // Slot etiketleri
        spriteBatch.DrawString(font, "Silah", new Vector2(_weaponSlotRect.X, _weaponSlotRect.Bottom + 2), 
            new Color(150, 150, 150), 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, "Zirh", new Vector2(_armorSlotRect.X, _armorSlotRect.Bottom + 2), 
            new Color(150, 150, 150), 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, "Kalkan", new Vector2(_shieldSlotRect.X, _shieldSlotRect.Bottom + 2), 
            new Color(150, 150, 150), 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, "Kask", new Vector2(_helmetSlotRect.X, _helmetSlotRect.Bottom + 2), 
            new Color(150, 150, 150), 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        
        // Sayfa göstergesi
        string pageText = $"Sayfa {_currentPage + 1}/{PAGE_COUNT}";
        Vector2 pageTextSize = font.MeasureString(pageText);
        Vector2 pageTextPos = new Vector2(
            _position.X + 20 + (GRID_SIZE * (SLOT_SIZE + SLOT_PADDING) - pageTextSize.X) / 2,
            _position.Y + _bounds.Height - 40
        );
        spriteBatch.DrawString(font, pageText, pageTextPos + new Vector2(1, 1), new Color(0, 0, 0, 150));
        spriteBatch.DrawString(font, pageText, pageTextPos, Color.White);
        
        // Sayfa butonları
        Color prevColor = _hoveringPrev ? new Color(255, 220, 100) : Color.White;
        Color nextColor = _hoveringNext ? new Color(255, 220, 100) : Color.White;
        
        if (_currentPage > 0)
        {
            spriteBatch.Draw(_pageButtonTexture, _prevPageButton, prevColor);
            DrawArrow(spriteBatch, _prevPageButton, true, prevColor);
        }
        
        if (_currentPage < PAGE_COUNT - 1)
        {
            spriteBatch.Draw(_pageButtonTexture, _nextPageButton, nextColor);
            DrawArrow(spriteBatch, _nextPageButton, false, nextColor);
        }
        
        // Kontrol ipuçları
        string hint = "[E] Kapat  [Sag Tik] Giy/Kullan/Cikar";
        Vector2 hintPos = new Vector2(_position.X + 20, _position.Y + _bounds.Height - 18);
        spriteBatch.DrawString(font, hint, hintPos, new Color(150, 150, 150), 
            0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        
        // Tooltip çiz
        if (_tooltipItem != null)
        {
            DrawTooltip(spriteBatch, font, _tooltipItem, _tooltipPosition);
        }
    }
    
    private void DrawEquipmentSlot(SpriteBatch spriteBatch, Rectangle rect, InventorySlot slot, 
        Texture2D emptyIcon, bool isHovered, bool isSelected)
    {
        // Arka plan
        Color bgTint = isSelected ? new Color(100, 255, 100) : (isHovered ? new Color(255, 220, 150) : Color.White);
        spriteBatch.Draw(_equipSlotTexture, rect, bgTint);
        
        if (slot.IsEmpty)
        {
            // Boş slot ikonu
            Rectangle iconRect = new Rectangle(rect.X + 5, rect.Y + 5, 50, 50);
            spriteBatch.Draw(emptyIcon, iconRect, new Color(255, 255, 255, 100));
        }
        else if (slot.Item?.Icon != null)
        {
            // Item ikonu
            Rectangle iconRect = new Rectangle(rect.X + 10, rect.Y + 10, 40, 40);
            spriteBatch.Draw(slot.Item.Icon, iconRect, slot.Item.GetRarityColor());
        }
    }
    
    private void DrawTooltip(SpriteBatch spriteBatch, SpriteFont font, Item item, Vector2 position)
    {
        // Tooltip içeriği
        string name = item.GetDisplayName();
        string type = item.Type switch
        {
            ItemType.Weapon => "Silah",
            ItemType.Armor => "Zirh",
            ItemType.Shield => "Kalkan",
            ItemType.Helmet => "Kask",
            ItemType.Material => "Malzeme",
            _ => "Esya"
        };
        string level = item.RequiredLevel > 0 ? $"Gerekli Seviye: {item.RequiredLevel}" : "";
        
        string stats = "";
        if (item.Type == ItemType.Weapon)
        {
            stats = $"Hasar: {item.MinDamage}-{item.MaxDamage}\nSaldiri Hizi: {item.AttackSpeed}";
        }
        else if (item.Type == ItemType.Armor)
        {
            stats = $"Savunma: +{item.Defense}\nCan: +{item.Health}";
        }
        else if (item.Type == ItemType.Shield)
        {
            stats = $"Savunma: +{item.Defense}\nBloklama: %{item.BlockChance}";
        }
        else if (item.Type == ItemType.Helmet)
        {
            stats = $"Savunma: +{item.Defense}\nCan: +{item.Health}";
        }
        
        // Fiyat bilgisi
        string priceInfo = $"Satis: {item.SellPrice}G | Alis: {item.BuyPrice}G";
        
        // Boyut hesapla
        float scale = 0.8f;
        Vector2 nameSize = font.MeasureString(name) * scale;
        Vector2 typeSize = font.MeasureString(type) * scale;
        Vector2 levelSize = level.Length > 0 ? font.MeasureString(level) * scale : Vector2.Zero;
        Vector2 statsSize = stats.Length > 0 ? font.MeasureString(stats) * scale : Vector2.Zero;
        Vector2 priceSize = font.MeasureString(priceInfo) * scale;
        
        float maxWidth = Math.Max(Math.Max(nameSize.X, typeSize.X), Math.Max(Math.Max(levelSize.X, statsSize.X), priceSize.X));
        float totalHeight = nameSize.Y + typeSize.Y + (level.Length > 0 ? levelSize.Y + 5 : 0) + (stats.Length > 0 ? statsSize.Y + 5 : 0) + priceSize.Y + 20;
        
        // Ekran sınırlarını kontrol et
        if (position.X + maxWidth + 20 > _screenWidth)
            position.X = _screenWidth - maxWidth - 25;
        if (position.Y + totalHeight > _screenHeight)
            position.Y = _screenHeight - totalHeight - 10;
        
        // Arka plan
        Rectangle bgRect = new Rectangle(
            (int)position.X - 5,
            (int)position.Y - 5,
            (int)maxWidth + 20,
            (int)totalHeight + 10
        );
        
        spriteBatch.Draw(_backgroundTexture, bgRect, new Color(20, 22, 30, 240));
        
        // Kenarlık
        spriteBatch.Draw(_backgroundTexture, new Rectangle(bgRect.X, bgRect.Y, bgRect.Width, 1), new Color(80, 80, 100));
        spriteBatch.Draw(_backgroundTexture, new Rectangle(bgRect.X, bgRect.Y, 1, bgRect.Height), new Color(80, 80, 100));
        spriteBatch.Draw(_backgroundTexture, new Rectangle(bgRect.Right - 1, bgRect.Y, 1, bgRect.Height), new Color(50, 50, 70));
        spriteBatch.Draw(_backgroundTexture, new Rectangle(bgRect.X, bgRect.Bottom - 1, bgRect.Width, 1), new Color(50, 50, 70));
        
        // İsim (rarity rengiyle)
        float yOffset = 0;
        spriteBatch.DrawString(font, name, position + new Vector2(0, yOffset), item.GetRarityColor(),
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        yOffset += nameSize.Y + 5;
        
        // Tip
        spriteBatch.DrawString(font, type, position + new Vector2(0, yOffset), new Color(150, 150, 150),
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        yOffset += typeSize.Y + 5;
        
        // Seviye (varsa)
        if (level.Length > 0)
        {
            spriteBatch.DrawString(font, level, position + new Vector2(0, yOffset), new Color(255, 200, 100),
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            yOffset += levelSize.Y + 5;
        }
        
        // İstatistikler (varsa)
        if (stats.Length > 0)
        {
            spriteBatch.DrawString(font, stats, position + new Vector2(0, yOffset), new Color(100, 200, 100),
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            yOffset += statsSize.Y + 5;
        }
        
        // Fiyat bilgisi
        spriteBatch.DrawString(font, priceInfo, position + new Vector2(0, yOffset), Color.Gold,
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
    
    private void DrawPanel(SpriteBatch spriteBatch, Rectangle bounds)
    {
        // Gölge
        spriteBatch.Draw(_backgroundTexture, 
            new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width, bounds.Height),
            new Color(0, 0, 0, 100));
        
        // Ana panel
        spriteBatch.Draw(_backgroundTexture, bounds, new Color(30, 32, 45));
        
        // Üst gradient
        for (int i = 0; i < 3; i++)
        {
            spriteBatch.Draw(_backgroundTexture,
                new Rectangle(bounds.X, bounds.Y + i, bounds.Width, 1),
                new Color(60 - i * 10, 62 - i * 10, 80 - i * 10));
        }
        
        // Kenarlık
        spriteBatch.Draw(_backgroundTexture, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), new Color(70, 70, 90));
        spriteBatch.Draw(_backgroundTexture, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), new Color(70, 70, 90));
        spriteBatch.Draw(_backgroundTexture, new Rectangle(bounds.X + bounds.Width - 2, bounds.Y, 2, bounds.Height), new Color(50, 50, 70));
        spriteBatch.Draw(_backgroundTexture, new Rectangle(bounds.X, bounds.Y + bounds.Height - 2, bounds.Width, 2), new Color(50, 50, 70));
    }
    
    private void DrawArrow(SpriteBatch spriteBatch, Rectangle buttonRect, bool left, Color color)
    {
        int centerX = buttonRect.X + buttonRect.Width / 2;
        int centerY = buttonRect.Y + buttonRect.Height / 2;
        int arrowSize = 6;
        
        if (left)
        {
            for (int i = 0; i < 3; i++)
            {
                spriteBatch.Draw(_backgroundTexture,
                    new Rectangle(centerX - arrowSize + i + 2, centerY - arrowSize + i, 2, 2), color);
                spriteBatch.Draw(_backgroundTexture,
                    new Rectangle(centerX - arrowSize + i + 2, centerY + arrowSize - i - 2, 2, 2), color);
            }
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                spriteBatch.Draw(_backgroundTexture,
                    new Rectangle(centerX + arrowSize - i - 4, centerY - arrowSize + i, 2, 2), color);
                spriteBatch.Draw(_backgroundTexture,
                    new Rectangle(centerX + arrowSize - i - 4, centerY + arrowSize - i - 2, 2, 2), color);
            }
        }
    }
    
    public bool AddItem(int itemId, int quantity = 1)
    {
        Item item = ItemDatabase.GetItem(itemId);
        if (item == null) return false;
        
        // 1. Önce mevcutları kontrol et (Stacking)
        // Sadece Materials (Malzemeler) ve +0 items birleşebilir. Ekipmanlar birleşmez.
        if (item.Type == ItemType.Material && item.EnhancementLevel == 0)
        {
            for (int p = 0; p < PAGE_COUNT; p++)
            {
                for (int y = 0; y < GRID_SIZE; y++)
                {
                    for (int x = 0; x < GRID_SIZE; x++)
                    {
                        if (!_slots[p, y, x].IsEmpty 
                            && _slots[p, y, x].Item.Id == itemId 
                            && _slots[p, y, x].Item.EnhancementLevel == 0)
                        {
                            _slots[p, y, x].Quantity += quantity;
                            return true;
                        }
                    }
                }
            }
        }
        
        // 2. Boş slot bul
        for (int p = 0; p < PAGE_COUNT; p++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                for (int x = 0; x < GRID_SIZE; x++)
                {
                    if (_slots[p, y, x].IsEmpty)
                    {
                        _slots[p, y, x].Item = item;
                        _slots[p, y, x].Quantity = quantity;
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    public Item GetEquippedWeapon()
    {
        return WeaponSlot.Item;
    }
    
    public Item GetEquippedArmor()
    {
        return ArmorSlot.Item;
    }
    

    
    // Sadece slotları temizler (ekipman hariç) - Opsiyonel
    public void ClearGrid()
    {
        for (int p = 0; p < PAGE_COUNT; p++)
            for (int y = 0; y < GRID_SIZE; y++)
                for (int x = 0; x < GRID_SIZE; x++)
                    _slots[p, y, x].Clear();
    }
    
    // --- HELPER METODLAR ---
    public int GetItemCount(int itemId)
    {
        int count = 0;
        for (int p = 0; p < PAGE_COUNT; p++)
            for (int y = 0; y < GRID_SIZE; y++)
                for (int x = 0; x < GRID_SIZE; x++)
                    if (!_slots[p, y, x].IsEmpty && _slots[p, y, x].Item.Id == itemId)
                        count += _slots[p, y, x].Quantity;
        return count;
    }
    
    // KAYIT SİSTEMİ İÇİN HELPERLAR
    public List<SavedItem> GetItemsForSave()
    {
        List<SavedItem> items = new List<SavedItem>();
        for (int p = 0; p < PAGE_COUNT; p++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                for (int x = 0; x < GRID_SIZE; x++)
                {
                    if (!_slots[p, y, x].IsEmpty)
                    {
                        items.Add(new SavedItem 
                        { 
                            ItemId = _slots[p, y, x].Item.Id,
                            Quantity = _slots[p, y, x].Quantity,
                            EnhancementLevel = _slots[p, y, x].Item.EnhancementLevel
                        });
                    }
                }
            }
        }
        return items;
    }
    
    public void LoadItems(List<SavedItem> items)
    {
        // Önce temizle
        ClearGrid();
                    
        WeaponSlot.Clear();
        ArmorSlot.Clear();
        
        // Eşyaları ekle
        if (items != null)
        {
            foreach(var savedItem in items)
            {
                Item item = ItemDatabase.GetItem(savedItem.ItemId);
                if (item != null)
                {
                    // Seviyeyi geri yükle
                    for(int i=0; i < savedItem.EnhancementLevel; i++)
                    {
                        item.UpgradeSuccess();
                    }
                    
                    AddItemObject(item, savedItem.Quantity);
                }
            }
        }
    }
    
    // Nesne olarak item ekle
    private void AddItemObject(Item item, int quantity)
    {
         // Boş slot bul
        for (int p = 0; p < PAGE_COUNT; p++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                for (int x = 0; x < GRID_SIZE; x++)
                {
                    if (_slots[p, y, x].IsEmpty)
                    {
                        _slots[p, y, x].Item = item;
                        _slots[p, y, x].Quantity = quantity;
                        return;
                    }
                }
            }
        }
    }
    
    // Nesne referansına göre item sil (Destroy)
    public void RemoveItemInstance(Item item)
    {
        // Grid kontrol
        for (int p = 0; p < PAGE_COUNT; p++)
            for (int y = 0; y < GRID_SIZE; y++)
                for (int x = 0; x < GRID_SIZE; x++)
                    if (!_slots[p, y, x].IsEmpty && _slots[p, y, x].Item == item)
                    {
                        _slots[p, y, x].Clear();
                        return;
                    }
        
        // Ekipman kontrol
        if (WeaponSlot.Item == item) WeaponSlot.Clear();
        if (ArmorSlot.Item == item) ArmorSlot.Clear();
    }
    
    public void RemoveItem(int itemId, int amount)
    {
        int remaining = amount;
        for (int p = 0; p < PAGE_COUNT; p++)
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                for (int x = 0; x < GRID_SIZE; x++)
                {
                    if (remaining <= 0) return;
                    
                    var slot = _slots[p, y, x];
                    if (!slot.IsEmpty && slot.Item.Id == itemId)
                    {
                        if (slot.Quantity > remaining)
                        {
                            slot.Quantity -= remaining;
                            remaining = 0;
                        }
                        else
                        {
                            remaining -= slot.Quantity;
                            slot.Clear();
                        }
                    }
                }
            }
        }
    }
}
