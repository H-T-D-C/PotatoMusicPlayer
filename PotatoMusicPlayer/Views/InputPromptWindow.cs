using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using PotatoMusicPlayer.Services;

namespace PotatoMusicPlayer.Views
{
    public class InputPromptWindow : Window
    {
        private readonly TextBox _input;
        private readonly Func<string, bool> _isValid;
        private readonly Brush _buttonBackground;
        private readonly Brush _buttonHoverBackground;

        private InputPromptWindow(string title, string label, string initialValue, Func<string, bool> isValid)
        {
            Title = title;
            Width = 360;
            Height = 155;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(ThemeService.IsLightMode ? Color.FromRgb(243, 243, 243) : Color.FromRgb(30, 30, 30));
            Color foregroundColor = ThemeService.IsLightMode ? Color.FromRgb(37, 37, 37) : Color.FromRgb(220, 220, 220);
            Color inputColor = ThemeService.IsLightMode ? Color.FromRgb(232, 232, 232) : Color.FromRgb(62, 62, 62);
            Color borderColor = ThemeService.IsLightMode ? Color.FromRgb(47, 111, 176) : Color.FromRgb(74, 144, 226);
            _isValid = isValid;
            _buttonBackground = new SolidColorBrush(inputColor);
            _buttonHoverBackground = new SolidColorBrush(ThemeService.IsLightMode ? Color.FromRgb(218, 228, 239) : Color.FromRgb(76, 94, 112));

            var panel = new Grid { Margin = new Thickness(16) };
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Content = panel;

            var text = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(foregroundColor)
            };
            Grid.SetRow(text, 0);
            panel.Children.Add(text);

            _input = new TextBox
            {
                Text = initialValue,
                Margin = new Thickness(0, 8, 0, 0),
                Background = new SolidColorBrush(inputColor),
                Foreground = new SolidColorBrush(foregroundColor),
                BorderBrush = new SolidColorBrush(borderColor)
            };
            Grid.SetRow(_input, 1);
            panel.Children.Add(_input);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var cancel = new Button
            {
                Content = "キャンセル", MinWidth = 76, IsCancel = true, Margin = new Thickness(0, 0, 8, 0),
                Background = _buttonBackground, Foreground = new SolidColorBrush(foregroundColor),
                BorderBrush = Brushes.Transparent
            };
            var ok = new Button
            {
                Content = "OK", MinWidth = 76,
                Background = new SolidColorBrush(ThemeService.IsLightMode ? Color.FromRgb(62, 112, 68) : Color.FromRgb(44, 92, 50)),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Transparent
            };
            cancel.MouseEnter += Button_MouseEnter;
            cancel.MouseLeave += Button_MouseLeave;
            ok.MouseEnter += Button_MouseEnter;
            ok.MouseLeave += Button_MouseLeave;
            ok.Click += (_, _) => AcceptIfValid();
            buttons.Children.Add(cancel);
            buttons.Children.Add(ok);
            Grid.SetRow(buttons, 2);
            panel.Children.Add(buttons);

            Loaded += (_, _) =>
            {
                _input.Focus();
                _input.SelectAll();
            };
            PreviewKeyDown += InputPromptWindow_PreviewKeyDown;
        }

        private void InputPromptWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                AcceptIfValid();
                e.Handled = true;
            }
        }

        private void AcceptIfValid()
        {
            if (_isValid == null || _isValid(_input.Text))
                DialogResult = true;
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button button)
                button.Background = _buttonHoverBackground;
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button button)
                button.Background = button.Content?.ToString() == "OK"
                    ? new SolidColorBrush(ThemeService.IsLightMode ? Color.FromRgb(62, 112, 68) : Color.FromRgb(44, 92, 50))
                    : _buttonBackground;
        }

        public static bool TryShow(Window owner, string title, string label, string initialValue, out string value,
            Func<string, bool> isValid = null)
        {
            var window = new InputPromptWindow(title, label, initialValue, isValid) { Owner = owner };
            bool accepted = window.ShowDialog() == true;
            value = window._input.Text;
            return accepted;
        }
    }
}
