using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SCLogReader.Core;
using SCLogReader.ViewModels;

namespace SCLogReader.Views;

public partial class FloatingOverlayWindow : Window
{
    private AppSettings? _settings;

    public FloatingOverlayWindow()
    {
        InitializeComponent();
    }

    public void InitSettings(AppSettings settings)
    {
        _settings = settings;

        if (_settings.OverlayPositionX > 0 || _settings.OverlayPositionY > 0)
        {
            Position = new PixelPoint((int)_settings.OverlayPositionX, (int)_settings.OverlayPositionY);
        }

        Opacity = Math.Clamp(_settings.OverlayOpacity, 0.3, 1.0);

        PositionChanged += (_, _) =>
        {
            if (_settings != null)
            {
                _settings.OverlayPositionX = Position.X;
                _settings.OverlayPositionY = Position.Y;
                Settings.Save(_settings);
            }
        };
    }

    private void OnBorderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsOverlayActive = false;
        }
        else
        {
            Hide();
        }
    }
}
