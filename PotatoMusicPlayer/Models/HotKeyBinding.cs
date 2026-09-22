using System;
using System.Windows.Input;

namespace PotatoMusicPlayer.Models
{
    /// <summary>
    /// ホットキーの機能ID
    /// </summary>
    public enum HotKeyAction
    {
        PlayPause = 1,
        Stop = 2,
        SkipBackward5s = 3,
        SkipForward5s = 4,
        VolumeUp = 5,
        VolumeDown = 6,
        Mute = 7,
        StepBackward01s = 8,
        StepForward01s = 9,
        GoToStart = 10,
        GoToEnd = 11,
        SpeedDecrease = 12,
        SpeedIncrease = 13,
        SpeedReset = 14,
        ToggleLoopMode = 15,
        ToggleWaveform = 16
    }

    /// <summary>
    /// ホットキーバインディング
    /// </summary>
    [Serializable]
    public class HotKeyBinding
    {
        public HotKeyAction Action { get; set; }
        public Key Key { get; set; }
        public ModifierKeys Modifiers { get; set; } = ModifierKeys.None;
        public string DisplayName { get; set; }  // UI表示用の日本語名

        public override string ToString()
        {
            var keyStr = Key.ToString();
            var modStr = Modifiers.ToString();
            
            if (Modifiers == ModifierKeys.None)
                return keyStr;
            
            return $"{modStr} + {keyStr}";
        }

        public override bool Equals(object obj)
        {
            if (obj is HotKeyBinding other)
            {
                return this.Action == other.Action && 
                       this.Key == other.Key && 
                       this.Modifiers == other.Modifiers;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Action, Key, Modifiers);
        }
    }
}
