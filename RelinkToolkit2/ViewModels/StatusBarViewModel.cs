using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using RelinkToolkit2.Messages.StatusBar;

namespace RelinkToolkit2.ViewModels;

public partial class StatusBarViewModel : ObservableObject
{
    [ObservableProperty]
    public string? _message = "Welcome to RelinkToolkit2";

    public StatusBarViewModel()
    {
        WeakReferenceMessenger.Default.Register<SetStatusBarTextRequest>(this, (recipient, message) =>
        {
            Message = message.Text;
            message.Reply(true);
        });
    }
}
