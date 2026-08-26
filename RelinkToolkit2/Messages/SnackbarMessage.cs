using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Messages;

public class SnackbarMessage
{
    public string Text { get; set; }
    public TimeSpan Duration { get; }

    public SnackbarMessage(string text, TimeSpan? duration = default)
    {
        Text = text;
        Duration = duration ?? TimeSpan.FromSeconds(3);
    }
}
