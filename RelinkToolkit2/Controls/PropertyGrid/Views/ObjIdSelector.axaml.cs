using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using GBFRDataTools.Entities;

using PropertyModels.Extensions;

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RelinkToolkit2.Controls.PropertyGrid.Views;

public partial class ObjIdSelector : UserControl
{
    private static readonly string[] _enumNames;
    private static readonly eObjIdType[] _enumIds;

    static ObjIdSelector()
    {
        _enumNames = Enum.GetNames<eObjIdType>();
        _enumIds = Enum.GetValues<eObjIdType>();
    }

    /// <summary>
    /// The ObjId property
    /// </summary>
    public static readonly DirectProperty<ObjIdSelector, int> ObjIdProperty =
        AvaloniaProperty.RegisterDirect<ObjIdSelector, int>(
            nameof(ObjId),
            o => o.ObjId,
            (o, v) => o.ObjId = v,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay
            );


    private int _objId;

    /// <summary>
    /// Gets or sets Is ReadOnly Flags.
    /// </summary>
    public int ObjId
    {
        get => _objId;
        set
        {
            SetAndRaise(ObjIdProperty, ref _objId, value);
            OnObjIdChanged();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property.Name == nameof(ObjId))
        {
            // Ensure not to cause re-triggers
            if (!_isApplyingFromControlChange)
                UpdateControls();
        }
    }

    public event EventHandler<RoutedEventArgs> ObjIdChanged
    {
        add => AddHandler(ObjIdChangedEvent, value);
        remove => RemoveHandler(ObjIdChangedEvent, value);
    }

    protected virtual void OnObjIdChanged()
    {
        RoutedEventArgs args = new RoutedEventArgs(ObjIdChangedEvent);
        RaiseEvent(args);
    }

    public static readonly RoutedEvent<RoutedEventArgs> ObjIdChangedEvent =
        RoutedEvent.Register<ObjIdSelector, RoutedEventArgs>(nameof(ObjIdChanged), RoutingStrategies.Bubble);

    private bool _isApplyingFromControlChange = false;
    private void UpdateControls()
    {
        CheckBox_IsSet.IsChecked = ObjId != -1;

        int selId = Array.IndexOf(_enumIds, (eObjIdType)(ObjId & 0xFFFF0000));
        Combo_ObjIdType.SelectedIndex = selId != -1 ? selId : 0;
        NumUpDown_ObjId.Value = ObjId != -1 ? ObjId & 0xFFFF : 0;
    }

    public ObjIdSelector()
    {
        InitializeComponent();

        Combo_ObjIdType.ItemsSource = _enumNames;
        Combo_ObjIdType.SelectedIndex = 0;
    }

    private void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Combo_ObjIdType.SelectedIndex == -1)
            return;

        _isApplyingFromControlChange = true;

        eObjIdType type = _enumIds[Combo_ObjIdType.SelectedIndex];
        ObjId = (int)type | (ObjId & 0xFFFF);

        _isApplyingFromControlChange = false;
    }

    private void CheckBox_Checked(object? sender, RoutedEventArgs e)
    {
        _isApplyingFromControlChange = true;

        if (CheckBox_IsSet.IsChecked == false)
        {
            ObjId = -1;
            Combo_ObjIdType.SelectedIndex = 0;
            NumUpDown_ObjId.Value = 0;
        }

        _isApplyingFromControlChange = false;
    }

    private void NumUpDown_ObjId_ValueChanged(object? sender, Avalonia.Controls.NumericUpDownValueChangedEventArgs e)
    {
        if (NumUpDown_ObjId.Value is null)
            return;

        _isApplyingFromControlChange = true;

        ObjId = (int)((ObjId & 0xFFFF0000) | (uint)((int)NumUpDown_ObjId.Value & 0xFFFF));

        _isApplyingFromControlChange = false;
    }
}