using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PotatoMusicPlayer.ViewModels;

/// <summary>
/// すべての ViewModel の基底クラス
/// INotifyPropertyChanged を実装してデータバインディングをサポート
/// </summary>
public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(backingField, value))
            return false;

        backingField = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
