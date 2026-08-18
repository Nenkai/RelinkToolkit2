using System;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Threading;
using RelinkToolkit2.ViewModels.Logging;
using System.Collections.ObjectModel;

namespace RelinkToolkit2.Logging;

public static class LogCatcher
{
    public readonly static ObservableCollection<LogItemViewModel> LogItems = new ObservableCollection<LogItemViewModel>();
    private static long _counter;

    /*
    public static IObservable<LogItemViewModel> LogEntryObservable
    {
        get { return LogItems.AsObservable(); }
    }
    */

    public static void Append(LogItemViewModel loggingEvent)
    {
        Interlocked.Increment(ref _counter);
        LogItems.Add(loggingEvent);
    }
}
