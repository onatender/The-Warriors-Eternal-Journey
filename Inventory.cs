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
        _player?.StopMoving();
    }
    
    public void Close()
    {
        if (_isDragging) CancelDrag();
        IsOpen = false;
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
    private Texture2D _pixelTexture;
    
    // SFX
    private SoundEffect _sfxCoinPickup;
    private SoundEffect _sfxCoinBuy;
    private SoundEffect _sfxCoinSell;
    private SoundEffect _sfxCoinDrop;
    
    private SoundEffect _sfxItemPickup;
    private SoundEffect _sfxItemEquip;
    
    public void SetCoinSounds(SoundEffect pickup, SoundEffect buy, SoundEffect sell, SoundEffect drop)
    {
        _sfxCoinPickup = pickup;
        _sfxCoinBuy = buy;
        _sfxCoinSell = sell;
        _sfxCoinDrop = drop;
    }
    
    public void SetItemSounds(SoundEffect pickup, SoundEffect equip)
    {
        _sfxItemPickup = pickup;
        _sfxItemEquip = equip;
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
    // private int _selectedEquipIndex = -1; // Unused
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
    private float _animationTimer;
    private Random _rng = new Random();
    
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
    
    public Inventory(GraphicsDevice graphicsDevice, Microsoft.Xna.Framework.Content.ContentManager content, int screenWidth, int screenHeight)
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
        ItemDatabase.Initialize(graphicsDevice, content);
        
        // Texture'ları oluştur
        CreateTextures(graphicsDevice, content);
        
        // Pozisyonu hesapla (ekranın ortası)
        CalculatePosition();
        
        // Başlangıç item'ı ekle - Artık TitleScreen/Game1.cs start kısmında yapılıyor.
        // AddItem(1, 1);
    }
    
    private void CreateTextures(GraphicsDevice graphicsDevice, Microsoft.Xna.Framework.Content.ContentManager content)
    {
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

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
        
        // Weapon slot icon (kılıç silüeti) - Real sprite if possible
        try { _weaponSlotIcon = content.Load<Texture2D>("icons/sword_1"); } catch { _weaponSlotIcon = CreateWeaponSlotIcon(graphicsDevice); }
        
        // Armor slot icon (zırh silüeti)
        try { _armorSlotIcon = content.Load<Texture2D>("icons/chestplate_1"); } catch { _armorSlotIcon = CreateArmorSlotIcon(graphicsDevice); }
        
        // Shield slot icon (kalkan silüeti)
        try { _shieldSlotIcon = content.Load<Texture2D>("icons/shield_1"); } catch { _shieldSlotIcon = CreateShieldSlotIcon(graphicsDevice); }
        
        // Helmet slot icon (kask silüeti)
        try { _helmetSlotIcon = content.Load<Texture2D>("icons/helmet_1"); } catch { _helmetSlotIcon = CreateHelmetSlotIcon(graphicsDevice); }
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
        _animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        // I tuşu ile aç/kapa
        if (currentKeyState.IsKeyDown(Keys.I) && !_previousKeyState.IsKeyDown(Keys.I))
        {
            if (IsOpen) Close();
            else Open();
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
            
        // Mouse tıklama (Basıldı)
        if (currentMouseState.LeftButton == ButtonState.Pressed && 
            _previousMouseState.LeftButton == ButtonState.Released)
        {
            HandleLeftClick(mousePos);
        }
        // Mouse bırakma (Released) - Drag bitir
        else if (currentMouseState.LeftButton == ButtonState.Released && 
                 _previousMouseState.LeftButton == ButtonState.Pressed)
        {
            if (_isDragging) EndDrag(mousePos);
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
    
    // Drag & Drop
    private bool _isDragging = false;
    private InventorySlot _dragSourceSlot = null; // Nereden aldık?
    private Item _dragItem = null; // Ne taşıyoruz?
    private bool _dragFromEquip = false; // Ekipmandan mı?
    private int _dragEquipIndex = -1; 
    
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

        // --- DRAG START ---
        // Eğer hiçbir şey taşımıyorsak, tıklanan yerdeki itemi al
        if (!_isDragging)
        {
            // Grid slotu mu?
            if (_hoveredSlot.X >= 0)
            {
                var slot = _slots[_currentPage, _hoveredSlot.Y, _hoveredSlot.X];
                if (!slot.IsEmpty)
                {
                    BeginDrag(slot, false, -1);
                }
            }
            // Equipment slotu mu?
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
                if (slot != null && !slot.IsEmpty)
                {
                    BeginDrag(slot, true, _hoveredEquipSlot);
                }
            }
        }
    }
    
    private void BeginDrag(InventorySlot source, bool isEquip, int equipIndex)
    {
        _sfxItemPickup?.Play(0.6f, 0.0f, 0.0f);
        _isDragging = true;
        _dragSourceSlot = source;
        _dragItem = source.Item;
        _dragFromEquip = isEquip;
        _dragEquipIndex = equipIndex;
        
        // Slotu görsel olarak boşaltmıyoruz, referans kalsın ama çizimde drag item'ı ayrıca çizeceğiz
        // Ancak genellikle inventory mantığında, drag edilen item slottan 'kalkar'.
        // Basitlik için: Slot'u geçici olarak boşaltalım, drop olmazsa geri koyarız.
        // Veya slotta dursun, ama şeffaf çizilsin? -> Slot boşalsın daha iyi.
        source.Item = null; // Itemı elimize aldık
        // Quantity? Eğer stackable ise hepsi gelir.
    }
    
    private void EndDrag(Point mousePos)
    {
        if (!_isDragging) return;
        
        bool handled = false;
        
        // Mouse nerede bırakıldı?
        
        // 1. Grid Slot Üzerine Bırakıldı
        if (_hoveredSlot.X >= 0)
        {
            var targetSlot = _slots[_currentPage, _hoveredSlot.Y, _hoveredSlot.X];
            
            // --- ENHANCEMENT STONE LOGIC (GRID) ---
            if (_dragItem.Id == 99 && !targetSlot.IsEmpty)
            {
               // Eğer hedef item geliştirilebilir bir eşya ise
               if (targetSlot.Item.Type == ItemType.Weapon || 
                   targetSlot.Item.Type == ItemType.Armor || 
                   targetSlot.Item.Type == ItemType.Shield || 
                   targetSlot.Item.Type == ItemType.Helmet)
               {
                   OnEnhancementTargetSelected?.Invoke(targetSlot.Item);
                   CancelDrag();
                   return;
               }
            }
            // -------------------------------------
            
            // Hedef boş mu?
            if (targetSlot.IsEmpty)
            {
                // Direkt koy
                targetSlot.Item = _dragItem;
                targetSlot.Quantity = _dragSourceSlot.Quantity; 
                if (targetSlot != _dragSourceSlot) _dragSourceSlot.Quantity = 0; // BUG FIX: Don't zero if self-drop
                
                // Eğer ekipmandan geldiyse event fırlat (çıkartıldı)
                if (_dragFromEquip) FireEquipEvent(_dragEquipIndex, null);
                
                handled = true;
            }
            else
            {
                // Dolu, yer değiştir (Swap) veya Stackle
                
                // Stackleme Logic
                if (targetSlot.Item.Id == _dragItem.Id && 
                   (targetSlot.Item.Type == ItemType.Material || targetSlot.Item.Type == ItemType.Consumable) &&
                   targetSlot.Item.EnhancementLevel == 0)
                {
                    targetSlot.Quantity += _dragSourceSlot.Quantity;
                    if (targetSlot != _dragSourceSlot) _dragSourceSlot.Quantity = 0; // BUG FIX
                    
                    if (_dragFromEquip) FireEquipEvent(_dragEquipIndex, null);
                    handled = true;
                }
                else
                {
                    // Swap Logic
                    Item temp = targetSlot.Item;
                    int tempQty = targetSlot.Quantity;
                    
                    bool canSwap = true;
                    
                    // Eğer kaynak Equip ise, hedefteki item oraya geri dönebilir mi?
                    if (_dragFromEquip)
                    {
                        if (!CanEquipItem(temp, _dragEquipIndex)) canSwap = false;
                    }
                    
                    if (canSwap)
                    {
                        targetSlot.Item = _dragItem;
                        targetSlot.Quantity = _dragSourceSlot.Quantity; // Kaynaktaki miktarı koru
                        
                        _dragSourceSlot.Item = temp;
                        _dragSourceSlot.Quantity = tempQty;
                        
                        if (_dragFromEquip) FireEquipEvent(_dragEquipIndex, temp);
                        handled = true;
                    }
                }
            }
        }
        // 2. Equipment Slot Üzerine Bırakıldı
        else if (_hoveredEquipSlot >= 0)
        {
            // Hedef slot
             InventorySlot targetSlot = _hoveredEquipSlot switch
            {
                0 => WeaponSlot,
                1 => ArmorSlot,
                2 => ShieldSlot,
                3 => HelmetSlot,
                _ => null
            };
            
            // --- GÜÇLENDİRME TAŞI KONTROLÜ ---
            // Eğer elimizdeki item bir "Enhancement Stone" (ID 99) ise ve bir ekipmanın üzerine bırakıyorsak
            if (_dragItem.Id == 99 && targetSlot != null && !targetSlot.IsEmpty)
            {
                // Yükseltme penceresini aç
                OnEnhancementTargetSelected?.Invoke(targetSlot.Item);
                
                // Taşı geri yerine koy (işlem bitmedi, UI açıldı sadece)
                CancelDrag();
                return;
            }
            // ----------------------------------
            
            // Normal Equip Logic
            if (CanEquipItem(_dragItem, _hoveredEquipSlot))
            {
                if (targetSlot.IsEmpty)
                {
                    targetSlot.Item = _dragItem;
                    targetSlot.Quantity = _dragSourceSlot.Quantity;
                    _dragSourceSlot.Quantity = 0;
                    
                    FireEquipEvent(_hoveredEquipSlot, _dragItem);
                    if (_dragFromEquip) FireEquipEvent(_dragEquipIndex, null); // Eski yer boşaldı
                    _sfxItemEquip?.Play(0.6f, 0.0f, 0.0f);
                    handled = true;
                }
                else
                {
                    // Swap
                    Item temp = targetSlot.Item;
                    int tempQty = targetSlot.Quantity;
                    
                    // Hedefteki item kaynağa gidebilir mi? (Kaynak Equip ise kontrol lazım)
                    bool canSwap = true;
                    if (_dragFromEquip)
                    {
                         if (!CanEquipItem(temp, _dragEquipIndex)) canSwap = false;
                    }
                    
                    if (canSwap)
                    {
                        targetSlot.Item = _dragItem;
                        targetSlot.Quantity = _dragSourceSlot.Quantity;
                        
                        _dragSourceSlot.Item = temp;
                        _dragSourceSlot.Quantity = tempQty;
                        
                        FireEquipEvent(_hoveredEquipSlot, _dragItem);
                        if (_dragFromEquip) FireEquipEvent(_dragEquipIndex, temp);
                        _sfxItemEquip?.Play(0.6f, 0.0f, 0.0f);
                        
                        handled = true;
                    }
                }
            }
        }
        
        if (!handled)
        {
            CancelDrag();
        }
        
        _isDragging = false;
        _dragSourceSlot = null;
        _dragItem = null;
    }
    
    private void CancelDrag()
    {
        // İptal, geri koy
        if (_isDragging && _dragSourceSlot != null && _dragItem != null)
        {
            _dragSourceSlot.Item = _dragItem;
        }

        // State'i temizle
        _isDragging = false;
        _dragSourceSlot = null;
        _dragItem = null;
    }
    
    private bool CanEquipItem(Item item, int slotIndex)
    {
        if (item == null) return false;
        if (slotIndex == 0 && item.Type == ItemType.Weapon) return true;
        if (slotIndex == 1 && item.Type == ItemType.Armor) return true;
        if (slotIndex == 2 && item.Type == ItemType.Shield) return true;
        if (slotIndex == 3 && item.Type == ItemType.Helmet) return true;
        return false;
    }
    
    private void FireEquipEvent(int slotIndex, Item item)
    {
        if (slotIndex == 0) OnWeaponEquipped?.Invoke(item);
        else if (slotIndex == 1) OnArmorEquipped?.Invoke(item);
        else if (slotIndex == 2) OnShieldEquipped?.Invoke(item);
        else if (slotIndex == 3) OnHelmetEquipped?.Invoke(item);
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
                    _sfxItemEquip?.Play(0.6f, 0.0f, 0.0f);
                }
                else if (slot.Item.Type == ItemType.Armor)
                {
                    // Zırhı giy
                    SwapOrMove(slot, ArmorSlot);
                    OnArmorEquipped?.Invoke(ArmorSlot.Item);
                    _sfxItemEquip?.Play(0.6f, 0.0f, 0.0f);
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
                    _sfxItemEquip?.Play(0.6f, 0.0f, 0.0f);
                }
                else if (slot.Item.Type == ItemType.Helmet)
                {
                    // Kaskı giy
                    SwapOrMove(slot, HelmetSlot);
                    OnHelmetEquipped?.Invoke(HelmetSlot.Item);
                    _sfxItemEquip?.Play(0.6f, 0.0f, 0.0f);
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
                    spriteBatch.Draw(slot.Item.Icon, iconRect, slot.Item.GetTintColor());
                    
                    // İlahi Efekt (ID 32: Tılsım, ID 10: Ebedi Kılıç)
                    if (slot.Item.Id == 32 || slot.Item.Id == 10)
                    {
                        DrawDivineEffect(spriteBatch, iconRect);
                    }
                    
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
        if (_tooltipItem != null && !_isDragging) // Drag yaparken tooltip gizle
        {
            DrawTooltip(spriteBatch, font, _tooltipItem, _tooltipPosition);
        }
        
        // Drag Item Çiz (En Üstte)
        if (_isDragging && _dragItem != null)
        {
             Vector2 mousePos = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
             // Mouse ortalansın (25, 25 offset)
             Rectangle dragRect = new Rectangle((int)mousePos.X - 25, (int)mousePos.Y - 25, 50, 50);
             spriteBatch.Draw(_dragItem.Icon, dragRect, _dragItem.GetTintColor() * 0.9f);
             
             // Miktar
             if (_dragSourceSlot != null && _dragSourceSlot.Quantity > 1)
             {
                 spriteBatch.DrawString(font, _dragSourceSlot.Quantity.ToString(), mousePos + new Vector2(10, 10), Color.White);
             }
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
            // Boş slot ikonu - Tinted ghost effect
            Rectangle iconRect = new Rectangle(rect.X + 5, rect.Y + 5, 50, 50);
            spriteBatch.Draw(emptyIcon, iconRect, new Color(10, 10, 10, 140));
        }
        else if (slot.Item?.Icon != null)
        {
            // Item ikonu
            Rectangle iconRect = new Rectangle(rect.X + 10, rect.Y + 10, 40, 40);
            spriteBatch.Draw(slot.Item.Icon, iconRect, slot.Item.GetTintColor());
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
        
        // Açıklama Metni
        string description = item.Description ?? "";
        List<string> descLines = new List<string>();
        float maxDescWidth = 250f;
        
        if (!string.IsNullOrEmpty(description))
        {
            string[] words = description.Split(' ');
            string currentLine = "";
            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                if (font.MeasureString(testLine).X * 0.7f > maxDescWidth)
                {
                    descLines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }
            if (!string.IsNullOrEmpty(currentLine)) descLines.Add(currentLine);
        }

        // Boyut hesapla
        float scale = 0.8f;
        float descScale = 0.7f;
        Vector2 nameSize = font.MeasureString(name) * scale;
        Vector2 typeSize = font.MeasureString(type) * scale;
        Vector2 levelSize = level.Length > 0 ? font.MeasureString(level) * scale : Vector2.Zero;
        Vector2 statsSize = stats.Length > 0 ? font.MeasureString(stats) * scale : Vector2.Zero;
        Vector2 priceSize = font.MeasureString(priceInfo) * scale;
        
        float descHeight = descLines.Count * (font.LineSpacing * 0.75f);
        
        float maxWidth = Math.Max(Math.Max(nameSize.X, typeSize.X), Math.Max(Math.Max(levelSize.X, statsSize.X), priceSize.X));
        if(descLines.Count > 0) maxWidth = Math.Max(maxWidth, maxDescWidth);
        
        float totalHeight = nameSize.Y + typeSize.Y + (level.Length > 0 ? levelSize.Y + 5 : 0) + (stats.Length > 0 ? statsSize.Y + 5 : 0) 
            + (descLines.Count > 0 ? descHeight + 10 : 0) + priceSize.Y + 20;
        
        // Ekran sınırlarını kontrol et
        if (position.X + maxWidth + 20 > _screenWidth)
        {
             position.X = _screenWidth - maxWidth - 25;
        }
            
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
        
        // Açıklama Yazısı
        if (descLines.Count > 0)
        {
            yOffset += 5;
            foreach (var line in descLines)
            {
                spriteBatch.DrawString(font, line, position + new Vector2(0, yOffset), new Color(200, 200, 200),
                    0f, Vector2.Zero, descScale, SpriteEffects.None, 0f);
                yOffset += font.LineSpacing * 0.75f;
            }
            yOffset += 5;
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
        // Sadece Materials (Malzemeler) ve Consumables (Tüketilebilir) birleşebilir.
        if ((item.Type == ItemType.Material || item.Type == ItemType.Consumable) && item.EnhancementLevel == 0)
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

    private void DrawDivineEffect(SpriteBatch sb, Rectangle rect)
    {
        float time = _animationTimer;
        Vector2 center = rect.Center.ToVector2();
        
        // 1. Çakan Yıldırımlar
        // Her 0.6 saniyede bir kısa süreli şimşek çaksın
        float phase = time % 0.6f;
        if (phase < 0.12f)
        {
            Random rnd = new Random((int)(time * 15f) + rect.X + rect.Y);
            Vector2 p1 = new Vector2(rect.X + rnd.Next(5, rect.Width - 5), rect.Y + 5);
            Vector2 p2 = p1 + new Vector2(rnd.Next(-12, 13), rnd.Next(10, 18));
            Vector2 p3 = p2 + new Vector2(rnd.Next(-12, 13), rnd.Next(10, 18));
            
            Color strikeCol = rnd.Next(2) == 0 ? Color.White : Color.LightCyan;
            float strikeAlpha = 0.8f * (1.0f - (phase / 0.12f)); // Fade out during flash
            
            DrawLine(sb, p1, p2, strikeCol * strikeAlpha, 2);
            DrawLine(sb, p2, p3, strikeCol * strikeAlpha, 2);
            
            // Küçük kıvılcımlar
            for(int i=0; i<3; i++) {
                sb.Draw(_pixelTexture, new Rectangle((int)p3.X + rnd.Next(-5,6), (int)p3.Y + rnd.Next(-5,6), 2, 2), Color.Gold * strikeAlpha);
            }
        }
        
        // 3. Kutsal Parçacıklar (Daha kısıtlı ve yukarı süzülen)
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
