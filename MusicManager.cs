using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace EternalJourney
{
    public class MusicManager
    {
        private enum MusicState
        {
            Stopped,
            FadingIn,
            Playing,
            FadingOut,
            Waiting
        }

        private Dictionary<int, SoundEffect> _tracks;
        private SoundEffectInstance _currentInstance;
        private int _currentMapIndex = -1;
        private MusicState _state = MusicState.Stopped;

        // Settings
        public float MasterVolume { get; private set; } = 0.5f;
        private const float FADE_DURATION = 3.0f; // 3 saniye fade
        private const float WAIT_DURATION = 10.0f; // Şarkı bitince 10 saniye bekle

        private float _timer = 0f;
        private float _currentVolume = 0f;
        
        public MusicManager()
        {
            _tracks = new Dictionary<int, SoundEffect>();
        }

        public void LoadContent(ContentManager content)
        {
            // Map 1: bg_2 (Başlangıç)
            _tracks[1] = content.Load<SoundEffect>("SFX/bg_2");
            
            // Map 2: bg_3 (Örümcek)
            _tracks[2] = content.Load<SoundEffect>("SFX/bg_3");
            
            // Map 3: bg_4 (Goblin)
            _tracks[3] = content.Load<SoundEffect>("SFX/bg_4");
            
            // Map 4: bg_1 (Şeytan/Final)
            _tracks[4] = content.Load<SoundEffect>("SFX/bg_1");
        }

        public void PlayMusicForMap(int mapIndex)
        {
            if (_currentMapIndex == mapIndex) return; // Aynı harita, değişiklik yok

            _currentMapIndex = mapIndex;

            // Eski müzik varsa hızlıca kapat
            if (_currentInstance != null)
            {
                _currentInstance.Stop();
                _currentInstance.Dispose();
                _currentInstance = null;
            }

            // Yeni mape geçişte hemen başlama, FadeIn ile başla
            if (_tracks.ContainsKey(mapIndex))
            {
                StartTrack(_tracks[mapIndex]);
            }
            else
            {
                _state = MusicState.Stopped;
            }
        }

        private void StartTrack(SoundEffect effect)
        {
            _currentInstance = effect.CreateInstance();
            _currentInstance.Volume = 0f;
            _currentInstance.IsLooped = false; // Loop kapalı, biz yöneteceğiz
            _currentInstance.Play();

            _state = MusicState.FadingIn;
            _currentVolume = 0f;
            _timer = 0f;
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_currentInstance == null && _state != MusicState.Stopped && _state != MusicState.Waiting) 
                return;

            switch (_state)
            {
                case MusicState.FadingIn:
                    _timer += dt;
                    _currentVolume = MathHelper.Lerp(0f, MasterVolume, _timer / FADE_DURATION);
                    if (_currentInstance != null) _currentInstance.Volume = _currentVolume;

                    if (_timer >= FADE_DURATION)
                    {
                        _state = MusicState.Playing;
                        _currentVolume = MasterVolume;
                    }
                    break;

                case MusicState.Playing:
                    // Şarkı bitti mi kontrol et
                    if (_currentInstance != null && _currentInstance.State == SoundState.Stopped)
                    {
                        _state = MusicState.Waiting;
                        _timer = 0f;
                    }
                    else
                    {
                        // Şarkı bitimine doğru FadeOut (Son 3 saniye kala mesela)
                        // SoundEffectInstance süresini tam bilemeyebiliriz, o yüzden şarkı bitince Wait'e geçip
                        // Wait'ten sonra tekrar başlatırken fade-in yapmak daha güvenli.
                        // Kullanıcı isteği: 1 loop çal -> bekle -> tekrar yavaşça gir
                        
                        // Şarkı kendiliğinden bitince 'Waiting' durumuna geçiyoruz.
                    }
                    break;

                case MusicState.FadingOut:
                    // Bu durum harita değişimi veya manuel durdurma için, şu an hızlı geçiş yapıyoruz
                    // Ama şarkı bitip tekrar başlamadan önce fade out istenirse burası kullanılır.
                    // Kullanıcı senaryosunda: "azalarak gidecek (bitişte)" dememiş, "yavaşça girecek... sonra geri gelecek" demiş.
                    // Şarkı doğal bitişiyle biterse FadingOut'a gerek yok, direkt biter.
                    break;

                case MusicState.Waiting:
                    _timer += dt;
                    if (_timer >= WAIT_DURATION)
                    {
                        // Bekleme süresi bitti, tekrar aynı şarkıyı başlat
                        if (_tracks.ContainsKey(_currentMapIndex))
                        {
                            StartTrack(_tracks[_currentMapIndex]);
                        }
                    }
                    break;
            }
        }
        
        public void Stop()
        {
            if (_currentInstance != null)
            {
                _currentInstance.Stop();
                _currentInstance = null;
            }
            _state = MusicState.Stopped;
        }


        public void IncreaseVolume()
        {
            MasterVolume = Math.Clamp(MasterVolume + 0.1f, 0.0f, 1.0f);
            if (_currentInstance != null && _state == MusicState.Playing)
                _currentInstance.Volume = MasterVolume;
        }

        public void DecreaseVolume()
        {
            MasterVolume = Math.Clamp(MasterVolume - 0.1f, 0.0f, 1.0f);
            if (_currentInstance != null && _state == MusicState.Playing)
                _currentInstance.Volume = MasterVolume;
        }
    }
}
