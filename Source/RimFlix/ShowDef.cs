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
            status.FillDefaults();
            return;
        }

        status = new ShowStatus(defName);

        foreach (var def in GetDefaults())
            status.status[def.defName] = true;

        status.FillDefaults();
        RimFlixMod.Settings.showStatus.Add(status);
    }

    protected virtual IEnumerable<ThingDef> GetDefaults() => televisionDefs ?? Enumerable.Empty<ThingDef>();
}