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
    
    private int _screenWidth;
    private int _screenHeight;
    
    // Panel pozisyonları
    private Rectangle _playerPanelBounds;
    private Rectangle _shopPanelBounds;
    
    // Slot boyutları
    private const int SLOT_SIZE = 50;
    private const int SLOT_PADDING = 5;
    private const int GRID_COLS = 5;
    private const int GRID_ROWS = 6;
    
    // Satıcının satılık item ID'leri
    private List<int> _shopItemIds = new List<int> { 1, 2, 3, 10, 11, 40, 41, 50, 51, 99, 25, 20, 21, 22, 30, 31, 32 };
    
    // Hover
    private int _hoveredPlayerSlot = -1; // Hangi oyuncu slotuna hover edildi
    private int _hoveredShopSlot = -1;   // Hangi satıcı slotuna hover edildi
    
    // Tooltip
    private Item _tooltipItem = null;
    private Vector2 _tooltipPosition;
    private bool _showSellPrice = false; // Satış fiyatı mı alış fiyatı mı gösterilecek
    
    // Mevcut durumlar
    private Player _player;
    private Inventory _inventory;
    
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyState;
    
    // Scroll
    private int _playerScrollOffset = 0;
    private int _shopScrollOffset = 0;
    private float _animationTimer = 0f;
    
    // SFX
    private SoundEffect _sfxBuy;
    private SoundEffect _sfxSell;
    
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
    }
    
    public void Open(Player player, Inventory inventory)
    {
        _player = player;
        _inventory = inventory;
        IsOpen = true;
        _playerScrollOffset = 0;
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
        
        // Oyuncu slotları hover
        var playerItems = GetPlayerItems();
        for (int i = 0; i < GRID_COLS * GRID_ROWS && i + _playerScrollOffset < playerItems.Count; i++)
        {
            Rectangle slotRect = GetPlayerSlotRect(i);
            if (slotRect.Contains(mousePos))
            {
                _hoveredPlayerSlot = i + _playerScrollOffset;
                _tooltipItem = playerItems[_hoveredPlayerSlot].item;
                _tooltipPosition = new Vector2(mousePos.X + 15, mousePos.Y + 15);
                _showSellPrice = true; // Oyuncu eşyası = satış fiyatı
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
            // Oyuncu slotuna tıklama = SAT
            if (_hoveredPlayerSlot >= 0 && _hoveredPlayerSlot < playerItems.Count)
            {
                SellItem(playerItems[_hoveredPlayerSlot]);
            }
            // Satıcı slotuna tıklama = AL
            else if (_hoveredShopSlot >= 0 && _hoveredShopSlot < _shopItemIds.Count)
            {
                BuyItem(_shopItemIds[_hoveredShopSlot]);
            }
        }
        
        // Scroll
        int scrollDelta = currentMouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
        if (scrollDelta != 0)
        {
            if (_playerPanelBounds.Contains(mousePos))
            {
                _playerScrollOffset -= scrollDelta / 120;
                int maxScroll = Math.Max(0, playerItems.Count - GRID_COLS * GRID_ROWS);
                _playerScrollOffset = Math.Clamp(_playerScrollOffset, 0, maxScroll);
            }
            else if (_shopPanelBounds.Contains(mousePos))
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
    
    private List<(int page, int y, int x, Item item, int quantity)> GetPlayerItems()
    {
        var items = new List<(int, int, int, Item, int)>();
        
        for (int p = 0; p < 4; p++)
        {
            for (int py = 0; py < 8; py++)
            {
                for (int px = 0; px < 8; px++)
                {
                    var slot = _inventory.GetSlot(p, py, px);
                    if (slot != null && !slot.IsEmpty && slot.Item != null)
                    {
                        items.Add((p, py, px, slot.Item, slot.Quantity));
                    }
                }
            }
        }
        
        return items;
    }
    
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
    
    private void SellItem((int page, int y, int x, Item item, int quantity) sellInfo)
    {
        if (sellInfo.item == null) return;
        
        int sellPrice = sellInfo.item.SellPrice;
        
        var slot = _inventory.GetSlot(sellInfo.page, sellInfo.y, sellInfo.x);
        if (slot != null)
        {
            slot.Quantity--;
            if (slot.Quantity <= 0) slot.Clear();
            
            _player.GainGold(sellPrice);
            _sfxSell?.Play();
        }
    }
    
    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        if (!IsOpen) return;
        
        // Karartma
        spriteBatch.Draw(_backgroundTexture, 
            new Rectangle(0, 0, _screenWidth, _screenHeight), 
            new Color(0, 0, 0, 200));
        
        // Oyuncu paneli
        DrawPanel(spriteBatch, _playerPanelBounds, "ESYALARIM", new Color(60, 80, 60));
        
        // Satıcı paneli
        DrawPanel(spriteBatch, _shopPanelBounds, "SATICI", new Color(80, 60, 80));
        
        // Altın göstergesi (ortada)
        string goldText = $"Altin: {_player.Gold} G";
        Vector2 goldSize = font.MeasureString(goldText);
        Vector2 goldPos = new Vector2(
            (_playerPanelBounds.Right + _shopPanelBounds.Left) / 2 - goldSize.X / 2,
            _playerPanelBounds.Y + 30
        );
        spriteBatch.DrawString(font, goldText, goldPos + new Vector2(1, 1), Color.Black);
        spriteBatch.DrawString(font, goldText, goldPos, Color.Gold);
        
        // Oyuncu itemlerini çiz
        var playerItems = GetPlayerItems();
        for (int i = 0; i < GRID_COLS * GRID_ROWS && i + _playerScrollOffset < playerItems.Count; i++)
        {
            int itemIndex = i + _playerScrollOffset;
            var itemInfo = playerItems[itemIndex];
            Rectangle slotRect = GetPlayerSlotRect(i);
            
            bool isHovered = _hoveredPlayerSlot == itemIndex;
            Color slotColor = isHovered ? new Color(80, 100, 80) : Color.White;
            
            spriteBatch.Draw(_slotTexture, slotRect, slotColor);
            
            if (itemInfo.item.Icon != null)
            {
                spriteBatch.Draw(itemInfo.item.Icon, 
                    new Rectangle(slotRect.X + 5, slotRect.Y + 5, SLOT_SIZE - 10, SLOT_SIZE - 10),
                    itemInfo.item.GetTintColor()); // BUG FIX: GetRarityColor -> GetTintColor

                if (itemInfo.item.Id == 32 || itemInfo.item.Id == 10)
                {
                    DrawDivineEffect(spriteBatch, slotRect);
                }
            }

            // Adet Göster
            if (itemInfo.quantity > 1)
            {
                string qtyText = itemInfo.quantity.ToString();
                Vector2 qtySize = font.MeasureString(qtyText) * 0.6f;
                Vector2 qtyPos = new Vector2(slotRect.X + 4, slotRect.Bottom - qtySize.Y - 2);
                spriteBatch.DrawString(font, qtyText, qtyPos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(font, qtyText, qtyPos, Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            }
            
            // Satış fiyatı göster (hover'da)
            if (isHovered)
            {
                string price = $"+{itemInfo.item.SellPrice}G";
                Vector2 priceSize = font.MeasureString(price) * 0.6f;
                spriteBatch.DrawString(font, price, 
                    new Vector2(slotRect.Right - priceSize.X - 2, slotRect.Bottom - priceSize.Y - 2),
                    Color.LightGreen, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            }
        }
        
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

                if (item.Id == 32 || item.Id == 10)
                {
                    DrawDivineEffect(spriteBatch, slotRect);
                }
            }
            
            // Alış fiyatı göster
            string price = $"{item.BuyPrice}G";
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
        string hintText = "Sol Tik: Sat/Al";
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
            ItemType.Armor => "Zirh",
            ItemType.Shield => "Kalkan",
            ItemType.Helmet => "Kask",
            ItemType.Material => "Malzeme",
            _ => "Esya"
        };
        
        string stats = "";
        if (item.Type == ItemType.Weapon)
            stats = $"Hasar: {item.MinDamage}-{item.MaxDamage}";
        else if (item.Type == ItemType.Armor || item.Type == ItemType.Helmet)
            stats = $"Savunma: +{item.Defense} | Can: +{item.Health}";
        else if (item.Type == ItemType.Shield)
            stats = $"Savunma: +{item.Defense} | Blok: %{item.BlockChance}";
        
        string priceText = showSellPrice 
            ? $"Satis Bedeli: {item.SellPrice}G" 
            : $"Alis Bedeli: {item.BuyPrice}G";
        
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
