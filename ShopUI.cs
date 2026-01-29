using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EternalJourney;

public class ShopUI
{
    public bool IsOpen { get; private set; } = false;
    
    private Texture2D _backgroundTexture;
    private Texture2D _slotTexture;
    private Texture2D _buttonTexture;
    
    private int _screenWidth;
    private int _screenHeight;
    
    private Rectangle _bounds;
    private Vector2 _position;
    
    // Satılabilir itemlar (ItemDatabase'den)
    private List<int> _shopItemIds = new List<int> { 1, 2, 3, 10, 11, 40, 41, 50, 51, 99, 20, 21, 22, 30, 31 };
    
    // Scroll
    private int _scrollOffset = 0;
    private const int ITEMS_PER_PAGE = 6;
    private const int SLOT_HEIGHT = 60;
    
    // Hover
    private int _hoveredItemIndex = -1;
    private bool _hoveringBuy = false;
    private bool _hoveringSell = false;
    private int _sellHoveredIndex = -1;
    
    // Mevcut durumlar
    private Player _player;
    private Inventory _inventory;
    
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyState;
    
    // Tab
    private int _currentTab = 0; // 0 = Buy, 1 = Sell
    private Rectangle _buyTabRect;
    private Rectangle _sellTabRect;
    
    public ShopUI(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        
        // Texture'lar
        _backgroundTexture = new Texture2D(graphicsDevice, 1, 1);
        _backgroundTexture.SetData(new[] { Color.White });
        
        _slotTexture = new Texture2D(graphicsDevice, 1, 1);
        _slotTexture.SetData(new[] { Color.White });
        
        _buttonTexture = new Texture2D(graphicsDevice, 1, 1);
        _buttonTexture.SetData(new[] { Color.White });
        
        CalculatePosition();
    }
    
    private void CalculatePosition()
    {
        int width = 450;
        int height = 500;
        
        _position = new Vector2(
            (_screenWidth - width) / 2f,
            (_screenHeight - height) / 2f
        );
        
        _bounds = new Rectangle((int)_position.X, (int)_position.Y, width, height);
        
        // Tab butonları
        _buyTabRect = new Rectangle((int)_position.X + 20, (int)_position.Y + 45, 100, 30);
        _sellTabRect = new Rectangle((int)_position.X + 130, (int)_position.Y + 45, 100, 30);
    }
    
    public void Open(Player player, Inventory inventory)
    {
        _player = player;
        _inventory = inventory;
        IsOpen = true;
        _scrollOffset = 0;
        _currentTab = 0;
    }
    
    public void Close()
    {
        IsOpen = false;
    }
    
    public void Update(GameTime gameTime, KeyboardState currentKeyState, MouseState currentMouseState)
    {
        if (!IsOpen) return;
        
        // ESC veya F ile kapat
        if ((currentKeyState.IsKeyDown(Keys.Escape) && !_previousKeyState.IsKeyDown(Keys.Escape)) ||
            (currentKeyState.IsKeyDown(Keys.F) && !_previousKeyState.IsKeyDown(Keys.F)))
        {
            Close();
        }
        
        Point mousePos = currentMouseState.Position;
        
        // Tab hover
        bool hoverBuyTab = _buyTabRect.Contains(mousePos);
        bool hoverSellTab = _sellTabRect.Contains(mousePos);
        
        // Tab tıklama
        if (currentMouseState.LeftButton == ButtonState.Pressed && 
            _previousMouseState.LeftButton == ButtonState.Released)
        {
            if (hoverBuyTab) _currentTab = 0;
            else if (hoverSellTab) _currentTab = 1;
        }
        
        // Item hover ve tıklama
        _hoveredItemIndex = -1;
        _sellHoveredIndex = -1;
        
        int listStartY = (int)_position.Y + 90;
        
        if (_currentTab == 0) // BUY
        {
            for (int i = 0; i < ITEMS_PER_PAGE && i + _scrollOffset < _shopItemIds.Count; i++)
            {
                Rectangle itemRect = new Rectangle(
                    (int)_position.X + 20,
                    listStartY + i * (SLOT_HEIGHT + 5),
                    _bounds.Width - 40,
                    SLOT_HEIGHT
                );
                
                if (itemRect.Contains(mousePos))
                {
                    _hoveredItemIndex = i + _scrollOffset;
                    
                    // Tıklama - Satın Al
                    if (currentMouseState.LeftButton == ButtonState.Pressed && 
                        _previousMouseState.LeftButton == ButtonState.Released)
                    {
                        TryBuyItem(_shopItemIds[_hoveredItemIndex]);
                    }
                }
            }
        }
        else // SELL
        {
            var sellableItems = GetSellableItems();
            for (int i = 0; i < ITEMS_PER_PAGE && i + _scrollOffset < sellableItems.Count; i++)
            {
                Rectangle itemRect = new Rectangle(
                    (int)_position.X + 20,
                    listStartY + i * (SLOT_HEIGHT + 5),
                    _bounds.Width - 40,
                    SLOT_HEIGHT
                );
                
                if (itemRect.Contains(mousePos))
                {
                    _sellHoveredIndex = i + _scrollOffset;
                    
                    // Tıklama - Sat
                    if (currentMouseState.LeftButton == ButtonState.Pressed && 
                        _previousMouseState.LeftButton == ButtonState.Released)
                    {
                        TrySellItem(sellableItems[_sellHoveredIndex]);
                    }
                }
            }
        }
        
        // Scroll
        int scrollDelta = currentMouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
        if (scrollDelta != 0)
        {
            _scrollOffset -= scrollDelta / 120;
            int maxItems = _currentTab == 0 ? _shopItemIds.Count : GetSellableItems().Count;
            _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, maxItems - ITEMS_PER_PAGE));
        }
        
        _previousKeyState = currentKeyState;
        _previousMouseState = currentMouseState;
    }
    
    private List<(int page, int y, int x, Item item)> GetSellableItems()
    {
        var items = new List<(int, int, int, Item)>();
        
        // Envanterdeki satılabilir itemları topla
        for (int p = 0; p < 4; p++)
        {
            for (int py = 0; py < 8; py++)
            {
                for (int px = 0; px < 8; px++)
                {
                    var slot = _inventory.GetSlot(p, py, px);
                    if (slot != null && !slot.IsEmpty && slot.Item != null)
                    {
                        items.Add((p, py, px, slot.Item));
                    }
                }
            }
        }
        
        return items;
    }
    
    private void TryBuyItem(int itemId)
    {
        Item item = ItemDatabase.GetItem(itemId);
        if (item == null) return;
        
        if (_player.Gold >= item.BuyPrice)
        {
            if (_inventory.AddItem(itemId, 1))
            {
                _player.LoseGold(item.BuyPrice);
            }
        }
    }
    
    private void TrySellItem((int page, int y, int x, Item item) sellInfo)
    {
        if (sellInfo.item == null) return;
        
        int sellPrice = sellInfo.item.SellPrice;
        
        // Slottan sil
        var slot = _inventory.GetSlot(sellInfo.page, sellInfo.y, sellInfo.x);
        if (slot != null)
        {
            slot.Quantity--;
            if (slot.Quantity <= 0) slot.Clear();
            
            _player.GainGold(sellPrice);
        }
    }
    
    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        if (!IsOpen) return;
        
        // Karartma
        spriteBatch.Draw(_backgroundTexture, 
            new Rectangle(0, 0, _screenWidth, _screenHeight), 
            new Color(0, 0, 0, 200));
        
        // Ana panel
        spriteBatch.Draw(_backgroundTexture, _bounds, new Color(30, 35, 45));
        
        // Başlık
        string title = "SATICI";
        Vector2 titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title, 
            new Vector2(_position.X + (_bounds.Width - titleSize.X) / 2, _position.Y + 10),
            new Color(255, 215, 100));
        
        // Altın göstergesi
        string goldText = $"Altin: {_player.Gold} G";
        spriteBatch.DrawString(font, goldText, 
            new Vector2(_position.X + _bounds.Width - font.MeasureString(goldText).X - 20, _position.Y + 12),
            Color.Gold, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        
        // Tab butonları
        Color buyTabColor = _currentTab == 0 ? new Color(80, 120, 80) : new Color(50, 55, 65);
        Color sellTabColor = _currentTab == 1 ? new Color(120, 80, 80) : new Color(50, 55, 65);
        
        spriteBatch.Draw(_backgroundTexture, _buyTabRect, buyTabColor);
        spriteBatch.Draw(_backgroundTexture, _sellTabRect, sellTabColor);
        
        spriteBatch.DrawString(font, "SATIN AL", 
            new Vector2(_buyTabRect.X + 10, _buyTabRect.Y + 5), 
            Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, "SAT", 
            new Vector2(_sellTabRect.X + 30, _sellTabRect.Y + 5), 
            Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        // Item listesi
        int listStartY = (int)_position.Y + 90;
        
        if (_currentTab == 0) // BUY
        {
            for (int i = 0; i < ITEMS_PER_PAGE && i + _scrollOffset < _shopItemIds.Count; i++)
            {
                int itemId = _shopItemIds[i + _scrollOffset];
                Item item = ItemDatabase.GetItem(itemId);
                if (item == null) continue;
                
                Rectangle itemRect = new Rectangle(
                    (int)_position.X + 20,
                    listStartY + i * (SLOT_HEIGHT + 5),
                    _bounds.Width - 40,
                    SLOT_HEIGHT
                );
                
                bool isHovered = _hoveredItemIndex == i + _scrollOffset;
                bool canAfford = _player.Gold >= item.BuyPrice;
                
                Color bgColor = isHovered ? new Color(60, 65, 80) : new Color(40, 45, 55);
                spriteBatch.Draw(_backgroundTexture, itemRect, bgColor);
                
                // İkon
                if (item.Icon != null)
                {
                    spriteBatch.Draw(item.Icon, 
                        new Rectangle(itemRect.X + 5, itemRect.Y + 5, 50, 50), 
                        item.GetRarityColor());
                }
                
                // İsim
                spriteBatch.DrawString(font, item.GetDisplayName(), 
                    new Vector2(itemRect.X + 60, itemRect.Y + 5), 
                    item.GetRarityColor(), 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                
                // Fiyat
                Color priceColor = canAfford ? Color.Gold : Color.Red;
                spriteBatch.DrawString(font, $"{item.BuyPrice} G", 
                    new Vector2(itemRect.X + 60, itemRect.Y + 30), 
                    priceColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                
                // Alınabilir mi?
                if (isHovered)
                {
                    string hint = canAfford ? "[Tıkla: Satın Al]" : "[Yetersiz Altın]";
                    spriteBatch.DrawString(font, hint, 
                        new Vector2(itemRect.Right - font.MeasureString(hint).X * 0.6f - 10, itemRect.Y + 20), 
                        canAfford ? Color.LightGreen : Color.Gray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                }
            }
        }
        else // SELL
        {
            var sellableItems = GetSellableItems();
            
            if (sellableItems.Count == 0)
            {
                spriteBatch.DrawString(font, "Satilacak esya yok!", 
                    new Vector2(_position.X + 120, listStartY + 100), 
                    Color.Gray);
            }
            
            for (int i = 0; i < ITEMS_PER_PAGE && i + _scrollOffset < sellableItems.Count; i++)
            {
                var sellInfo = sellableItems[i + _scrollOffset];
                Item item = sellInfo.item;
                
                Rectangle itemRect = new Rectangle(
                    (int)_position.X + 20,
                    listStartY + i * (SLOT_HEIGHT + 5),
                    _bounds.Width - 40,
                    SLOT_HEIGHT
                );
                
                bool isHovered = _sellHoveredIndex == i + _scrollOffset;
                
                Color bgColor = isHovered ? new Color(80, 60, 60) : new Color(40, 45, 55);
                spriteBatch.Draw(_backgroundTexture, itemRect, bgColor);
                
                // İkon
                if (item.Icon != null)
                {
                    spriteBatch.Draw(item.Icon, 
                        new Rectangle(itemRect.X + 5, itemRect.Y + 5, 50, 50), 
                        item.GetRarityColor());
                }
                
                // İsim
                spriteBatch.DrawString(font, item.GetDisplayName(), 
                    new Vector2(itemRect.X + 60, itemRect.Y + 5), 
                    item.GetRarityColor(), 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                
                // Satış Fiyatı
                spriteBatch.DrawString(font, $"+{item.SellPrice} G", 
                    new Vector2(itemRect.X + 60, itemRect.Y + 30), 
                    Color.LightGreen, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                
                if (isHovered)
                {
                    spriteBatch.DrawString(font, "[Tıkla: Sat]", 
                        new Vector2(itemRect.Right - 80, itemRect.Y + 20), 
                        Color.LightCoral, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                }
            }
        }
        
        // Kapatma ipucu
        spriteBatch.DrawString(font, "[F] veya [ESC] Kapat", 
            new Vector2(_position.X + 20, _position.Y + _bounds.Height - 25), 
            new Color(150, 150, 150), 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
    }
}
