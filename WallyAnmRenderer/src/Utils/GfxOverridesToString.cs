using System;
using BrawlhallaAnimLib.Gfx;

namespace WallyAnmRenderer;

internal static class GfxOverridesToString
{
    public static string MouthOverrideToString(GfxMouthOverride mouthOverride) => mouthOverride switch
    {
        GfxMouthOverride.NoChange => "None",
        GfxMouthOverride.Warcry => "Warcry",
        GfxMouthOverride.Smile => "Smile",
        GfxMouthOverride.KO => "KO",
        GfxMouthOverride.Hit => "Hit",
        GfxMouthOverride.Growl => "Growl",
        GfxMouthOverride.Whistle => "Whistle",
        GfxMouthOverride.Neutral => "Neutral",
        _ => throw new IndexOutOfRangeException(),
    };

    public static string EyesOverrideToString(GfxEyesOverride eyesOverride) => eyesOverride switch
    {
        GfxEyesOverride.NoChange => "None",
        GfxEyesOverride.LookSide => "Look side",
        GfxEyesOverride.KO => "KO",
        GfxEyesOverride.Hit => "Hit",
        GfxEyesOverride.LookDown => "Look down",
        GfxEyesOverride.Angry => "Angry",
        GfxEyesOverride.Neutral => "Neutral",
        _ => throw new IndexOutOfRangeException(),
    };

    public static string AccentOverrideToString(GfxAccentOverride accentOverride) => accentOverride switch
    {
        GfxAccentOverride.NoChange => "None",
        GfxAccentOverride.LookSide => "Look side",
        GfxAccentOverride.KO => "KO",
        GfxAccentOverride.Hit => "Hit",
        GfxAccentOverride.LookDown => "Look down",
        GfxAccentOverride.Angry => "Angry",
        GfxAccentOverride.Neutral => "Neutral",
        _ => throw new IndexOutOfRangeException(),
    };
}