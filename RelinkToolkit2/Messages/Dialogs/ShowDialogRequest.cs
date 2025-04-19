using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;

using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;

namespace RelinkToolkit2.Messages.Dialogs;

public class ShowDialogRequest : RequestMessage<ButtonResult>
{
    public IMsBox<ButtonResult> Box { get; set; }

    public ShowDialogRequest(IMsBox<ButtonResult> box)
    {
        Box = box;
    }
}
