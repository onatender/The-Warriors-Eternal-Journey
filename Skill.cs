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
                Cooldown = 5f,
                KeyDisplay = "1",
                Icon = CreateWhirlwindIcon(_graphicsDevice)
            });

            // Skill 2: Dash Strike (Zincirleme Saldırı)
            Skills.Add(new Skill
            {
                Type = SkillType.DashStrike,
                Name = "Dash Strike",
                Description = "Dash to nearest enemies in a chain reaction.",
                Cooldown = 5f,
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
                    data[i] = new Color(30, 20, 40); // Dark Background

                    // 1. Spinning Sweep Effect (Red/Orange Trail)
                    float angle = (float)Math.Atan2(y - center.Y, x - center.X);
                    // Normalize angle
                    if(angle < 0) angle += MathHelper.TwoPi;

                    // Spiral shape for swipe
                    float spiralOffset = (dist / 20f); 
                    float spiralAngle = (angle + spiralOffset) % MathHelper.TwoPi;
                    
                    if (dist > 10 && dist < 28)
                    {
                        if (spiralAngle > 0 && spiralAngle < 2.5f)
                        {
                            data[i] = Color.Lerp(Color.OrangeRed, Color.Transparent, spiralAngle / 3f);
                        }
                    }

                    // 2. Sword Blade (Curved)
                    // Basit bir kılıç şekli çizmek zor, o yüzden "dönme hissi" veren 2 kavisli çizgi
                    if (Math.Abs(dist - 20) < 3) 
                    {
                        data[i] = Color.Silver; // Blade track
                    }
                    
                    // Sword Hilt (Center)
                    if (dist < 6) data[i] = Color.Gold;

                    // Border
                    if (x == 0 || y == 0 || x == size - 1 || y == size - 1) data[i] = new Color(100, 100, 100);
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
                    data[i] = new Color(20, 30, 40); // Dark Blueish BG

                    // Koordinatları döndür (45 derece) - Kılıç çapraz olsun
                    // x' = x cos a - y sin a
                    // y' = x sin a + y cos a
                    // Merkez etrafında
                    float cx = x - 32;
                    float cy = y - 32;
                    // 45 deg = PI/4
                    float rx = cx * 0.707f - cy * 0.707f;
                    float ry = cx * 0.707f + cy * 0.707f;

                    // Sword Blade (Vertical in rotated space -> ry ekseni boyunca)
                    // Genişlik (rx) az, Uzunluk (ry) eksi yönde
                    
                    bool isBlade = Math.Abs(rx) < 4 && ry < 20 && ry > -25;
                    bool isGuard = Math.Abs(rx) < 10 && ry >= 20 && ry <= 23;
                    bool isHilt = Math.Abs(rx) < 2 && ry > 23 && ry < 30;

                    if (isBlade) 
                    {
                        if (Math.Abs(rx) < 2) data[i] = Color.White; // Sharp edge
                        else data[i] = Color.Silver;
                    }
                    else if (isGuard || isHilt) data[i] = Color.Gold;
                    
                    // Motion Lines (Arkasında)
                    // Orijinal koordinatlarda (x,y) kontrol etmek bazen daha kolay
                    // Ama rotated'da ry > 25 (kılıcın arkası)
                    if (ry > 25 && Math.Abs(rx) > 5 && Math.Abs(rx) < 15 && ry < 45)
                    {
                        // Hız çizgileri (Cyan/White)
                        if ((int)ry % 4 != 0) data[i] = Color.FromNonPremultiplied(100, 255, 255, 150);
                    }

                    // Border
                    if (x == 0 || y == 0 || x == size - 1 || y == size - 1) data[i] = new Color(100, 100, 100);
                }
            }
            texture.SetData(data);
            return texture;
        }
    }
}
