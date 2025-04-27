using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Dock.Model.Controls;
using Dock.Model.Core;

using GBFRDataTools.Entities.Quest;
using GBFRDataTools.FSM;
using GBFRDataTools.FSM.Components.Actions.Quest;
using GBFRDataTools.FSM.Entities;
using GBFRDataTools.Hashing;

using MsBox.Avalonia;

using RelinkToolkit2.Messages.Dialogs;
using RelinkToolkit2.Messages.Documents;
using RelinkToolkit2.Messages.IO;
using RelinkToolkit2.ViewModels.Documents;
using RelinkToolkit2.ViewModels.Documents.GraphEditor;
using RelinkToolkit2.ViewModels.TreeView;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace RelinkToolkit2.ViewModels;

public partial class StringHasherViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _inputString;

    [ObservableProperty]
    private string? _expectedString;

    [ObservableProperty]
    private bool _toLower;

    [ObservableProperty]
    private string? _XXHash64Hex;

    [ObservableProperty]
    private IBrush? _XXHash64HexBackground;

    [ObservableProperty]
    private string? _XXHash64Dec;

    [ObservableProperty]
    private IBrush? _XXHash64DecBackground;

    [ObservableProperty]
    private string? _XXHash32Hex;

    [ObservableProperty]
    private IBrush? _XXHash32HexBackground;

    [ObservableProperty]
    private string? _XXHash32Dec;

    [ObservableProperty]
    private IBrush? _XXHash32DecBackground;

    [ObservableProperty]
    private string? _CRC32Hex;

    [ObservableProperty]
    private IBrush? _CRC32HexBackground;

    [ObservableProperty]
    private string? _CRC32Dec;

    [ObservableProperty]
    private IBrush? _CRC32DecBackground;


    public StringHasherViewModel()
    {
        
    }

    partial void OnToLowerChanged(bool value)
    {
        Update();
    }

    partial void OnInputStringChanged(string? value)
    {
        Update();
    }

    partial void OnExpectedStringChanged(string? value)
    {
        Update();
    }

    private void Update()
    {
        string str = InputString ?? string.Empty;
        str = ToLower ? str.ToLower() : str;

        ulong xxhash64 = XXHash64.Hash(Encoding.UTF8.GetBytes(str), 0);
        XXHash64Hex = xxhash64.ToString("X16");
        XXHash64Dec = xxhash64.ToString();

        uint xxHash32 = XXHash32Custom.Hash(str);
        XXHash32Hex = xxHash32.ToString("X8");
        XXHash32Dec = xxHash32.ToString();

        uint crc32 = CRC32.crc32_0x77073096(str, toLower: ToLower);
        CRC32Hex = crc32.ToString("X8");
        CRC32Dec = crc32.ToString();

        XXHash64HexBackground = XXHash64Hex == ExpectedString ? Brushes.Green : null;
        XXHash64DecBackground = XXHash64Dec == ExpectedString ? Brushes.Green : null;
        XXHash32HexBackground = XXHash32Hex == ExpectedString ? Brushes.Green : null;
        XXHash32DecBackground = XXHash32Dec == ExpectedString ? Brushes.Green : null;
        CRC32HexBackground = CRC32Hex == ExpectedString ? Brushes.Green : null;
        CRC32DecBackground = CRC32Dec == ExpectedString ? Brushes.Green : null;
    }
}
