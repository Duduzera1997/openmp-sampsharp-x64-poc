﻿namespace SashManaged.OpenMp;

[OpenMpApi2(typeof(ITextLabelBase))]
public readonly partial struct ITextLabel
{
    public partial bool IsStreamedInForPlayer(IPlayer player);
    public partial void StreamInForPlayer(IPlayer player);
    public partial void StreamOutForPlayer(IPlayer player);
}