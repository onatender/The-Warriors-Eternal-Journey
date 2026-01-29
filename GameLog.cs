using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EternalJourney;

public class LogMessage
{
    public string Text { get; set; }
    public Color Color { get; set; }
    public float LifeTime { get; set; } = 5.0f; // 5 saniye ekranda kal
    public float Alpha { get; set; } = 1.0f;
}

public class GameLog
{
    private List<LogMessage> _messages;
    private int _maxMessages = 10;
    private int _screenHeight;
    private Vector2 _position;
    
    public GameLog(int screenHeight)
    {
        _messages = new List<LogMessage>();
        _screenHeight = screenHeight;
        _position = new Vector2(20, screenHeight - 150); // Sol alt köşe (Envanter ipucunun üstü)
    }
    
    public void AddMessage(string text, Color color)
    {
        // Mesajı en başa ekle (Yeni mesajlar altta veya üstte, biz alttan yukarı kaymasını istiyoruz)
        // Ya da standart chat gibi: Yeni mesaj en alta gelir, eskiler yukarı kayar.
        // Ama pozisyonumuz sabitse, listeyi ters çizebiliriz.
        
        _messages.Add(new LogMessage { Text = text, Color = color });
        
        if (_messages.Count > _maxMessages)
        {
            _messages.RemoveAt(0); // En eski mesajı sil
        }
    }
    
    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        for (int i = _messages.Count - 1; i >= 0; i--)
        {
            _messages[i].LifeTime -= dt;
            
            // Son 1 saniye fade out
            if (_messages[i].LifeTime < 1.0f)
            {
                _messages[i].Alpha = _messages[i].LifeTime;
            }
            
            if (_messages[i].LifeTime <= 0)
            {
                _messages.RemoveAt(i);
            }
        }
    }
    
    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        // Mesajları çiz (En yeni mesaj en altta)
        // Başlangıç Y: _position.Y
        // Her satır yukarı doğru gitsin: _position.Y - (index * satırYüksekliği)
        
        // Listede 0 en eski, Count-1 en yeni.
        // Biz en yeni mesajı en altta görmek istiyoruz.
        
        int lineHeight = 20;
        int currentY = (int)_position.Y;
        
        // Tersten döngü: En yeni (son) mesajdan en eskiye
        for (int i = _messages.Count - 1; i >= 0; i--)
        {
            LogMessage msg = _messages[i];
            
            Color drawColor = msg.Color * msg.Alpha;
            
            // Gölge
            spriteBatch.DrawString(font, msg.Text, new Vector2(_position.X + 1, currentY + 1), Color.Black * msg.Alpha * 0.7f);
            
            // Metin
            spriteBatch.DrawString(font, msg.Text, new Vector2(_position.X, currentY), drawColor);
            
            currentY -= lineHeight; // Yukarı çık
        }
    }
}
