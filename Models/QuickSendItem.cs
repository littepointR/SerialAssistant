using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace SerialAssistant.Models;

public partial class QuickSendItem : ObservableObject
{
    public QuickSendItem(Action<QuickSendItem> sendAction, Action<QuickSendItem> removeAction)
    {
        SendAction = sendAction;
        RemoveAction = removeAction;
    }

    [ObservableProperty]
    private string _name = "Quick Send";

    [ObservableProperty]
    private string _data = "";

    [ObservableProperty]
    private bool _isHex = false;

    public Action<QuickSendItem> SendAction { get; }
    public Action<QuickSendItem> RemoveAction { get; }

    [RelayCommand]
    private void Send()
    {
        SendAction.Invoke(this);
    }

    [RelayCommand]
    private void Remove()
    {
        RemoveAction.Invoke(this);
    }
}