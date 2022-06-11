using System.Collections.Generic;
using Verse;

namespace RimFlix;

public class UserShowDef : ShowDef, IExposable
{
    public string path = null;
    public List<string> televisionDefStrings = new();

    public void ExposeData()
    {
        Scribe_Values.Look(ref defName, "defName");
        Scribe_Values.Look(ref path, "path");
        Scribe_Values.Look(ref label, "label");
        Scribe_Values.Look(ref description, "description");
        Scribe_Values.Look(ref secondsBetweenFrames, "secondsBetweenFrames");
        Scribe_Collections.Look(ref televisionDefStrings, "televisionDefStrings");
    }
}