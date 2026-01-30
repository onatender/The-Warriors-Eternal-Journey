using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EternalJourney
{
    public class ReviveUI
    {
        private GraphicsDevice _graphicsDevice;
        private int _screenWidth;
        private int _screenHeight;
        private Texture2D _pixelTexture;
        
        public bool IsOpen { get; private set; }
        
        private Rectangle _windowRect;
        private Rectangle _reviveButtonRect;
        private Rectangle _townButtonRect;
        
        private int _reviveCost;
        private int _townCost;
        private bool _canAfford;
        private Player _player;
        
        public event Action OnReviveClicked;
        public event Action OnTownClicked;
        
        private Color _overlayColor = new Color(0, 0, 0, 200);
        
        public ReviveUI(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
        {
            _graphicsDevice = graphicsDevice;
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            
            CalculateLayout();
        }
        
        public void UpdateScreenSize(int width, int height)
        {
            _screenWidth = width;
            _screenHeight = height;
            CalculateLayout();
        }
        
        private void CalculateLayout()
        {
            int width = 400;
            int height = 250;
            
            _windowRect = new Rectangle(
                (_screenWidth - width) / 2,
                (_screenHeight - height) / 2,
                width,
                height
            );
            
            int btnWidth = 240;
            int btnHeight = 50;
            
            _reviveButtonRect = new Rectangle(
                _windowRect.X + (_windowRect.Width - btnWidth) / 2,
                _windowRect.Y + 110,
                btnWidth,
                btnHeight
            );
            
            _townButtonRect = new Rectangle(
                _windowRect.X + (_windowRect.Width - btnWidth) / 2,
                _reviveButtonRect.Bottom + 20,
                btnWidth,
                btnHeight
            );
        }
        
        public void Open(Player player)
        {
            _player = player;
            IsOpen = true;
            
            // Calculate Cost:
            // Revive Here: 20% of Gold
            // Town Return: 10% of Gold
            _reviveCost = (int)(_player.Gold * 0.20f);
            _townCost = (int)(_player.Gold * 0.10f);
            
            // Always afford (it's a penalty on what you have)
            // But maybe we want to show 0 if 0 gold.
            _canAfford = true; 
        }
        
        public void Close()
        {
            IsOpen = false;
        }
        
        public void Update(GameTime gameTime, MouseState mouseState)
        {
            if (!IsOpen) return;
            
            Point mousePos = mouseState.Position;
            
            if (_reviveButtonRect.Contains(mousePos))
            {
                if (mouseState.LeftButton == ButtonState.Pressed && _canAfford)
                {
                    OnReviveClicked?.Invoke();
                    Close();
                }
            }
            
            if (_townButtonRect.Contains(mousePos))
            {
                if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                {
                    OnTownClicked?.Invoke();
                    Close();
                }
            }
            
            _previousMouseState = mouseState;
        }
        
        public int GetCost() => _reviveCost;
        public int GetTownCost() => _townCost;
        
        private MouseState _previousMouseState;

        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (!IsOpen) return;
            
            // Full screen dark overlay
            spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, _screenWidth, _screenHeight), _overlayColor);
            
            // Window Background
            spriteBatch.Draw(_pixelTexture, _windowRect, new Color(30, 30, 30));
            // Border
            spriteBatch.Draw(_pixelTexture, new Rectangle(_windowRect.X - 2, _windowRect.Y - 2, _windowRect.Width + 4, _windowRect.Height + 4), Color.Gray); // Fixed Draw arguments
            
            // Title: "YOU DIED"
            string title = "OLDUNUZ";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(
                _windowRect.X + (_windowRect.Width - titleSize.X) / 2,
                _windowRect.Y + 20
            );
            spriteBatch.DrawString(font, title, titlePos, Color.Red);
            
            // Message: "Revive for X Gold"
            string msg = $"Oldugun Yerde Diril: {_reviveCost} Altin (%20)";
            string msg2 = $"Sehre Don: {_townCost} Altin (%10)";
            
            Vector2 msgSize = font.MeasureString(msg);
            Vector2 msgPos = new Vector2(
                _windowRect.X + (_windowRect.Width - msgSize.X) / 2,
                _windowRect.Y + 55
            );
            spriteBatch.DrawString(font, msg, msgPos, Color.Gold);
            
            Vector2 msg2Size = font.MeasureString(msg2);
            Vector2 msg2Pos = new Vector2(
                _windowRect.X + (_windowRect.Width - msg2Size.X) / 2,
                _windowRect.Y + 80
            );
            spriteBatch.DrawString(font, msg2, msg2Pos, Color.Silver);
            
            // Revive Button
            DrawButton(spriteBatch, font, _reviveButtonRect, "DIRIL", Color.Green);
            
            // Town Button
            DrawButton(spriteBatch, font, _townButtonRect, "SEHRE DON", Color.CornflowerBlue);
        }
        
        private void DrawButton(SpriteBatch spriteBatch, SpriteFont font, Rectangle rect, string text, Color color)
        {
            spriteBatch.Draw(_pixelTexture, rect, color);
            
            // Border
            spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, 2), Color.White);
            spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), Color.White);
            spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, 2, rect.Height), Color.White);
            spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), Color.White);
            
            Vector2 textSize = font.MeasureString(text);
            Vector2 textPos = new Vector2(
                rect.X + (rect.Width - textSize.X) / 2,
                rect.Y + (rect.Height - textSize.Y) / 2
            );
            spriteBatch.DrawString(font, text, textPos, Color.White);
        }
    }
}
