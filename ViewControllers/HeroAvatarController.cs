using System;
using System.Collections.Generic;
using DeskWarrior.Helpers;
using DeskWarrior.Models;

namespace DeskWarrior.ViewControllers
{
    public class HeroAvatarController : IDisposable
    {
        private readonly MainWindow _window;
        private readonly Random _random = new();
        
        private HeroData? _currentHero;
        private System.Windows.Threading.DispatcherTimer? _heroAttackTimer;

        public HeroAvatarController(MainWindow window)
        {
            _window = window;
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _heroAttackTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _heroAttackTimer.Tick += HeroAttackTimer_Tick;
        }

        public void LoadCharacterImages(List<HeroData> heroes)
        {
            try
            {
                if (heroes != null && heroes.Count > 0)
                {
                    _currentHero = heroes[_random.Next(heroes.Count)];
                    _window.HeroImage.Source = ImageHelper.LoadWithChromaKey(
                        $"pack://application:,,,/Assets/Images/{_currentHero.IdleSprite}.png");
                }
            }
            catch (Exception ex)
            {
               // Log error if needed, or silent fail as before
            }
        }

        public void ShowHeroAttackSprite()
        {
            if (_currentHero == null) return;
            
            _heroAttackTimer?.Stop();
            
            try 
            {
                _window.HeroImage.Source = ImageHelper.LoadWithChromaKey(
                    $"pack://application:,,,/Assets/Images/{_currentHero.AttackSprite}.png");
            }
            catch { }

            _heroAttackTimer?.Start();
        }

        private void HeroAttackTimer_Tick(object? sender, EventArgs e)
        {
            _heroAttackTimer?.Stop();
            if (_currentHero != null)
            {
                try
                {
                    _window.HeroImage.Source = ImageHelper.LoadWithChromaKey(
                        $"pack://application:,,,/Assets/Images/{_currentHero.IdleSprite}.png");
                }
                catch { }
            }
        }

        public void Dispose()
        {
            _heroAttackTimer?.Stop();
        }
    }
}
