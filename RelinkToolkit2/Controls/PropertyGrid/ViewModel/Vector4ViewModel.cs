using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using PropertyModels.ComponentModel;

namespace RelinkToolkit2.Controls.PropertyGrid.ViewModel;

public class Vector4ViewModel : MiniReactiveObject
{
    public Vector4 Vector;

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

    public float Z
    {
        get => Vector.Z;
        set => this.RaiseAndSetIfChanged(ref Vector.Z, value);
    }

    public float W
    {
        get => Vector.W;
        set => this.RaiseAndSetIfChanged(ref Vector.W, value);
    }

    public Vector4ViewModel(Vector4 vector)
    {
        Vector = vector;
    }
}
