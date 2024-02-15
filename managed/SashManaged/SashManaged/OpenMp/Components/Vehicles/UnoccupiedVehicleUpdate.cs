﻿using System.Numerics;
using System.Runtime.InteropServices;

namespace SashManaged.OpenMp;

[StructLayout(LayoutKind.Sequential)]
public readonly struct UnoccupiedVehicleUpdate
{
    public readonly byte seat;
    public readonly Vector3 position;
    public readonly Vector3 velocity;
}