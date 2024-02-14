﻿using System.Runtime.InteropServices;

namespace SashManaged.OpenMp;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Size(nint value)
{
    public const int Length = 8; // 64-bits

    public nint Value { get; } = value;

    public static implicit operator Size(int value)
    {
        return new Size(value);
    }
}