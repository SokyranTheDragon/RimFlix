using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimFlix;

public class ShowDef : Def
{
    public float secondsBetweenFrames = 1;
    public SoundDef sound = null;
    public List<GraphicData> frames = new();
    public List<ThingDef> televisionDefs = new();

    public string SortName = "";
    public bool disabled = false;
    public bool deleted = false;

    public void RecheckDisabled()
    {
        var status = RimFlixMod.Settings.showStatus.FirstOrDefault(x => x.showDefName == defName);

        if (status != null)
        {
            disabled = status.status.All(x => !x.Value);
            FillDefaults(status);
            return;
        }

        status = new ShowStatus(defName);

        foreach (var def in GetDefaults())
            status.status[def.defName] = true;

        FillDefaults(status);
        RimFlixMod.Settings.showStatus.Add(status);
    }

    protected static void FillDefaults(in ShowStatus status)
    {
        foreach (var def in DefDatabase<ThingDef>.AllDefs)
        {
            if (def.GetCompProperties<CompProperties_Screen>() == null)
                continue;
            if (status.status.ContainsKey(def.defName))
                continue;
            status.status[def.defName] = false;
        }
    }

    protected virtual IEnumerable<ThingDef> GetDefaults() => televisionDefs ?? Enumerable.Empty<ThingDef>();
}