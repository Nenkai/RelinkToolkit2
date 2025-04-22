using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.PropertyGrid;
using Avalonia.PropertyGrid.Controls;
using Avalonia.PropertyGrid.Controls.Factories;
using Avalonia.PropertyGrid.Services;

using GBFRDataTools.Entities;

using PropertyModels.Extensions;

using RelinkToolkit2.Controls.PropertyGrid.ViewModel;
using RelinkToolkit2.Controls.PropertyGrid.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Controls.PropertyGrid;

public class Vector3CellEditFactory : AbstractCellEditFactory
{
    public override Control? HandleNewProperty(PropertyCellContext context)
    {
        var propertyDescriptor = context.Property;

        if (propertyDescriptor.PropertyType != typeof(Vector3))
            return null;

        var control = new Vector3View();
        return control;
    }

    public override bool HandlePropertyChanged(PropertyCellContext context)
    {
        var propertyDescriptor = context.Property;
        var target = context.Target;
        var control = context.CellEdit!;

        if (propertyDescriptor.PropertyType != typeof(Vector3))
        {
            return false;
        }

        ValidateProperty(control, propertyDescriptor, target);

        if (control is Vector3View vv)
        {
            var vec = (Vector3)propertyDescriptor.GetValue(target)!;

            var model = new Vector3ViewModel(vec);
            vv.DataContext = model;

            model.PropertyChanged += (s, e) => SetAndRaise(context, control, model.Vector);

            return true;
        }

        return true;
    }
}
