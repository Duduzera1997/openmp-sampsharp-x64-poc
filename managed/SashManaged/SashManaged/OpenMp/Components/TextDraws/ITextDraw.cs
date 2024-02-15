﻿namespace SashManaged.OpenMp;

[OpenMpApi(typeof(ITextDrawBase))]
public readonly partial struct ITextDraw
{
    public partial void ShowForPlayer(IPlayer player);
    public partial void HideForPlayer(IPlayer player);
    public partial bool IsShownForPlayer(IPlayer player);
    public partial void SetTextForPlayer(IPlayer player, StringView text);
}