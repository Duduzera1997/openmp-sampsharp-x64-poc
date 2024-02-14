﻿using System.Runtime.InteropServices;

namespace SashManaged.OpenMp;

[StructLayout(LayoutKind.Sequential)]
public readonly struct WeaponSlots
{
    private const int MAX_WEAPON_SLOTS = 13;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst=MAX_WEAPON_SLOTS)]
    public readonly WeaponSlotData[] Data;
}