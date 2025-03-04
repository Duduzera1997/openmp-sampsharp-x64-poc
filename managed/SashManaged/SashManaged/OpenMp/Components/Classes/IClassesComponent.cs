﻿using System.Numerics;

namespace SashManaged.OpenMp;

[OpenMpApi2(typeof(IComponent))]
public readonly partial struct IClassesComponent
{
    public static UID ComponentId => new(0x8cfb3183976da208);

    public partial IEventDispatcher2<IClassEventHandler> GetEventDispatcher();

    public partial IClass Create(int skin, int team, Vector3 spawn, float angle, ref WeaponSlots weapons);
}