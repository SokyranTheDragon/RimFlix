using UnityEngine;
using Verse;

namespace RimFlix;

public class RimFlixMod : Mod
{
    public static RimFlixSettings Settings { get; private set; }
    public static ModContentPack ModContent { get; private set; }

    public RimFlixMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<RimFlixSettings>();
        ModContent = content;
    }

    public override void DoSettingsWindowContents(Rect rect) => Settings.DoSettingsWindowContents(rect);

    public override string SettingsCategory() => "RimFlix_Title".Translate();
}

public enum SortType
{
    None,
    Name,
    Frames,
    Time,
    Action
}

public enum DrawType
{
    Stretch,
    Fit,
    Fill
}