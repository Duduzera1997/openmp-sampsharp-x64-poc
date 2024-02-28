﻿using System.Numerics;

namespace SashManaged.OpenMp;

[OpenMpApi2]
public readonly partial struct IEntity
{
    public partial Vector3 GetPosition();

    public partial void SetPosition(Vector3 position);

    public partial GTAQuat GetRotation();

    public partial void SetRotation(GTAQuat rotation);

    public partial int GetVirtualWorld();

    public partial void SetVirtualWorld(int vw);
}