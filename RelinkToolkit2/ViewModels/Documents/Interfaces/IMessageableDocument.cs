using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dock.Model.Mvvm.Controls;

namespace RelinkToolkit2.ViewModels.Documents.Interfaces;

/// <summary>
/// Interface for a document that can be messaged.
/// </summary>
public interface IMessageableDocument
{
    public void RegisterMessageListeners();
    public void UnregisterMessageListeners();
}
