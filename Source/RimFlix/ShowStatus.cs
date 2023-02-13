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
}