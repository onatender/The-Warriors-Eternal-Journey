using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace EternalJourney;

public class ShopUI
{
    public bool IsOpen { get; private set; } = false;
    
    private Texture2D _backgroundTexture;
    private Texture2D _slotTexture;
    
    private string FormatPrice(int price)
    {
        if (price >= 1000000)
            return (price / 1000000f).ToString("0.##") + "M";
        if (price >= 1000)
            return (price / 1000f).ToString("0.##") + "K";
        return price.ToString();
    }
    
    private int _screenWidth;
    private int _screenHeight;
    
    // Panel pozisyonları
    private Rectangle _playerPanelBounds;
    private Rectangle _shopPanelBounds;
    
    // Slot boyutları
    private const int SLOT_SIZE = 50;
    private const int SLOT_PADDING = 5;
    private const int GRID_COLS = 8; // Inventory ile aynı (8x8)
    private const int GRID_ROWS = 8;
    
    // Satıcının satılık item ID'leri
    private List<int> _shopItemIds = new List<int> { 1, 2, 3, 10, 11, 40, 41, 50, 51, 99, 25, 20, 21, 22, 30, 31, 32 };
    
    // Hover
    private int _hoveredPlayerSlot = -1; // Hangi oyuncu slotuna hover edildi
    private int _hoveredShopSlot = -1;   // Hangi satıcı slotuna hover edildi
    
    // Pagination
    private int _currentPlayerPage = 0;
    private const int MAX_PLAYER_PAGES = 4;
    private Rectangle _btnPrevPage;
    private Rectangle _btnNextPage;
    private bool _hoveringPrev = false;
    private bool _hoveringNext = false;
    
    // Tooltip
    private Item _tooltipItem = null;
    private Vector2 _tooltipPosition;
    private bool _showSellPrice = false; // Satış fiyatı mı alış fiyatı mı gösterilecek
    
    // Mevcut durumlar
    private Player _player;
    private Inventory _inventory;
    
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyState;
    
    // Scroll (Sadece shop için kalsın, player için sayfalama var)
    private int _shopScrollOffset = 0;
    private float _animationTimer = 0f;
    
    // SFX
    private SoundEffect _sfxBuy;
    private SoundEffect _sfxSell;
    
    private float _clickCooldown = 0f;
    
    public void SetCoinSounds(SoundEffect buy, SoundEffect sell)
    {
        _sfxBuy = buy;
        _sfxSell = sell;
    }

    public ShopUI(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        
        // Texture'lar
        _backgroundTexture = new Texture2D(graphicsDevice, 1, 1);
        _backgroundTexture.SetData(new[] { Color.White });
        
        _slotTexture = CreateSlotTexture(graphicsDevice);
        
        CalculatePosition();
    }
    
    private Texture2D CreateSlotTexture(GraphicsDevice graphicsDevice)
    {
        int size = SLOT_SIZE;
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] colors = new Color[size * size];
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                
                // Kenar
                if (x == 0 || y == 0 || x == size - 1 || y == size - 1)
                    colors[i] = new Color(60, 65, 80);
                // İç
                else
                    colors[i] = new Color(40, 45, 55);
            }
        }
        
        texture.SetData(colors);
        return texture;
    }
    
    private void CalculatePosition()
    {
        int panelWidth = GRID_COLS * (SLOT_SIZE + SLOT_PADDING) + 40;
        int panelHeight = GRID_ROWS * (SLOT_SIZE + SLOT_PADDING) + 100;
        int gap = 40;
        
        int totalWidth = panelWidth * 2 + gap;
        int startX = (_screenWidth - totalWidth) / 2;
        int startY = (_screenHeight - panelHeight) / 2;
        
        _playerPanelBounds = new Rectangle(startX, startY, panelWidth, panelHeight);
        _shopPanelBounds = new Rectangle(startX + panelWidth + gap, startY, panelWidth, panelHeight);
        
        // Pagination Buttons (Player panelinin altında)
        int btnSize = 30;
        int btnY = _playerPanelBounds.Bottom - 45;
        _btnPrevPage = new Rectangle(_playerPanelBounds.X + 20, btnY, btnSize, btnSize);
        _btnNextPage = new Rectangle(_playerPanelBounds.Right - 20 - btnSize, btnY, btnSize, btnSize);
    }
    
    public void Open(Player player, Inventory inventory)
    {
        _player = player;
        _inventory = inventory;
        IsOpen = true;
        _currentPlayerPage = 0;
        _shopScrollOffset = 0;
    }
    
    public void Close()
    {
        IsOpen = false;
    }
    
    public void Update(GameTime gameTime, KeyboardState currentKeyState, MouseState currentMouseState)
    {
        _animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (!IsOpen) return;
        
        // ESC veya F ile kapat
        if ((currentKeyState.IsKeyDown(Keys.Escape) && !_previousKeyState.IsKeyDown(Keys.Escape)) ||
            (currentKeyState.IsKeyDown(Keys.F) && !_previousKeyState.IsKeyDown(Keys.F)))
        {
            Close();
        }
        
        Point mousePos = currentMouseState.Position;
        
        // Hover kontrolü
        _hoveredPlayerSlot = -1;
        _hoveredShopSlot = -1;
        _tooltipItem = null;
        _hoveringPrev = _btnPrevPage.Contains(mousePos);
        _hoveringNext = _btnNextPage.Contains(mousePos);
        
        // Oyuncu slotları hover (Sayfalı)
        for (int i = 0; i < GRID_COLS * GRID_ROWS; i++)
        {
            Rectangle slotRect = GetPlayerSlotRect(i);
            if (slotRect.Contains(mousePos))
            {
                // Slotun indexi (x, y)
                int col = i % GRID_COLS;
                int row = i / GRID_COLS;
                var slot = _inventory.GetSlot(_currentPlayerPage, row, col);
                
                if (slot != null && !slot.IsEmpty)
                {
                    _hoveredPlayerSlot = i;
                    _tooltipItem = slot.Item;
                    _tooltipPosition = new Vector2(mousePos.X + 15, mousePos.Y + 15);
                    _showSellPrice = true;
                }
                break;
            }
        }
        
        // Satıcı slotları hover
        for (int i = 0; i < GRID_COLS * GRID_ROWS && i + _shopScrollOffset < _shopItemIds.Count; i++)
        {
            Rectangle slotRect = GetShopSlotRect(i);
            if (slotRect.Contains(mousePos))
            {
                _hoveredShopSlot = i + _shopScrollOffset;
                _tooltipItem = ItemDatabase.GetItem(_shopItemIds[_hoveredShopSlot]);
                _tooltipPosition = new Vector2(mousePos.X + 15, mousePos.Y + 15);
                _showSellPrice = false; // Satıcı eşyası = alış fiyatı
                break;
            }
        }
        
        // Tıklama
        if (currentMouseState.LeftButton == ButtonState.Pressed && 
            _previousMouseState.LeftButton == ButtonState.Released)
        {
            // Sayfa değiştirme
            if (_clickCooldown <= 0)
            {
                if (_hoveringPrev && _currentPlayerPage > 0)
                {
                    _currentPlayerPage--;
                    _clickCooldown = 0.2f; // 200ms cooldown
                    return;
                }
                if (_hoveringNext && _currentPlayerPage < MAX_PLAYER_PAGES - 1)
                {
                    _currentPlayerPage++;
                    _clickCooldown = 0.2f;
                    return;
                }
            }

            // Oyuncu slotuna tıklama = SAT
            if (_hoveredPlayerSlot >= 0)
            {
                int col = _hoveredPlayerSlot % GRID_COLS;
                int row = _hoveredPlayerSlot / GRID_COLS;
                var slot = _inventory.GetSlot(_currentPlayerPage, row, col);
                if (slot != null && !slot.IsEmpty)
                {
                    SellItem(slot);
                }
            }
            // Satıcı slotuna tıklama = AL
            else if (_hoveredShopSlot >= 0 && _hoveredShopSlot < _shopItemIds.Count)
            {
                BuyItem(_shopItemIds[_hoveredShopSlot]);
            }
        }
        
        if (_clickCooldown > 0) _clickCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Scroll (Sadece Shop İçin)
        int scrollDelta = currentMouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
        if (scrollDelta != 0)
        {
            if (_shopPanelBounds.Contains(mousePos))
            {
                _shopScrollOffset -= scrollDelta / 120;
                int maxScroll = Math.Max(0, _shopItemIds.Count - GRID_COLS * GRID_ROWS);
                _shopScrollOffset = Math.Clamp(_shopScrollOffset, 0, maxScroll);
            }
        }
        
        _previousKeyState = currentKeyState;
        _previousMouseState = currentMouseState;
    }
    
    private Rectangle GetPlayerSlotRect(int index)
    {
        int col = index % GRID_COLS;
        int row = index / GRID_COLS;
        return new Rectangle(
            _playerPanelBounds.X + 20 + col * (SLOT_SIZE + SLOT_PADDING),
            _playerPanelBounds.Y + 60 + row * (SLOT_SIZE + SLOT_PADDING),
            SLOT_SIZE,
            SLOT_SIZE
        );
    }
    
    // Shop slotları aynı mantık
    private Rectangle GetShopSlotRect(int index)
    {
        int col = index % GRID_COLS;
        int row = index / GRID_COLS;
        return new Rectangle(
            _shopPanelBounds.X + 20 + col * (SLOT_SIZE + SLOT_PADDING),
            _shopPanelBounds.Y + 60 + row * (SLOT_SIZE + SLOT_PADDING),
            SLOT_SIZE,
            SLOT_SIZE
        );
    }
    
    // GetPlayerItems metodunu siliyoruz, doğrudan slot erişimi var
    
    private void BuyItem(int itemId)
    {
        Item item = ItemDatabase.GetItem(itemId);
        if (item == null) return;
        
        if (_player.Gold >= item.BuyPrice)
        {
            if (_inventory.AddItem(itemId, 1))
            {
                _player.LoseGold(item.BuyPrice);
                _sfxBuy?.Play();
            }
        }
    }
    
    private void SellItem(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty) return;
        
        int sellPrice = slot.Item.SellPrice;
        
        slot.Quantity--;
        if (slot.Quantity <= 0) slot.Clear();
        
        _player.GainGold(sellPrice);
        _sfxSell?.Play();
    }
    
    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        if (!IsOpen) return;
        
        // Karartma
        spriteBatch.Draw(_backgroundTexture, 
            new Rectangle(0, 0, _screenWidth, _screenHeight), 
            new Color(0, 0, 0, 200));
        
        // Oyuncu paneli
        DrawPanel(spriteBatch, _playerPanelBounds, "EŞYALARIM", new Color(60, 80, 60));
        
        // Satıcı paneli
        DrawPanel(spriteBatch, _shopPanelBounds, "SATICI", new Color(80, 60, 80));
        
        // Altın göstergesi (ortada)
        string goldText = $"Altın: {_player.Gold} G";
        Vector2 goldSize = font.MeasureString(goldText);
        Vector2 goldPos = new Vector2(
            (_playerPanelBounds.Right + _shopPanelBounds.Left) / 2 - goldSize.X / 2,
            _playerPanelBounds.Y + 30
        );
        spriteBatch.DrawString(font, goldText, goldPos + new Vector2(1, 1), Color.Black);
        spriteBatch.DrawString(font, goldText, goldPos, Color.Gold);
        
        // Oyuncu itemlerini çiz (Sayfalı)
        for (int i = 0; i < GRID_COLS * GRID_ROWS; i++)
        {
            int col = i % GRID_COLS;
            int row = i / GRID_COLS;
            Rectangle slotRect = GetPlayerSlotRect(i);
            
            var slot = _inventory.GetSlot(_currentPlayerPage, row, col);
            bool isHovered = _hoveredPlayerSlot == i;
            Color slotColor = isHovered ? new Color(80, 100, 80) : Color.White;
            
            spriteBatch.Draw(_slotTexture, slotRect, slotColor);
            
            if (slot != null && !slot.IsEmpty && slot.Item != null)
            {
                if (slot.Item.Icon != null)
                {
                    spriteBatch.Draw(slot.Item.Icon, 
                        new Rectangle(slotRect.X + 5, slotRect.Y + 5, SLOT_SIZE - 10, SLOT_SIZE - 10),
                        slot.Item.GetTintColor()); 

                    if (slot.Item.Id == 32 || slot.Item.Id == 10 || slot.Item.Id == 11)
                    {
                        DrawDivineEffect(spriteBatch, slotRect);
                    }
                }

                // Adet Göster
                if (slot.Quantity > 1)
                {
                    string qtyText = slot.Quantity.ToString();
                    Vector2 qtySize = font.MeasureString(qtyText) * 0.6f;
                    Vector2 qtyPos = new Vector2(slotRect.X + 4, slotRect.Bottom - qtySize.Y - 2);
                    spriteBatch.DrawString(font, qtyText, qtyPos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(font, qtyText, qtyPos, Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                }
            }
        }
        
        // Sayfa Butonlarını Çiz (Player paneli için)
        Color prevColor = _hoveringPrev ? Color.Gold : Color.White;
        Color nextColor = _hoveringNext ? Color.Gold : Color.White;
        
        if (_currentPlayerPage > 0)
        {
            // _btnPrevPage için özel ok çizimi veya basit rect
            DrawArrow(spriteBatch, _btnPrevPage, true, prevColor);
        }
        
        if (_currentPlayerPage < MAX_PLAYER_PAGES - 1)
        {
            DrawArrow(spriteBatch, _btnNextPage, false, nextColor);
        }

        // Sayfa numarası texti
        string pageText = $"{_currentPlayerPage + 1}/{MAX_PLAYER_PAGES}";
        Vector2 pageSize = font.MeasureString(pageText);
        spriteBatch.DrawString(font, pageText, 
            new Vector2(_playerPanelBounds.Center.X - pageSize.X / 2, _btnPrevPage.Y), 
            Color.LightGray);
        
        
        // Satıcı itemlerini çiz
        for (int i = 0; i < GRID_COLS * GRID_ROWS && i + _shopScrollOffset < _shopItemIds.Count; i++)
        {
            int itemIndex = i + _shopScrollOffset;
            int itemId = _shopItemIds[itemIndex];
            Item item = ItemDatabase.GetItem(itemId);
            if (item == null) continue;
            
            Rectangle slotRect = GetShopSlotRect(i);
            
            bool isHovered = _hoveredShopSlot == itemIndex;
            bool canAfford = _player.Gold >= item.BuyPrice;
            Color slotColor = isHovered ? (canAfford ? new Color(80, 80, 100) : new Color(100, 60, 60)) : Color.White;
            
            spriteBatch.Draw(_slotTexture, slotRect, slotColor);
            
            if (item.Icon != null)
            {
                Color iconColor = canAfford ? item.GetTintColor() : new Color(100, 100, 100);
                Rectangle iconRect = new Rectangle(slotRect.X + 5, slotRect.Y + 5, SLOT_SIZE - 10, SLOT_SIZE - 10);
                spriteBatch.Draw(item.Icon, iconRect, iconColor);

                if (item.Id == 32 || item.Id == 10 || item.Id == 11)
                {
                    DrawDivineEffect(spriteBatch, slotRect);
                }
            }
            
            // Alış fiyatı göster
            // Alış fiyatı göster
            string price = FormatPrice(item.BuyPrice);
            if(!price.EndsWith("K") && !price.EndsWith("M")) price += "G";
            
            Color priceColor = canAfford ? Color.Gold : Color.Red;
            Vector2 priceSize = font.MeasureString(price) * 0.55f;
            spriteBatch.DrawString(font, price, 
                new Vector2(slotRect.Right - priceSize.X - 2, slotRect.Bottom - 12),
                priceColor, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
        }
        
        // Tooltip çiz
        if (_tooltipItem != null)
        {
            DrawTooltip(spriteBatch, font, _tooltipItem, _tooltipPosition, _showSellPrice);
        }
        
        // Kapatma ipucu
        string closeText = "[F] veya [ESC] Kapat";
        spriteBatch.DrawString(font, closeText, 
            new Vector2((_screenWidth - font.MeasureString(closeText).X * 0.7f) / 2, _shopPanelBounds.Bottom + 10),
            new Color(150, 150, 150), 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        // İpucu
        string hintText = "Sol Tık: Sat/Al";
        spriteBatch.DrawString(font, hintText, 
            new Vector2((_screenWidth - font.MeasureString(hintText).X * 0.6f) / 2, _shopPanelBounds.Bottom + 30),
            new Color(120, 120, 120), 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
    }
    
    private void DrawPanel(SpriteBatch spriteBatch, Rectangle bounds, string title, Color accentColor)
    {
        // Gölge
        spriteBatch.Draw(_backgroundTexture, 
            new Rectangle(bounds.X + 4, bounds.Y + 4, bounds.Width, bounds.Height),
            new Color(0, 0, 0, 100));
        
        // Ana panel
        spriteBatch.Draw(_backgroundTexture, bounds, new Color(30, 32, 40));
        
        // Başlık bar
        Rectangle titleBar = new Rectangle(bounds.X, bounds.Y, bounds.Width, 40);
        spriteBatch.Draw(_backgroundTexture, titleBar, accentColor);
        
        // Başlık
        // Fontu kullanmak için parametre olarak almamız lazım - basit çözüm
    }
    
    private void DrawTooltip(SpriteBatch spriteBatch, SpriteFont font, Item item, Vector2 position, bool showSellPrice)
    {
        string name = item.GetDisplayName();
        string type = item.Type switch
        {
            ItemType.Weapon => "Silah",
            ItemType.Armor => "Zırh",
            ItemType.Shield => "Kalkan",
            ItemType.Helmet => "Kask",
            ItemType.Material => "Malzeme",
            _ => "Eşya"
        };
        
        string stats = "";
        if (item.Type == ItemType.Weapon)
            stats = $"Hasar: {item.MinDamage}-{item.MaxDamage}";
        else if (item.Type == ItemType.Armor || item.Type == ItemType.Helmet)
            stats = $"Savunma: +{item.Defense} | Can: +{item.Health}";
        else if (item.Type == ItemType.Shield)
            stats = $"Savunma: +{item.Defense} | Blok: %{item.BlockChance}";
        
        string priceText = showSellPrice 
            ? $"Satış Bedeli: {item.SellPrice}G" 
            : $"Alış Bedeli: {item.BuyPrice}G";
        
        Color priceColor = showSellPrice ? Color.LightGreen : Color.Gold;
        
        // Açıklama Metni (Wrap Logic)
        string description = item.Description ?? "";
        List<string> descLines = new List<string>();
        float maxDescWidth = 250f; // Max tooltip genişliği
        
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

        float scale = 0.75f;
        Vector2 nameSize = font.MeasureString(name) * scale;
        Vector2 typeSize = font.MeasureString(type) * scale;
        Vector2 statsSize = stats.Length > 0 ? font.MeasureString(stats) * scale : Vector2.Zero;
        Vector2 priceSize = font.MeasureString(priceText) * scale;
        
        // Satırlar arası boşluk artırıldı (basık görünüm düzeltildi)
        float descHeight = descLines.Count * (font.LineSpacing * 0.75f); 
        
        float maxWidth = Math.Max(Math.Max(nameSize.X, typeSize.X), Math.Max(statsSize.X, priceSize.X));
        if (descLines.Count > 0) maxWidth = Math.Max(maxWidth, maxDescWidth);
        
        float totalHeight = nameSize.Y + typeSize.Y + (stats.Length > 0 ? statsSize.Y + 5 : 0) + (descLines.Count > 0 ? descHeight + 10 : 0) + priceSize.Y + 20;
        
        // Ekran sınırları
        if (position.X + maxWidth + 20 > _screenWidth)
            position.X = _screenWidth - maxWidth - 25;
        if (position.Y + totalHeight > _screenHeight)
            position.Y = _screenHeight - totalHeight - 10;
        
        Rectangle bgRect = new Rectangle(
            (int)position.X - 5,
            (int)position.Y - 5,
            (int)maxWidth + 20,
            (int)totalHeight + 10
        );
        
        spriteBatch.Draw(_backgroundTexture, bgRect, new Color(20, 22, 30, 250));
        
        // Kenarlık
        spriteBatch.Draw(_backgroundTexture, new Rectangle(bgRect.X, bgRect.Y, bgRect.Width, 1), new Color(80, 80, 100));
        spriteBatch.Draw(_backgroundTexture, new Rectangle(bgRect.X, bgRect.Y, 1, bgRect.Height), new Color(80, 80, 100));
        spriteBatch.Draw(_backgroundTexture, new Rectangle(bgRect.Right - 1, bgRect.Y, 1, bgRect.Height), new Color(80, 80, 100));
        spriteBatch.Draw(_backgroundTexture, new Rectangle(bgRect.X, bgRect.Bottom - 1, bgRect.Width, 1), new Color(80, 80, 100));
        
        float yOffset = 0;
        spriteBatch.DrawString(font, name, position + new Vector2(0, yOffset), item.GetRarityColor(),
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        yOffset += nameSize.Y + 3;
        
        spriteBatch.DrawString(font, type, position + new Vector2(0, yOffset), new Color(150, 150, 150),
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        yOffset += typeSize.Y + 3;
        
        if (stats.Length > 0)
        {
            spriteBatch.DrawString(font, stats, position + new Vector2(0, yOffset), new Color(100, 200, 100),
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            yOffset += statsSize.Y + 5;
        }
        
        // Açıklama Çizimi
        if (descLines.Count > 0)
        {
            yOffset += 5;
            foreach (var line in descLines)
            {
                spriteBatch.DrawString(font, line, position + new Vector2(0, yOffset), new Color(200, 200, 200),
                    0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f); // Scale 0.7f yapıldı
                yOffset += font.LineSpacing * 0.75f;
            }
            yOffset += 5;
        }
        
        spriteBatch.DrawString(font, priceText, position + new Vector2(0, yOffset), priceColor,
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawArrow(SpriteBatch spriteBatch, Rectangle buttonRect, bool left, Color color)
    {
        int x = buttonRect.Center.X;
        int y = buttonRect.Center.Y;
        int size = 8;
        
        for(int i=0; i<=size; i++)
        {
            if(left)
            {
                // < Şekli
                // Sol uca (x - size/2) yaklaştıkça yükseklik azalmalı (0)
                // Sağ tabana (x + size/2) yaklaştıkça yükseklik artmalı
                
                // i=0 -> Sol uç -> Height 1
                // i=size -> Sağ taban -> Height 2*size
                
                int px = (x - size/2) + i;
                int halfH = i;
                spriteBatch.Draw(_backgroundTexture, new Rectangle(px, y - halfH, 2, 2 * halfH + 1), color);
            }
            else
            {
                // > Şekli
                // Sol taban (x - size/2) -> Height max
                // Sağ uç (x + size/2) -> Height 0
                
                int px = (x - size/2) + i;
                int halfH = size - i;
                spriteBatch.Draw(_backgroundTexture, new Rectangle(px, y - halfH, 2, 2 * halfH + 1), color);
            }
        }
    }
    
    private void DrawDivineEffect(SpriteBatch sb, Rectangle rect)
    {
        float time = _animationTimer;
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
                sb.Draw(_backgroundTexture, new Rectangle((int)p3.X + rnd.Next(-5,6), (int)p3.Y + rnd.Next(-5,6), 2, 2), Color.Gold * strikeAlpha);
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
            
            sb.Draw(_backgroundTexture, new Rectangle((int)(center.X + pX), (int)(center.Y + pY), 2, 2), Color.White * alpha * 0.5f);
        }
    }

    private void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, int thickness = 1)
    {
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);
        sb.Draw(_backgroundTexture,
            new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness),
            null, color, angle, Vector2.Zero, SpriteEffects.None, 0);
    }
}
