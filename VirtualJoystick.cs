using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EternalJourney
{
    public class VirtualJoystick
    {
        private Vector2 _basePos;
        private Vector2 _knobPos;
        private float _baseRadius;
        private float _knobRadius;
        private bool _isDragging;
        private int _activeFingerId = -1;

        private Texture2D _circleTexture;
        private GraphicsDevice _graphicsDevice;

        public Vector2 InputDirection { get; private set; }

        public VirtualJoystick(GraphicsDevice gd, Vector2 position, float radius)
        {
            _graphicsDevice = gd;
            _basePos = position;
            _knobPos = position;
            _baseRadius = radius;
            _knobRadius = radius * 0.4f;
            _circleTexture = CreateCircleTexture(gd, (int)radius * 2);
        }

        private Texture2D CreateCircleTexture(GraphicsDevice gd, int size)
        {
            Texture2D texture = new Texture2D(gd, size, size);
            Color[] data = new Color[size * size];
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius;
                    float dy = y - radius;
                    if (dx * dx + dy * dy < radius * radius)
                        data[y * size + x] = Color.White;
                    else
                        data[y * size + x] = Color.Transparent;
                }
            }
            texture.SetData(data);
            return texture;
        }

        public void Update(MouseState currentMouse, MouseState previousMouse)
        {
            Vector2 mousePos = new Vector2(currentMouse.X, currentMouse.Y);
            
            if (currentMouse.LeftButton == ButtonState.Pressed)
            {
                float dist = Vector2.Distance(mousePos, _basePos);
                if (!_isDragging && dist < _baseRadius)
                {
                    _isDragging = true;
                }

                if (_isDragging)
                {
                    Vector2 dir = mousePos - _basePos;
                    float length = dir.Length();
                    if (length > _baseRadius)
                    {
                        dir.Normalize();
                        _knobPos = _basePos + dir * _baseRadius;
                    }
                    else
                    {
                        _knobPos = mousePos;
                    }

                    InputDirection = (_knobPos - _basePos) / _baseRadius;
                }
            }
            else
            {
                _isDragging = false;
                _knobPos = _basePos;
                InputDirection = Vector2.Zero;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Base
            spriteBatch.Draw(_circleTexture, _basePos - new Vector2(_baseRadius), null, 
                new Color(255, 255, 255, 50), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            
            // Knob
            float knobScale = (_knobRadius * 2) / _circleTexture.Width;
            spriteBatch.Draw(_circleTexture, _knobPos - new Vector2(_knobRadius), null, 
                new Color(255, 255, 255, 150), 0f, Vector2.Zero, knobScale, SpriteEffects.None, 0f);
        }
    }
}
