using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EternalJourney;

public class Camera2D
{
    private readonly Viewport _viewport;
    
    public Vector2 Position { get; set; }
    public float Zoom { get; set; }
    public float Rotation { get; set; }
    public Vector2 Origin { get; set; }
    
    // Harita sınırları
    private int _mapWidth;
    private int _mapHeight;

    public Camera2D(Viewport viewport, int mapWidth, int mapHeight)
    {
        _viewport = viewport;
        _mapWidth = mapWidth;
        _mapHeight = mapHeight;
        
        Rotation = 0;
        Zoom = 1;
        Origin = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
        Position = Vector2.Zero;
    }

    public void Update(Vector2 targetPosition)
    {
        // Hedefi takip et
        Position = targetPosition;
        
        // Sınırların dışına çıkmayı engelle (Clamp)
        // Kamera merkezi ekranın ortası (Origin).
        // Sol üst köşe Position - Origin.
        
        // Kameranın görebileceği sol sınır: Origin.X
        // Kameranın görebileceği sağ sınır: MapWidth - Origin.X
        
        float minX = Origin.X;
        float maxX = _mapWidth - Origin.X;
        float minY = Origin.Y;
        float maxY = _mapHeight - Origin.Y;
        
        // Eğer harita ekrandan küçükse ortala
        if (_mapWidth < _viewport.Width)
            Position = new Vector2(_mapWidth / 2f, Position.Y);
        else
            Position = new Vector2(MathHelper.Clamp(Position.X, minX, maxX), Position.Y);
            
        if (_mapHeight < _viewport.Height)
            Position = new Vector2(Position.X, _mapHeight / 2f);
        else
            Position = new Vector2(Position.X, MathHelper.Clamp(Position.Y, minY, maxY));
    }

    public Matrix GetViewMatrix()
    {
        return 
            Matrix.CreateTranslation(new Vector3(-Position, 0.0f)) *
            Matrix.CreateRotationZ(Rotation) *
            Matrix.CreateScale(Zoom, Zoom, 1) *
            Matrix.CreateTranslation(new Vector3(Origin, 0.0f));
    }
    
    // Ekran koordinatlarını dünya koordinatlarına çevir
    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return Vector2.Transform(screenPosition, Matrix.Invert(GetViewMatrix()));
    }
    
    public void SetMapSize(int width, int height)
    {
        _mapWidth = width;
        _mapHeight = height;
    }

    public void LookAt(Vector2 targetPosition)
    {
        Position = targetPosition;
        Update(targetPosition); // Apply clamping immediately
    }
}
