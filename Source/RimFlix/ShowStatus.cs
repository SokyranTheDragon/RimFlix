using System.Collections.Generic;
using Verse;

namespace RimFlix;

public class ShowStatus : IExposable
{
    public string showDefName;
    public Dictionary<string, bool> status;

    public ShowStatus() 
        => status = new Dictionary<string, bool>();

    public ShowStatus(string showDefName) : this() 
        => this.showDefName = showDefName;

    public void ExposeData()
    {
        Scribe_Values.Look(ref showDefName, "showDefName");
        Scribe_Collections.Look(ref status, "status", LookMode.Value, LookMode.Value);
    }

    public void FillDefaults()
    {
        foreach (var def in DefDatabase<ThingDef>.AllDefs)
        {
            if (def.GetCompProperties<CompProperties_Screen>() == null)
                continue;
            if (status.ContainsKey(def.defName))
                continue;
            status[def.defName] = false;
        }
    }
}