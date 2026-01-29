using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EternalJourney
{
    public enum SkillType
    {
        Whirlwind,
        DashStrike
    }

    public class Skill
    {
        public SkillType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public float Cooldown { get; set; }
        public float CurrentCooldown { get; set; } = 0f;
        public Texture2D Icon { get; set; }
        public int ManaCost { get; set; } = 0;
        
        // Input Key (Display purposes)
        public string KeyDisplay { get; set; }

        public bool IsReady => CurrentCooldown <= 0f;

        public void Update(GameTime gameTime)
        {
            if (CurrentCooldown > 0)
            {
                CurrentCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (CurrentCooldown < 0) CurrentCooldown = 0;
            }
        }

        public void Use()
        {
            CurrentCooldown = Cooldown;
        }
    }

    public class SkillManager
    {
        public List<Skill> Skills { get; private set; }
        private GraphicsDevice _graphicsDevice;

        public SkillManager(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            Skills = new List<Skill>();
            InitializeSkills();
        }

        private void InitializeSkills()
        {
            // Skill 1: Whirlwind (Döne Döne Vurma)
            Skills.Add(new Skill
            {
                Type = SkillType.Whirlwind,
                Name = "Whirlwind",
                Description = "Spin around dealing damage to all nearby enemies.",
                Cooldown = 1f, // Test mode: 1s
                KeyDisplay = "1",
                Icon = CreateWhirlwindIcon(_graphicsDevice)
            });

            // Skill 2: Dash Strike (Zincirleme Saldırı)
            Skills.Add(new Skill
            {
                Type = SkillType.DashStrike,
                Name = "Dash Strike",
                Description = "Dash to nearest enemies in a chain reaction.",
                Cooldown = 1f, // Test mode: 1s
                KeyDisplay = "2",
                Icon = CreateDashIcon(_graphicsDevice)
            });
        }

        public void Update(GameTime gameTime)
        {
            foreach (var skill in Skills)
            {
                skill.Update(gameTime);
            }
        }

        public Skill GetSkill(SkillType type)
        {
            return Skills.Find(s => s.Type == type);
        }

        // --- ICON GENERATION ---
        private Texture2D CreateWhirlwindIcon(GraphicsDevice gd)
        {
            int size = 64;
            Texture2D texture = new Texture2D(gd, size, size);
            Color[] data = new Color[size * size];

            Vector2 center = new Vector2(size / 2, size / 2);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int i = y * size + x;
                    Vector2 pos = new Vector2(x, y);
                    float dist = Vector2.Distance(pos, center);
                    
                    // Spiral Pattern
                    float angle = (float)Math.Atan2(y - center.Y, x - center.X);
                    float spiral = (angle + dist * 0.2f) % (MathHelper.TwoPi);
                    
                    if (dist < 28 && Math.Abs(spiral - MathHelper.Pi) < 0.5f)
                    {
                         data[i] = Color.OrangeRed;
                         if (dist < 15) data[i] = Color.Yellow;
                    }
                    else
                    {
                        data[i] = new Color(30, 30, 40); // Dark BG
                    }
                    
                    // Border
                    if (x == 0 || y == 0 || x == size - 1 || y == size - 1) data[i] = Color.Gray;
                }
            }
            texture.SetData(data);
            return texture;
        }

        private Texture2D CreateDashIcon(GraphicsDevice gd)
        {
            int size = 64;
            Texture2D texture = new Texture2D(gd, size, size);
            Color[] data = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int i = y * size + x;
                    data[i] = new Color(30, 30, 40); // Dark BG

                    // Lightning Bolt / Dash Arrow
                    // Simple diagonal shape
                    if (Math.Abs(x - y) < 8 && x > 10 && x < 54 && y > 10 && y < 54)
                    {
                        data[i] = Color.Cyan;
                    }
                    
                    // Border
                    if (x == 0 || y == 0 || x == size - 1 || y == size - 1) data[i] = Color.Gray;
                }
            }
            texture.SetData(data);
            return texture;
        }
    }
}
