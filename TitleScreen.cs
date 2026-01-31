using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace EternalJourney;

public enum TitleState
{
    MainMenu,
    SaveSelection,
    Settings
}

public class TitleScreen
{
    private GraphicsDevice _graphicsDevice;
    private int _screenWidth;
    private int _screenHeight;
    
    // UI Elements
    private Texture2D _panelTexture;
    
    // State
    private TitleState _currentState = TitleState.MainMenu;
    
    // -- MAIN MENU --
    private Rectangle _btnPlayRect;
    private Rectangle _btnSettingsRect;
    private Rectangle _btnExitRect;
    
    // -- SAVE SELECTION --
    private SaveData[] _saveSlots;
    private Rectangle[] _slotRects;
    private int _hoveredSlot = -1;
    public bool IsNameInputActive { get; private set; } = false;
    private int _targetSlotForNewGame = -1;
    private string _newPlayerName = "";
    private Rectangle _btnBackRect; // Common Back Button
    private Rectangle[] _btnDeleteRects; // Delete buttons for slots
    
    // -- SETTINGS --
    // Volume Control
    private Rectangle _musicVolumeBarRect;
    private Rectangle _musicVolumeKnobRect;
    private Rectangle _sfxVolumeBarRect;
    private Rectangle _sfxVolumeKnobRect;
    private bool _isDraggingMusic = false;
    private bool _isDraggingSFX = false;
    
    // New Game Modal Buttons
    private Rectangle _btnStartRect;
    private Rectangle _btnCancelRect;
    private bool _hoverStart = false;
    private bool _hoverCancel = false;
    
    // Events
    public event Action<int, string> OnGameStart; // slotIndex, playerName (if new)
    public event Action OnExitRequested;
    
    // Keyboard for text input
    private KeyboardState _previousKeyState;
    private MouseState _previousMouseState;
    
    public TitleScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        
        // Create simple textures
        _panelTexture = new Texture2D(graphicsDevice, 1, 1);
        _panelTexture.SetData(new[] { Color.White });
        
        CalculateLayout();
        
        RefreshSlots();
    }
    
    private void CalculateLayout()
    {
        int centerX = _screenWidth / 2;
        int centerY = _screenHeight / 2;
        
        // -- MAIN MENU BUTTONS --
        int btnW = 200; int btnH = 50; int spacing = 20;
        int startY = centerY - (btnH * 3 + spacing * 2) / 2 + 50;
        
        _btnPlayRect = new Rectangle(centerX - btnW/2, startY, btnW, btnH);
        _btnSettingsRect = new Rectangle(centerX - btnW/2, startY + btnH + spacing, btnW, btnH);
        _btnExitRect = new Rectangle(centerX - btnW/2, startY + (btnH + spacing) * 2, btnW, btnH);
        
        // -- SAVE SLOTS --
        _slotRects = new Rectangle[3];
        _btnDeleteRects = new Rectangle[3];
        int slotWidth = 400;
        int slotHeight = 100;
        int slotStartY = centerY - (slotHeight * 3 + 40) / 2;
        
        for (int i = 0; i < 3; i++)
        {
            _slotRects[i] = new Rectangle((_screenWidth - slotWidth) / 2, slotStartY + i * (slotHeight + 20), slotWidth, slotHeight);
            
            // Delete button: Small 'X' to the right of the slot
            _btnDeleteRects[i] = new Rectangle(_slotRects[i].Right + 10, _slotRects[i].Y + (slotHeight - 30)/2, 30, 30);
        }
        
        // -- BACK BUTTON --
        _btnBackRect = new Rectangle(20, _screenHeight - 60, 100, 40);
        
        // -- SETTINGS (Title Screen specific) --
        // Music
        _musicVolumeBarRect = new Rectangle(centerX - 100, centerY - 30, 200, 10);
        _sfxVolumeBarRect = new Rectangle(centerX - 100, centerY + 30, 200, 10);
        _musicVolumeBarRect = new Rectangle(centerX - 100, centerY - 30, 200, 10);
        _sfxVolumeBarRect = new Rectangle(centerX - 100, centerY + 30, 200, 10);
        UpdateVolumeKnobs();
    }
    
    public void Reset()
    {
        _currentState = TitleState.MainMenu;
        RefreshSlots();
    }
    
    private void UpdateVolumeKnobs()
    {
        // Music
        float musicVol = MusicManager.Instance?.MasterVolume ?? 0.5f;
        
        // Knob positions will be updated in Update loop based on current values
    }
    
    public void RefreshSlots()
    {
        _saveSlots = SaveManager.GetSaveSlots();
    }
    
    public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
    {
        Point mousePos = mouseState.Position;
        bool leftClick = mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;
        
        if (_currentState == TitleState.MainMenu)
        {
            if (leftClick)
            {
                if (_btnPlayRect.Contains(mousePos)) _currentState = TitleState.SaveSelection;
                else if (_btnSettingsRect.Contains(mousePos)) _currentState = TitleState.Settings;
                else if (_btnExitRect.Contains(mousePos)) OnExitRequested?.Invoke();
            }
        }
        else if (_currentState == TitleState.SaveSelection)
        {
            if (IsNameInputActive)
            {
                HandleNameInput(keyboardState);
                
                // Modal Buttons
                _hoverStart = _btnStartRect.Contains(mousePos);
                _hoverCancel = _btnCancelRect.Contains(mousePos);
                
                if (leftClick)
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
            }
            else
            {
                // Slot Interaction
                _hoveredSlot = -1;
                for (int i = 0; i < 3; i++)
                {
                    if (_slotRects[i].Contains(mousePos))
                    {
                        _hoveredSlot = i;
                        if (leftClick) OnSlotClicked(i);
                    }
                    
                    // Handle Delete
                     if (_saveSlots[i] != null && _btnDeleteRects[i].Contains(mousePos) && leftClick)
                    {
                         SaveManager.DeleteSave(i);
                         RefreshSlots();
                    }
                }
                
                // Backck
                if (leftClick && _btnBackRect.Contains(mousePos))
                {
                     _currentState = TitleState.MainMenu;
                }
            }
        }
        else if (_currentState == TitleState.Settings)
        {
            // Back
            if (leftClick && _btnBackRect.Contains(mousePos) && !_isDraggingMusic && !_isDraggingSFX)
            {
                _currentState = TitleState.MainMenu;
            }
            
            // --- MUSIC ---
            // Drag Start
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                if (_musicVolumeKnobRect.Contains(mousePos) || _musicVolumeBarRect.Contains(mousePos)) _isDraggingMusic = true;
                if (_sfxVolumeKnobRect.Contains(mousePos) || _sfxVolumeBarRect.Contains(mousePos)) _isDraggingSFX = true;
            }
            
            // Dragging
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                if (_isDraggingMusic)
                {
                    float val = MathHelper.Clamp((float)(mousePos.X - _musicVolumeBarRect.X) / _musicVolumeBarRect.Width, 0f, 1f);
                    // Update Music Volume Global
                    if (MusicManager.Instance != null)
                        MusicManager.Instance.SetVolume(val);
                }
                if (_isDraggingSFX)
                {
                    float val = MathHelper.Clamp((float)(mousePos.X - _sfxVolumeBarRect.X) / _sfxVolumeBarRect.Width, 0f, 1f);
                    SoundEffect.MasterVolume = val;
                }
            }
            else
            {
                _isDraggingMusic = false;
                _isDraggingSFX = false;
            }
            
            // Update Rects based on current values
            float musicVol = MusicManager.Instance?.MasterVolume ?? 0.5f;
            float sfxVol = SoundEffect.MasterVolume;
            
            _musicVolumeKnobRect = new Rectangle(_musicVolumeBarRect.X + (int)(_musicVolumeBarRect.Width * musicVol) - 5, _musicVolumeBarRect.Y - 5, 10, 20);
            _sfxVolumeKnobRect = new Rectangle(_sfxVolumeBarRect.X + (int)(_sfxVolumeBarRect.Width * sfxVol) - 5, _sfxVolumeBarRect.Y - 5, 10, 20);
        }
        
        _previousMouseState = mouseState;
        _previousKeyState = keyboardState;
    }
    
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
        Keys[] pressedKeys = currentKey.GetPressedKeys();
        foreach (Keys key in pressedKeys)
        {
            if (!_previousKeyState.IsKeyDown(key))
            {
                if (key == Keys.Enter) {
                    if (!string.IsNullOrWhiteSpace(_newPlayerName)) {
                        OnGameStart?.Invoke(_targetSlotForNewGame, _newPlayerName);
                        IsNameInputActive = false;
                    }
                }
                else if (key == Keys.Back && _newPlayerName.Length > 0) _newPlayerName = _newPlayerName.Substring(0, _newPlayerName.Length - 1);
                else if (key == Keys.Escape) { IsNameInputActive = false; _targetSlotForNewGame = -1; }
                else {
                    string charStr = GetCharFromKey(key);
                    if (charStr != "" && _newPlayerName.Length < 12) _newPlayerName += charStr;
                }
            }
        }
    }
    
    private string GetCharFromKey(Keys key)
    {
        if (key >= Keys.A && key <= Keys.Z) return key.ToString();
        if (key >= Keys.D0 && key <= Keys.D9) return key.ToString().Substring(1);
        if (key == Keys.Space) return " ";
        return "";
    }
    
    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        // Draw Header
        string title = "THE WARRIOR'S ETERNAL JOURNEY";
        Vector2 titleSize = font.MeasureString(title) * 1.5f;
        Vector2 titlePos = new Vector2((_screenWidth - titleSize.X)/2, 80);
        
        spriteBatch.DrawString(font, title, titlePos, Color.Gold, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
        
        if (_currentState == TitleState.MainMenu)
        {
            DrawButton(spriteBatch, font, "OYNA", _btnPlayRect, _btnPlayRect.Contains(Mouse.GetState().Position));
            DrawButton(spriteBatch, font, "AYARLAR", _btnSettingsRect, _btnSettingsRect.Contains(Mouse.GetState().Position));
            DrawButton(spriteBatch, font, "ÇIKIŞ", _btnExitRect, _btnExitRect.Contains(Mouse.GetState().Position));
        }
        else if (_currentState == TitleState.SaveSelection)
        {
             // Draw Slots
             for (int i = 0; i < 3; i++)
             {
                Rectangle r = _slotRects[i];
                bool isHover = _hoveredSlot == i;
                Color bgColor = isHover ? new Color(60, 60, 80) : new Color(40, 40, 50);
                Color borderColor = isHover ? Color.Yellow : Color.Gray;

                spriteBatch.Draw(_panelTexture, r, bgColor);
                DrawBorder(spriteBatch, r, borderColor, 2);

                SaveData data = _saveSlots[i];
                if (data != null)
                {
                    spriteBatch.DrawString(font, $"SLOT {i+1}: {data.PlayerName}", new Vector2(r.X + 20, r.Y + 20), Color.White);
                    spriteBatch.DrawString(font, $"Level {data.Level} | Gold: {data.Gold}", new Vector2(r.X + 20, r.Y + 50), Color.LightGray);
                    
                    // Draw Delete Button
                    Rectangle delRect = _btnDeleteRects[i];
                    bool delHover = delRect.Contains(Mouse.GetState().Position);
                    spriteBatch.Draw(_panelTexture, delRect, delHover ? Color.Red : Color.DarkRed);
                    DrawBorder(spriteBatch, delRect, Color.White, 1);
                    spriteBatch.DrawString(font, "X", new Vector2(delRect.Center.X - font.MeasureString("X").X/2, delRect.Center.Y - font.MeasureString("X").Y/2), Color.White);
                }
                else
                {
                    spriteBatch.DrawString(font, $"SLOT {i+1}: [ BOŞ ]", new Vector2(r.X + 20, r.Y + 35), Color.Gray);
                     if (isHover) spriteBatch.DrawString(font, "(Yeni Oluştur)", new Vector2(r.Right - 150, r.Y + 35), Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                }
             }
             
             // Back Button
             DrawButton(spriteBatch, font, "GERİ", _btnBackRect, _btnBackRect.Contains(Mouse.GetState().Position));
        }
        else if (_currentState == TitleState.Settings)
        {
            // Settings UI
            string header = "AYARLAR";
            Vector2 hSz = font.MeasureString(header);
            spriteBatch.DrawString(font, header, new Vector2((_screenWidth - hSz.X)/2, 150), Color.White);
            
            // Music
            DrawSlider(spriteBatch, font, "MÜZİK", _musicVolumeBarRect, _musicVolumeKnobRect, MusicManager.Instance?.MasterVolume ?? 0.5f);
            
            // SFX
            DrawSlider(spriteBatch, font, "SES EFEKT", _sfxVolumeBarRect, _sfxVolumeKnobRect, SoundEffect.MasterVolume);
            
            // Back
            DrawButton(spriteBatch, font, "GERİ", _btnBackRect, _btnBackRect.Contains(Mouse.GetState().Position));
        }
        
        // Modal
        if (IsNameInputActive)
        {
            // Dim
             spriteBatch.Draw(_panelTexture, new Rectangle(0, 0, _screenWidth, _screenHeight), new Color(0, 0, 0, 200));
             
             int boxW = 400; int boxH = 200;
             Rectangle boxRect = new Rectangle((_screenWidth - boxW)/2, (_screenHeight - boxH)/2, boxW, boxH);
             spriteBatch.Draw(_panelTexture, boxRect, new Color(50, 50, 60));
             DrawBorder(spriteBatch, boxRect, Color.Cyan, 2);
             
             string prompt = "KARAKTER İSMİ GİRİN:";
             Vector2 pSize = font.MeasureString(prompt);
             spriteBatch.DrawString(font, prompt, new Vector2(boxRect.X + (boxW-pSize.X)/2, boxRect.Y + 40), Color.Cyan);
             
             string display = _newPlayerName + "_";
             Vector2 nSize = font.MeasureString(display);
             spriteBatch.DrawString(font, display, new Vector2(boxRect.X + (boxW-nSize.X)/2, boxRect.Y + 80), Color.White);
             
             DrawButton(spriteBatch, font, "BAŞLA", _btnStartRect, _hoverStart);
             DrawButton(spriteBatch, font, "İPTAL", _btnCancelRect, _hoverCancel, Color.Red);
        }
    }
    
    private void DrawButton(SpriteBatch sb, SpriteFont font, string text, Rectangle rect, bool hover, Color? baseColor = null)
    {
        sb.Draw(_panelTexture, rect, hover ? Color.DarkGray : (baseColor ?? Color.Black));
        DrawBorder(sb, rect, hover ? Color.Gold : Color.Gray, 2);
        
        Vector2 sz = font.MeasureString(text);
        sb.DrawString(font, text, new Vector2(rect.Center.X - sz.X/2, rect.Center.Y - sz.Y/2), Color.White);
    }
    
    private void DrawBorder(SpriteBatch sb, Rectangle r, Color c, int thickness)
    {
        sb.Draw(_panelTexture, new Rectangle(r.X, r.Y, r.Width, thickness), c);
        sb.Draw(_panelTexture, new Rectangle(r.X, r.Bottom-thickness, r.Width, thickness), c);
        sb.Draw(_panelTexture, new Rectangle(r.X, r.Y, thickness, r.Height), c);
        sb.Draw(_panelTexture, new Rectangle(r.Right-thickness, r.Y, thickness, r.Height), c);
    }
    
    private void DrawSlider(SpriteBatch sb, SpriteFont font, string label, Rectangle bar, Rectangle knob, float val)
    {
        string text = $"{label}: %{(int)(val*100)}";
        Vector2 sz = font.MeasureString(text);
        sb.DrawString(font, text, new Vector2(bar.X - sz.X - 15, bar.Y - sz.Y/2 + 5), Color.White);
        
        sb.Draw(_panelTexture, bar, Color.Gray);
        sb.Draw(_panelTexture, new Rectangle(bar.X, bar.Y, (int)(bar.Width * val), bar.Height), Color.Lime);
        sb.Draw(_panelTexture, knob, Color.White);
    }
}
