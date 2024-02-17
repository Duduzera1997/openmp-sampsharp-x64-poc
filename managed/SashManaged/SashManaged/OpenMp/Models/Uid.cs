﻿using System.Runtime.InteropServices;

namespace SashManaged.OpenMp;

[StructLayout(LayoutKind.Sequential)]
public readonly struct UID(ulong value)
{
    public readonly ulong Value = value;

    public override string ToString()
    {
        return Value.ToString("x16");
    }
}