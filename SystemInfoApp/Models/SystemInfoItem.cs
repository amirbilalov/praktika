using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SystemInfoApp.Models;

public enum LoadingState
{
    Pending,
    Loading,
    Done,
    Error
}

public sealed class SystemInfoItem : INotifyPropertyChanged
{
    private string _value = string.Empty;
    private LoadingState _state = LoadingState.Pending;

    public string Name { get; init; } = string.Empty;

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            OnPropertyChanged();
        }
    }

    public LoadingState State
    {
        get => _state;
        set
        {
            if (_state == value) return;
            _state = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    public bool IsLoading => _state == LoadingState.Loading;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
