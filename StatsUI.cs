using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EternalJourney
{
    public class StatsUI
    {
        private Texture2D _backgroundTexture;
        private int _screenWidth;
        private int _screenHeight;
        
        public StatsUI(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            
            _backgroundTexture = new Texture2D(graphicsDevice, 1, 1);
            _backgroundTexture.SetData(new[] { new Color(0, 0, 0, 200) });
        }
        
        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Player player)
        {
            int width = 450;
            int height = 550;
            Rectangle bounds = new Rectangle(
                (_screenWidth - width) / 2,
                (_screenHeight - height) / 2,
                width, height
            );
            
            // Background
            spriteBatch.Draw(_backgroundTexture, bounds, Color.White);
            
            // Title
            string title = "KARAKTER ÖZELLİKLERİ";
            Vector2 titleSize = font.MeasureString(title);
            spriteBatch.DrawString(font, title, 
                new Vector2(bounds.X + (width - titleSize.X)/2, bounds.Y + 20), 
                Color.Gold);
                
            // Stats List
            Vector2 pos = new Vector2(bounds.X + 40, bounds.Y + 80);
            float spacing = 35f;
            
            DrawStat(spriteBatch, font, "Seviye:", player.Level.ToString(), pos, Color.White);
            pos.Y += spacing;
            
            DrawStat(spriteBatch, font, "Tecrübe:", $"{player.Experience} / {player.MaxExperience}", pos, Color.LightGray);
            pos.Y += spacing;
            
            DrawStat(spriteBatch, font, "Sağlık:", $"{player.CurrentHealth} / {player.MaxHealth}", pos, Color.LightGreen);
            pos.Y += spacing;
            
            DrawStat(spriteBatch, font, "Altın:", player.Gold.ToString(), pos, Color.Yellow);
            pos.Y += spacing + 10;
            
            // Combat Stats
            int minDmg = player.GetMinDamage();
            int maxDmg = player.GetMaxDamage();
            DrawStat(spriteBatch, font, "Hasar:", $"{minDmg} - {maxDmg}", pos, Color.Red);
            pos.Y += spacing;
            
            int def = player.GetTotalDefense();
            DrawStat(spriteBatch, font, "Savunma:", $"{def} (-{MathHelper.Clamp(def, 0, 90)}%)", pos, Color.Cyan);
            pos.Y += spacing;

            int block = player.GetBlockChance();
            DrawStat(spriteBatch, font, "Bloklama:", $"%{block}", pos, Color.LightBlue);
            pos.Y += spacing;

            // Footer
            string hint = "[C] Kapat";
            Vector2 hintSize = font.MeasureString(hint);
            spriteBatch.DrawString(font, hint, 
                new Vector2(bounds.X + (width - hintSize.X)/2, bounds.Y + height - 40), 
                Color.Gray);
        }
        
        private void DrawStat(SpriteBatch sb, SpriteFont font, string label, string value, Vector2 pos, Color valueColor)
        {
            sb.DrawString(font, label, pos, Color.LightGray);
            sb.DrawString(font, value, new Vector2(pos.X + 150, pos.Y), valueColor);
        }
    }
}
