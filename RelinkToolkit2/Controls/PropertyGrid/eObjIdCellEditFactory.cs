using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.PropertyGrid;
using Avalonia.PropertyGrid.Controls;
using Avalonia.PropertyGrid.Controls.Factories;
using Avalonia.PropertyGrid.Services;

using GBFRDataTools.Entities;

using PropertyModels.Extensions;

namespace RelinkToolkit2.Controls.PropertyGrid;

public class eObjIdCellEditFactory : AbstractCellEditFactory
{
    public override Control? HandleNewProperty(PropertyCellContext context)
    {
        var propertyDescriptor = context.Property;

        if (propertyDescriptor.PropertyType != typeof(int))
            return null;

        if (!propertyDescriptor.IsDefined<eObjIdAttribute>())
            return null;

        var selector = new ObjIdSelector();
        selector.ObjId = (int)context.GetValue()!;
        selector.ObjIdChanged += (s, e) => 
            SetAndRaise(context, selector, selector.ObjId);

        return selector;
    }

    public override bool HandlePropertyChanged(PropertyCellContext context)
    {
        var propertyDescriptor = context.Property;
        var target = context.Target;
        var control = context.CellEdit!;

        if (propertyDescriptor.PropertyType != typeof(int))
        {
            return false;
        }

        ValidateProperty(control, propertyDescriptor, target);

        return true;
    }
}
