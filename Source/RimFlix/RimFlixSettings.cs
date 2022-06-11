using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimFlix;

public class RimFlixSettings : ModSettings
{
    public string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
    public string lastPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
    public bool PlayAlways = true;
    public float PowerConsumptionOn = 100f;
    public float PowerConsumptionOff = 100f;
    public float SecondsBetweenShows = 60f;
    public DrawType DrawType = DrawType.Stretch;
    public List<UserShowDef> UserShows = new();
    public HashSet<string> DisabledShows = new();

    public static Vector2 TubeScaleDefault = new(0.5162f, 0.4200f);
    public static Vector2 FlatScaleDefault = new(0.8700f, 0.7179f);
    public static Vector2 MegaScaleDefault = new(0.9414f, 0.8017f);
    public static Vector2 UltraScaleDefault = new(0.896f, 0.621f);
    public static Vector2 TubeScale = TubeScaleDefault;
    public static Vector2 FlatScale = FlatScaleDefault;
    public static Vector2 MegaScale = MegaScaleDefault;
    public static Vector2 UltraScale = UltraScaleDefault;

    public static Vector2 TubeOffsetDefault = new(-0.0897f, 0.1172f);
    public static Vector2 FlatOffsetDefault = new(0.0f, -0.0346f);
    public static Vector2 MegaOffsetDefault = new(0.0f, -0.0207f);
    public static Vector2 UltraOffsetDefault = new(0.0f, -0.0425f);
    public static Vector2 TubeOffset = TubeOffsetDefault;
    public static Vector2 FlatOffset = FlatOffsetDefault;
    public static Vector2 MegaOffset = MegaOffsetDefault;
    public static Vector2 UltraOffset = MegaOffsetDefault;

    public float TubeScaleX = TubeScaleDefault.x;
    public float TubeScaleY = TubeScaleDefault.y;
    public float FlatScaleX = FlatScaleDefault.x;
    public float FlatScaleY = FlatScaleDefault.y;
    public float MegaScaleX = MegaScaleDefault.x;
    public float MegaScaleY = MegaScaleDefault.y;
    public float UltraScaleX = MegaScaleDefault.x;
    public float UltraScaleY = MegaScaleDefault.y;

    public float TubeOffsetX = TubeOffsetDefault.x;
    public float TubeOffsetY = TubeOffsetDefault.y;
    public float FlatOffsetX = FlatOffsetDefault.x;
    public float FlatOffsetY = FlatOffsetDefault.y;
    public float MegaOffsetX = MegaOffsetDefault.x;
    public float MegaOffsetY = MegaOffsetDefault.y;
    public float UltraOffsetX = UltraOffsetDefault.x;
    public float UltraOffsetY = UltraOffsetDefault.y;

    public static double screenUpdateTime = 0;
    public static double showUpdateTime = 0;

    public static DateTime RimFlixEpoch = new(2019, 03, 10, 0, 0, 0, DateTimeKind.Utc);

    public static double TotalSeconds => (DateTime.UtcNow - RimFlixEpoch).TotalSeconds;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref PlayAlways, "playAlways", true);
        Scribe_Values.Look(ref PowerConsumptionOn, "powerConsumptionOn", 100f);
        Scribe_Values.Look(ref PowerConsumptionOff, "powerConsumptionOff", 100f);
        Scribe_Values.Look(ref SecondsBetweenShows, "secondsBetweenShows", 60f);
        Scribe_Values.Look(ref DrawType, "drawType");
        Scribe_Values.Look(ref lastPath, "lastPath");
        Scribe_Collections.Look(ref DisabledShows, "disabledShows");
        Scribe_Collections.Look(ref UserShows, "userShows", LookMode.Deep);
        UserShows ??= new List<UserShowDef>();
        DisabledShows ??= new HashSet<string>();

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            Scribe_Values.Look(ref TubeScaleX, "tubeScaleX", TubeScaleDefault.x);
            Scribe_Values.Look(ref TubeScaleY, "tubeScaleY", TubeScaleDefault.y);
            Scribe_Values.Look(ref FlatScaleX, "flatScaleX", FlatScaleDefault.x);
            Scribe_Values.Look(ref FlatScaleY, "flatScaleY", FlatScaleDefault.y);
            Scribe_Values.Look(ref MegaScaleX, "megaScaleX", MegaScaleDefault.x);
            Scribe_Values.Look(ref MegaScaleY, "megaScaleY", MegaScaleDefault.y);
            Scribe_Values.Look(ref UltraScaleX, "ultraScaleX", UltraScaleDefault.x);
            Scribe_Values.Look(ref UltraScaleY, "ultraScaleY", UltraScaleDefault.y);
            TubeScale = new Vector2(TubeScaleX, TubeScaleY);
            FlatScale = new Vector2(FlatScaleX, FlatScaleY);
            MegaScale = new Vector2(MegaScaleX, MegaScaleY);
            UltraScale = new Vector2(UltraScaleX, UltraScaleY);

            Scribe_Values.Look(ref TubeOffsetX, "tubeOffsetX", TubeOffsetDefault.x);
            Scribe_Values.Look(ref TubeOffsetY, "tubeOffsetY", TubeOffsetDefault.y);
            Scribe_Values.Look(ref FlatOffsetX, "flatOffsetX", FlatOffsetDefault.x);
            Scribe_Values.Look(ref FlatOffsetY, "flatOffsetY", FlatOffsetDefault.y);
            Scribe_Values.Look(ref MegaOffsetX, "megaOffsetX", MegaOffsetDefault.x);
            Scribe_Values.Look(ref MegaOffsetY, "megaOffsetY", MegaOffsetDefault.y);
            Scribe_Values.Look(ref UltraOffsetX, "ultraOffsetX", UltraOffsetDefault.x);
            Scribe_Values.Look(ref UltraOffsetY, "ultraOffsetY", UltraOffsetDefault.y);
            TubeOffset = new Vector2(TubeOffsetX, TubeOffsetY);
            FlatOffset = new Vector2(FlatOffsetX, FlatOffsetY);
            MegaOffset = new Vector2(MegaOffsetX, MegaOffsetY);
            UltraOffset = new Vector2(UltraOffsetX, UltraOffsetY);
        }
        else
        {
            TubeScaleX = TubeScale.x;
            TubeScaleY = TubeScale.y;
            FlatScaleX = FlatScale.x;
            FlatScaleY = FlatScale.y;
            MegaScaleX = MegaScale.x;
            MegaScaleY = MegaScale.y;
            UltraScaleX = UltraScale.x;
            UltraScaleY = UltraScale.y;
            Scribe_Values.Look(ref TubeScaleX, "tubeScaleX", TubeScaleDefault.x);
            Scribe_Values.Look(ref TubeScaleY, "tubeScaleY", TubeScaleDefault.y);
            Scribe_Values.Look(ref FlatScaleX, "flatScaleX", FlatScaleDefault.x);
            Scribe_Values.Look(ref FlatScaleY, "flatScaleY", FlatScaleDefault.y);
            Scribe_Values.Look(ref MegaScaleX, "megaScaleX", MegaScaleDefault.x);
            Scribe_Values.Look(ref MegaScaleY, "megaScaleY", MegaScaleDefault.y);
            Scribe_Values.Look(ref UltraScaleX, "ultraScaleX", UltraScaleDefault.x);
            Scribe_Values.Look(ref UltraScaleY, "ultraScaleY", UltraScaleDefault.y);
            TubeOffsetX = TubeOffset.x;
            TubeOffsetY = TubeOffset.y;
            FlatOffsetX = FlatOffset.x;
            FlatOffsetY = FlatOffset.y;
            MegaOffsetX = MegaOffset.x;
            MegaOffsetY = MegaOffset.y;
            UltraOffsetX = UltraOffset.x;
            UltraOffsetY = UltraOffset.y;
            Scribe_Values.Look(ref TubeOffsetX, "tubeOffsetX", TubeOffsetDefault.x);
            Scribe_Values.Look(ref TubeOffsetY, "tubeOffsetY", TubeOffsetDefault.y);
            Scribe_Values.Look(ref FlatOffsetX, "flatOffsetX", FlatOffsetDefault.x);
            Scribe_Values.Look(ref FlatOffsetY, "flatOffsetY", FlatOffsetDefault.y);
            Scribe_Values.Look(ref MegaOffsetX, "megaOffsetX", MegaOffsetDefault.x);
            Scribe_Values.Look(ref MegaOffsetY, "megaOffsetY", MegaOffsetDefault.y);
            Scribe_Values.Look(ref UltraOffsetX, "ultraOffsetX", UltraOffsetDefault.x);
            Scribe_Values.Look(ref UltraOffsetY, "ultraOffsetY", UltraOffsetDefault.y);
        }

        base.ExposeData();
    }
}