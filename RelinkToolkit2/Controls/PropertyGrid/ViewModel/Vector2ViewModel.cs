﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using PropertyModels.ComponentModel;

namespace RelinkToolkit2.Controls.PropertyGrid.ViewModel;

public class Vector2ViewModel : MiniReactiveObject
{
    public Vector2 Vector;

    public float X
    {
        get => Vector.X;
        set => this.RaiseAndSetIfChanged(ref Vector.X, value);
    }

    public float Y
    {
        get => Vector.Y;
        set => this.RaiseAndSetIfChanged(ref Vector.Y, value);
    }

    public Vector2ViewModel(Vector2 vector)
    {
        Vector = vector;
    }
}
