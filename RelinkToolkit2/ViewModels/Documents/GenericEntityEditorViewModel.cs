
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using GBFRDataTools.Entities;
using GBFRDataTools.Entities.Player;
using GBFRDataTools.FSM;
using GBFRDataTools.FSM.Components;
using GBFRDataTools.FSM.Entities;
using GBFRDataTools.Hashing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MsBox.Avalonia;

using Nodify;
using Nodify.Compatibility;

using RelinkToolkit2.Messages.Dialogs;
using RelinkToolkit2.Messages.Fsm;
using RelinkToolkit2.Services;
using RelinkToolkit2.ViewModels.Documents.Interfaces;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.Documents.GraphEditor.TransitionComponents;
using RelinkToolkit2.ViewModels.Menu;
using RelinkToolkit2.ViewModels.TreeView;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace RelinkToolkit2.ViewModels.Documents;

public partial class GenericEntityEditorViewModel : EditorDocumentBase, ISaveableDocument
{
    private ILogger? _logger;

    /// <summary>
    /// Last name this file was saved as.
    /// </summary>
    public Uri? LastFile { get; set; }

    public bool ShouldPromptAsFirstSave { get; set; } = true;

    [ObservableProperty]
    private int _selectedIndex;

    [ObservableProperty]
    private object? _selectedObject;

    public PropertyGridViewModel PropertyGridViewModel { get; set; } = new();

    public ObservableCollection<GenericEntityEntryViewModel> Objects { get; set; } = [];

    public GenericEntityEditorViewModel()
    {
        _logger = App.Current?.Services?.GetService<ILogger<GenericEntityEditorViewModel>>();
    }

    partial void OnSelectedIndexChanged(int value)
    {
        if (value != -1)
            SelectedObject = Objects[value].Object;
    }

    public void SetObjects(IEnumerable<object> objects)
    {
        foreach (var obj in objects)
        {
            string title;
            if (obj is ActionInfo actionInfo)
                title = $"[{actionInfo.Id} - {actionInfo.AbilityTag}] ActionInfo ({actionInfo.ActionName})";
            else
                title = obj.GetType().Name;

            Objects.Add(new GenericEntityEntryViewModel()
            {
                Title = title,
                Object = obj,
            });
        }

        if (Objects.Count > 0)
        {
            SelectedIndex = 0;
            PropertyGridViewModel.SelectedObject = Objects[0].Object;
        }
    }

    public override void RegisterMessageListeners()
    {
        
    }

    public override void UnregisterMessageListeners()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    public async Task<Uri?> SaveDocument(IFilesService filesService, bool isSaveAs = false)
    {
        Uri? outputUri = LastFile;
        string? localPath = null;
        if (isSaveAs || outputUri is null || ShouldPromptAsFirstSave)
        {
            var file = await filesService.SaveFileAsync("Save file", null,
                                  $"{Title}.json", LastFile);
            if (file is null)
                return null;

            localPath = file.TryGetLocalPath();
            outputUri = file.Path;
        }

        if (string.IsNullOrEmpty(localPath))
            return null;

        var flattened = Objects.Select(e => e.Object);

        using var fs = File.Create(localPath);
        GenericEntitySerializer.Serialize(fs, flattened);
        ShouldPromptAsFirstSave = false;
        return outputUri;
    }
}
