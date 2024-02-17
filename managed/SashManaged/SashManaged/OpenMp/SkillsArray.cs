﻿using System.Runtime.InteropServices;

namespace SashManaged.OpenMp;

[StructLayout(LayoutKind.Sequential)]
public readonly struct SkillsArray
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = OpenMpConstants.NUM_SKILL_LEVELS)]
    public readonly ushort[] Values;
}