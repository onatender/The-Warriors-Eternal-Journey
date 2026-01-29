using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EternalJourney;

public class TitleScreen
{
    private GraphicsDevice _graphicsDevice;
    private int _screenWidth;
    private int _screenHeight;
    
    // UI Elements
    private Texture2D _panelTexture;
    
    // Slots
    private SaveData[] _saveSlots;
    private Rectangle[] _slotRects;
    private int _hoveredSlot = -1;
    
    // State
    public bool IsNameInputActive { get; private set; } = false;
    private int _targetSlotForNewGame = -1;
    private string _newPlayerName = "";
    
    // Events
    public event Action<int, string> OnGameStart; // slotIndex, playerName (if new)
    
    // Keyboard for text input
    private KeyboardState _previousKeyState;
    
    public TitleScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        
        // Create simple textures
        _panelTexture = new Texture2D(graphicsDevice, 1, 1);
        _panelTexture.SetData(new[] { Color.White });
        
        // Slot Layout (3 horizontal or vertical, let's do Vertical for details)
        _slotRects = new Rectangle[3];
        int slotWidth = 400;
        int slotHeight = 100;
        int startY = screenHeight / 2 - (slotHeight * 3 + 40) / 2;
        int centerX = (screenWidth - slotWidth) / 2;
        
        for (int i = 0; i < 3; i++)
        {
            _slotRects[i] = new Rectangle(centerX, startY + i * (slotHeight + 20), slotWidth, slotHeight);
        }
        
        RefreshSlots();
    }
    
    public void RefreshSlots()
    {
        _saveSlots = SaveManager.GetSaveSlots();
    }
    
    public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
    {
        if (IsNameInputActive)
        {
            HandleNameInput(keyboardState);
            _previousKeyState = keyboardState;
            return;
        }
        
        // Mouse Hover
        _hoveredSlot = -1;
        for (int i = 0; i < 3; i++)
        {
            if (_slotRects[i].Contains(mouseState.Position))
            {
                _hoveredSlot = i;
                
                // Click
                if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                {
                    OnSlotClicked(i);
                }
            }
        }
        
        _previousMouseState = mouseState;
        _previousKeyState = keyboardState;
    }
    
    private MouseState _previousMouseState;
    
    private void OnSlotClicked(int slotIndex)
    {
        if (_saveSlots[slotIndex] != null)
        {
            // Load Existing
            OnGameStart?.Invoke(slotIndex, null);
        }
        else
        {
            // Create New
            _targetSlotForNewGame = slotIndex;
            _newPlayerName = "";
            IsNameInputActive = true;
        }
    }
    
    private void HandleNameInput(KeyboardState currentKey)
    {
        // Simple text input helper would be better, but implementing basic here
        Keys[] pressedKeys = currentKey.GetPressedKeys();
        
        foreach (Keys key in pressedKeys)
        {
            if (!_previousKeyState.IsKeyDown(key))
            {
                if (key == Keys.Enter)
                {
                    if (!string.IsNullOrWhiteSpace(_newPlayerName))
                    {
                        OnGameStart?.Invoke(_targetSlotForNewGame, _newPlayerName);
                        IsNameInputActive = false;
                    }
                }
                else if (key == Keys.Back && _newPlayerName.Length > 0)
                {
                    _newPlayerName = _newPlayerName.Substring(0, _newPlayerName.Length - 1);
                }
                else if (key == Keys.Escape)
                {
                    IsNameInputActive = false;
                    _targetSlotForNewGame = -1;
                }
                else
                {
                    // Add char
                    string charStr = GetCharFromKey(key);
                    if (charStr != "" && _newPlayerName.Length < 12)
                    {
                        _newPlayerName += charStr;
                    }
                }
            }
        }
    }
    
    // Helper to get char from key (very basic)
    private string GetCharFromKey(Keys key)
    {
        if (key >= Keys.A && key <= Keys.Z) return key.ToString();
        if (key >= Keys.D0 && key <= Keys.D9) return key.ToString().Substring(1);
        if (key == Keys.Space) return " ";
        return "";
    }
    
    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        // Title
        string title = "THE WARRIOR'S ETERNAL JOURNEY";
        Vector2 titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title, 
            new Vector2((_screenWidth - titleSize.X) / 2, 80), 
            Color.Gold, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
            
        // Slots
        for (int i = 0; i < 3; i++)
        {
            Rectangle r = _slotRects[i];
            bool isHover = _hoveredSlot == i;
            Color bgColor = isHover ? new Color(60, 60, 80) : new Color(40, 40, 50);
            Color borderColor = isHover ? Color.Yellow : Color.Gray;
            
            // Background
            spriteBatch.Draw(_panelTexture, r, bgColor);
            
            // Border (Simple 2px)
            spriteBatch.Draw(_panelTexture, new Rectangle(r.X, r.Y, r.Width, 2), borderColor);
            spriteBatch.Draw(_panelTexture, new Rectangle(r.X, r.Bottom-2, r.Width, 2), borderColor);
            spriteBatch.Draw(_panelTexture, new Rectangle(r.X, r.Y, 2, r.Height), borderColor);
            spriteBatch.Draw(_panelTexture, new Rectangle(r.Right-2, r.Y, 2, r.Height), borderColor);
            
            // Text
            SaveData data = _saveSlots[i];
            if (data != null)
            {
                // Existing Save
                spriteBatch.DrawString(font, $"SLOT {i+1}: {data.PlayerName}", new Vector2(r.X + 20, r.Y + 20), Color.White);
                spriteBatch.DrawString(font, $"Level {data.Level} | Gold: {data.Gold}", new Vector2(r.X + 20, r.Y + 50), Color.LightGray);
            }
            else
            {
                // Empty
                spriteBatch.DrawString(font, $"SLOT {i+1}: [ EMPTY ]", new Vector2(r.X + 20, r.Y + 35), Color.Gray);
                if (isHover)
                {
                     spriteBatch.DrawString(font, "(Click to Create New)", new Vector2(r.X + 250, r.Y + 35), Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                }
            }
        }
        
        // Name Input Modal
        if (IsNameInputActive)
        {
            // Dim background
            spriteBatch.Draw(_panelTexture, new Rectangle(0, 0, _screenWidth, _screenHeight), new Color(0, 0, 0, 200));
            
            // Input Box
            int boxW = 400; int boxH = 200;
            Rectangle boxRect = new Rectangle((_screenWidth - boxW)/2, (_screenHeight - boxH)/2, boxW, boxH);
            spriteBatch.Draw(_panelTexture, boxRect, new Color(50, 50, 60));
            spriteBatch.Draw(_panelTexture, new Rectangle(boxRect.X, boxRect.Y, boxW, 2), Color.Cyan); // Border Top
            
            string prompt = "ENTER CHARACTER NAME:";
            Vector2 pSize = font.MeasureString(prompt);
            spriteBatch.DrawString(font, prompt, new Vector2(boxRect.X + (boxW-pSize.X)/2, boxRect.Y + 40), Color.Cyan);
            
            // Name
            string display = _newPlayerName + "_";
            Vector2 nSize = font.MeasureString(display);
            spriteBatch.DrawString(font, display, new Vector2(boxRect.X + (boxW-nSize.X)/2, boxRect.Y + 90), Color.White);
            
            // Hint
            string hitT = "[ENTER] Start  [ESC] Cancel";
            Vector2 hSize = font.MeasureString(hitT) * 0.7f;
            spriteBatch.DrawString(font, hitT, new Vector2(boxRect.X + (boxW-hSize.X)/2, boxRect.Y + 150), Color.Gray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
    }
}
