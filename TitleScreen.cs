using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

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
    
    // Volume Control
    private Rectangle _volumeBarRect;
    private Rectangle _volumeKnobRect;
    private bool _isDraggingVolume = false;
    private float _currentVolume = 1.0f;
    
    // Buttons (New Game Modal)
    private Rectangle _btnStartRect;
    private Rectangle _btnCancelRect;
    private bool _hoverStart = false;
    private bool _hoverCancel = false;
    
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
        
        // Volume Slider (Bottom Right)
        _volumeBarRect = new Rectangle(screenWidth - 170, screenHeight - 40, 150, 10);
        UpdateVolumeKnob();
        
        // Init Sound Volume
        try { _currentVolume = SoundEffect.MasterVolume; } catch { _currentVolume = 1.0f; }
        UpdateVolumeKnob();
        
        RefreshSlots();
    }
    
    private void UpdateVolumeKnob()
    {
        int knobX = _volumeBarRect.X + (int)(_volumeBarRect.Width * _currentVolume) - 5;
        _volumeKnobRect = new Rectangle(knobX, _volumeBarRect.Y - 5, 10, 20);
    }
    
    public void RefreshSlots()
    {
        _saveSlots = SaveManager.GetSaveSlots();
    }
    
    public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
    {
        Point mousePos = mouseState.Position;
        
        // --- VOLUME CONTROL ---
        // Drag start
        if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            if (_volumeKnobRect.Contains(mousePos) || _volumeBarRect.Contains(mousePos))
            {
                _isDraggingVolume = true;
            }
        }
        
        // Dragging
        if (_isDraggingVolume && mouseState.LeftButton == ButtonState.Pressed)
        {
            float relativeX = mousePos.X - _volumeBarRect.X;
            _currentVolume = MathHelper.Clamp(relativeX / _volumeBarRect.Width, 0f, 1f);
            
            try { SoundEffect.MasterVolume = _currentVolume; } catch {}
            UpdateVolumeKnob();
        }
        
        // Drag End
        if (mouseState.LeftButton == ButtonState.Released)
        {
            _isDraggingVolume = false;
        }
    
        if (IsNameInputActive)
        {
            HandleNameInput(keyboardState);
            
            // Mouse for Modal Buttons
            _hoverStart = _btnStartRect.Contains(mousePos);
            _hoverCancel = _btnCancelRect.Contains(mousePos);
            
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                if (_hoverStart && !string.IsNullOrWhiteSpace(_newPlayerName))
                {
                    OnGameStart?.Invoke(_targetSlotForNewGame, _newPlayerName);
                    IsNameInputActive = false;
                }
                else if (_hoverCancel)
                {
                    IsNameInputActive = false;
                    _targetSlotForNewGame = -1;
                }
            }
            
            _previousKeyState = keyboardState;
            _previousMouseState = mouseState; // IMPORTANT to update prev state here too if returning early
            return;
        }
        
        // Mouse Hover Slots
        _hoveredSlot = -1;
        for (int i = 0; i < 3; i++)
        {
            if (_slotRects[i].Contains(mousePos))
            {
                _hoveredSlot = i;
                
                // Click
                if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released && !_isDraggingVolume)
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
            
            // Initialize Modal Button Rects (Centered relative to screen)
            int boxW = 400; int boxH = 200;
            Rectangle boxRect = new Rectangle((_screenWidth - boxW)/2, (_screenHeight - boxH)/2, boxW, boxH);
            
            // Buttons at bottom of modal
            int btnW = 100; int btnH = 30;
            int spacing = 20;
            int startX = boxRect.Center.X - btnW - spacing/2;
            int cancelX = boxRect.Center.X + spacing/2;
            int btnY = boxRect.Bottom - 45;
            
            _btnStartRect = new Rectangle(startX, btnY, btnW, btnH);
            _btnCancelRect = new Rectangle(cancelX, btnY, btnW, btnH);
        }
    }
    
    private void HandleNameInput(KeyboardState currentKey)
    {
        // Simple text input helper
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
        // 1. TAM ORTALI TITLE
        string title = "THE WARRIOR'S ETERNAL JOURNEY";
        Vector2 titleSize = font.MeasureString(title) * 1.5f; // Scale 1.5
        
        // Ekranın tam ortası
        float centerX = _screenWidth / 2f;
        float centerY = _screenHeight / 2f;
        
        // Başlık pozisyonu (Ekranın üst kısmında ortalı)
        Vector2 titlePos = new Vector2(centerX - titleSize.X / 2f, 80);
        
        spriteBatch.DrawString(font, title, 
            titlePos, 
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
            
            // Text - Slot İçeriği de ortalanabilir veya sola hizalı kalabilir
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
                     string clickText = "(Click to Create New)";
                     Vector2 clickSz = font.MeasureString(clickText) * 0.7f;
                     spriteBatch.DrawString(font, clickText, 
                        new Vector2(r.Right - clickSz.X - 20, r.Y + 35), 
                        Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
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
            spriteBatch.DrawString(font, display, new Vector2(boxRect.X + (boxW-nSize.X)/2, boxRect.Y + 80), Color.White);
            
            // Buttons
            Color startColor = _hoverStart ? Color.Lime : new Color(0, 100, 0);
            Color cancelColor = _hoverCancel ? Color.Red : new Color(100, 0, 0);
            
            // Start Btn
            spriteBatch.Draw(_panelTexture, _btnStartRect, startColor);
            string startTxt = "BASLA";
            Vector2 sSz = font.MeasureString(startTxt);
            spriteBatch.DrawString(font, startTxt, new Vector2(_btnStartRect.Center.X - sSz.X/2, _btnStartRect.Center.Y - sSz.Y/2), Color.White);
            
            // Cancel Btn
            spriteBatch.Draw(_panelTexture, _btnCancelRect, cancelColor);
            string cancelTxt = "IPTAL";
            Vector2 cSz = font.MeasureString(cancelTxt);
            spriteBatch.DrawString(font, cancelTxt, new Vector2(_btnCancelRect.Center.X - cSz.X/2, _btnCancelRect.Center.Y - cSz.Y/2), Color.White);
        }
        
        // DRAW VOLUME SLIDER (FIXED LAYOUT)
        // Label
        string volLabel = $"SES: %{(int)(_currentVolume * 100)}";
        Vector2 volLabelSize = font.MeasureString(volLabel);
        
        // Label to the left of Bar
        Vector2 labelPos = new Vector2(_volumeBarRect.X - volLabelSize.X - 10, _volumeBarRect.Y - volLabelSize.Y / 2 + 5);
        spriteBatch.DrawString(font, volLabel, labelPos, Color.White);
        
        // Bar Background
        spriteBatch.Draw(_panelTexture, _volumeBarRect, Color.DarkGray);
        
        // Fill
        Rectangle fillRect = new Rectangle(_volumeBarRect.X, _volumeBarRect.Y, (int)(_volumeBarRect.Width * _currentVolume), _volumeBarRect.Height);
        spriteBatch.Draw(_panelTexture, fillRect, Color.Lime);
        
        // Knob
        spriteBatch.Draw(_panelTexture, _volumeKnobRect, Color.White);
    }
}
