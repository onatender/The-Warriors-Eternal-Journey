using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EternalJourney
{
    public class SkillBarUI
    {
        private SkillManager _skillManager;
        private Inventory _inventory;
        private GraphicsDevice _graphicsDevice;
        private Texture2D _slotTexture;
        private Texture2D _cooldownOverlay;
        
        private int _screenWidth;
        private int _screenHeight;
        
        private const int SLOT_SIZE = 64;
        private const int PADDING = 10;
        
        private Rectangle _bounds;

        public SkillBarUI(GraphicsDevice graphicsDevice, SkillManager skillManager, Inventory inventory, int screenWidth, int screenHeight)
        {
            _graphicsDevice = graphicsDevice;
            _skillManager = skillManager;
            _inventory = inventory;
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            
            _slotTexture = CreateSlotTexture(_graphicsDevice);
            _cooldownOverlay = CreateCooldownTexture(_graphicsDevice);
            
            CalculatePosition();
        }
        
        public void UpdateScreenSize(int width, int height)
        {
            _screenWidth = width;
            _screenHeight = height;
            CalculatePosition();
        }
        
        private void CalculatePosition()
        {
            int totalWidth = _skillManager.Skills.Count * (SLOT_SIZE + PADDING) - PADDING;
            int startX = (_screenWidth - totalWidth) / 2;
            int startY = _screenHeight - SLOT_SIZE - 20; // Bottom center
            
            _bounds = new Rectangle(startX, startY, totalWidth, SLOT_SIZE);
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            int startX = _bounds.X;
            int y = _bounds.Y;

            for (int i = 0; i < _skillManager.Skills.Count; i++)
            {
                var skill = _skillManager.Skills[i];
                Rectangle slotRect = new Rectangle(startX + i * (SLOT_SIZE + PADDING), y, SLOT_SIZE, SLOT_SIZE);

                // Draw Slot Background
                spriteBatch.Draw(_slotTexture, slotRect, Color.White);

                // Draw Icon
                if (skill.Icon != null)
                {
                    spriteBatch.Draw(skill.Icon, 
                        new Rectangle(slotRect.X + 4, slotRect.Y + 4, SLOT_SIZE - 8, SLOT_SIZE - 8), 
                        Color.White);
                }

                // Draw Cooldown Overlay
                if (skill.CurrentCooldown > 0)
                {
                    float ratio = skill.CurrentCooldown / skill.Cooldown;
                    int cooldownHeight = (int)((SLOT_SIZE - 8) * ratio);
                    
                    Rectangle overlayRect = new Rectangle(
                        slotRect.X + 4, 
                        slotRect.Y + 4 + (SLOT_SIZE - 8) - cooldownHeight, 
                        SLOT_SIZE - 8, 
                        cooldownHeight);
                        
                    spriteBatch.Draw(_cooldownOverlay, overlayRect, new Color(0, 0, 0, 150));
                    
                    // Draw Cooldown Text
                    string cdText = skill.CurrentCooldown.ToString("0.0");
                    Vector2 textSize = font.MeasureString(cdText);
                    Vector2 textPos = new Vector2(
                        slotRect.Center.X - textSize.X / 2, 
                        slotRect.Center.Y - textSize.Y / 2);
                        
                    spriteBatch.DrawString(font, cdText, textPos + new Vector2(1,1), Color.Black);
                    spriteBatch.DrawString(font, cdText, textPos, Color.White);
                }
                
                // Draw Key Binding
                spriteBatch.DrawString(font, skill.KeyDisplay, 
                    new Vector2(slotRect.X + 5, slotRect.Y + 2), 
                    Color.Yellow, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
            
            // --- POTION SLOT ---
            int potionX = startX + _skillManager.Skills.Count * (SLOT_SIZE + PADDING) + 20;
            Rectangle potRect = new Rectangle(potionX, y, SLOT_SIZE, SLOT_SIZE);
            
            // Background
            spriteBatch.Draw(_slotTexture, potRect, Color.White);
            
            Item potItem = _inventory.GetFirstPotion();
            int potCount = _inventory.GetPotionCount();
            
            if (potItem != null && potItem.Icon != null)
            {
                spriteBatch.Draw(potItem.Icon, 
                    new Rectangle(potRect.X + 4, potRect.Y + 4, SLOT_SIZE - 8, SLOT_SIZE - 8), 
                    Color.White);
            }
            
            // Key Binding (0)
            spriteBatch.DrawString(font, "0", 
                 new Vector2(potRect.X + 5, potRect.Y + 2), 
                 Color.Yellow, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                 
            // Quantity
            if (potCount > 0)
            {
                string qty = potCount.ToString();
                Vector2 sz = font.MeasureString(qty);
                Vector2 pos = new Vector2(potRect.Right - sz.X - 4, potRect.Bottom - sz.Y - 2);
                spriteBatch.DrawString(font, qty, pos + new Vector2(1,1), Color.Black);
                spriteBatch.DrawString(font, qty, pos, Color.White);
            }
            else
            {
                // Dim if empty
                spriteBatch.Draw(_cooldownOverlay, potRect, new Color(0, 0, 0, 150));
            }
        }

        private Texture2D CreateSlotTexture(GraphicsDevice gd)
        {
            Texture2D tex = new Texture2D(gd, SLOT_SIZE, SLOT_SIZE);
            Color[] data = new Color[SLOT_SIZE * SLOT_SIZE];
            Color border = new Color(100, 100, 120);
            Color bg = new Color(40, 40, 50);
            
            for(int i=0; i<data.Length; i++)
            {
                int x = i % SLOT_SIZE;
                int y = i / SLOT_SIZE;
                
                if (x < 3 || y < 3 || x > SLOT_SIZE - 4 || y > SLOT_SIZE - 4)
                    data[i] = border;
                else
                    data[i] = bg;
            }
            tex.SetData(data);
            return tex;
        }

        private Texture2D CreateCooldownTexture(GraphicsDevice gd)
        {
            Texture2D tex = new Texture2D(gd, 1, 1);
            tex.SetData(new[] { Color.Black });
            return tex;
        }
    }
}
