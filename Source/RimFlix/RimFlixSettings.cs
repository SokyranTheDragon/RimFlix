using System;
using System.Collections.Generic;
using Verse;

namespace RimFlix;

public class RimFlixSettings : ModSettings
{
    public string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
    public string lastPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
    public bool playAlways = true;
    public float powerConsumptionOn = 100f;
    public float powerConsumptionOff = 100f;
    public float secondsBetweenShows = 60f;
    public DrawType drawType = DrawType.Stretch;
    public List<UserShowDef> userShows = new();
    public HashSet<string> disabledShows = new();
    
    public Dictionary<Def, Values> defValues = new();
    private List<Def> defWorkingList = null;
    private List<Values> defValuesWorkingList = null;

    public static double screenUpdateTime = 0;
    public static double showUpdateTime = 0;

    public static DateTime rimFlixEpoch = new(2019, 03, 10, 0, 0, 0, DateTimeKind.Utc);

    public static double TotalSeconds => (DateTime.UtcNow - rimFlixEpoch).TotalSeconds;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref playAlways, "playAlways", true);
        Scribe_Values.Look(ref powerConsumptionOn, "powerConsumptionOn", 100f);
        Scribe_Values.Look(ref powerConsumptionOff, "powerConsumptionOff", 100f);
        Scribe_Values.Look(ref secondsBetweenShows, "secondsBetweenShows", 60f);
        Scribe_Values.Look(ref drawType, "drawType");
        Scribe_Values.Look(ref lastPath, "lastPath");
        Scribe_Collections.Look(ref disabledShows, "disabledShows");
        Scribe_Collections.Look(ref userShows, "userShows", LookMode.Deep);
        userShows ??= new List<UserShowDef>();
        disabledShows ??= new HashSet<string>();

        defValues ??= new Dictionary<Def, Values>();
        Scribe_Collections.Look(ref defValues, "defValues", LookMode.Def, LookMode.Deep, ref defWorkingList, ref defValuesWorkingList);

        base.ExposeData();
    }
}