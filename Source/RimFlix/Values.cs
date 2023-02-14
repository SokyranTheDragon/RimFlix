using System;
using UnityEngine;
using Verse;

namespace RimFlix;

public class Values : IExposable
{
    public Vector2? scaleSouth = null;
    public Vector2? offsetSouth = null;
    public Vector2? scaleNorth = null;
    public Vector2? offsetNorth = null;
    public Vector2? scaleEast = null;
    public Vector2? offsetEast = null;
    public Vector2? scaleWest = null;
    public Vector2? offsetWest = null;
    public DrawType drawType = DrawType.Stretch;

    public void ExposeData()
    {
        SaveAccurateVector(ref scaleSouth, "scaleSouth");
        SaveAccurateVector(ref offsetSouth, "offsetSouth");
        SaveAccurateVector(ref scaleNorth, "scaleNorth");
        SaveAccurateVector(ref offsetNorth, "offsetNorth");
        SaveAccurateVector(ref scaleEast, "scaleEast");
        SaveAccurateVector(ref offsetEast, "offsetEast");
        SaveAccurateVector(ref scaleWest, "scaleWest");
        SaveAccurateVector(ref offsetWest, "offsetWest");
        Scribe_Values.Look(ref drawType, "drawType");
    }

    protected static void SaveAccurateVector(ref Vector2? vec, string label)
    {
        var x = vec?.x;
        var y = vec?.y;
        
        Scribe_Values.Look(ref x, $"{label}x");
        Scribe_Values.Look(ref y, $"{label}x");

        if (x == null || y == null)
            return;
        
        vec = new Vector2(x.Value, y.Value);
    }

    public ref Vector2? GetScale(Rot4 rotation)
    {
        if (rotation == Rot4.South)
            return ref scaleSouth;
        if (rotation == Rot4.North)
            return ref scaleNorth;
        if (rotation == Rot4.East)
            return ref scaleEast;
        if (rotation == Rot4.West)
            return ref scaleWest;
        throw new ArgumentOutOfRangeException(nameof(rotation), rotation, $"Provided rotation is not defined for the value. Use {nameof(IsRotationSupported)} first.");
    }

    public ref Vector2? GetOffset(Rot4 rotation)
    {
        if (rotation == Rot4.South)
            return ref offsetSouth;
        if (rotation == Rot4.North)
            return ref offsetNorth;
        if (rotation == Rot4.East)
            return ref offsetEast;
        if (rotation == Rot4.West)
            return ref offsetWest;
        throw new ArgumentOutOfRangeException(nameof(rotation), rotation, $"Provided rotation is not defined for the value. Use {nameof(IsRotationSupported)} first.");
    }

    public bool IsRotationSupported(Rot4 rotation)
    {
        if (rotation == Rot4.South)
            return offsetSouth != null && scaleSouth != null;
        if (rotation == Rot4.North)
            return offsetNorth != null && scaleNorth != null;
        if (rotation == Rot4.East)
            return offsetEast != null && scaleEast != null;
        if (rotation == Rot4.West)
            return offsetWest != null && scaleWest != null;
        return false;
    }

    public void RefreshValues(ThingDef def)
    {
        var values = def.GetCompProperties<CompProperties_Screen>()?.defaultValues;
        if (values == null) return;

        scaleSouth = values.scaleSouth;
        offsetSouth = values.offsetSouth;
        scaleNorth = values.scaleNorth;
        offsetNorth = values.offsetNorth;
        scaleEast = values.scaleEast;
        offsetEast = values.offsetEast;
        scaleWest = values.scaleWest;
        offsetWest = values.offsetWest;
    }

    public Values Copy()
        => new()
        {
            scaleSouth = scaleSouth,
            offsetSouth = offsetSouth,
            scaleNorth = scaleNorth,
            offsetNorth = offsetNorth,
            scaleEast = scaleEast,
            offsetEast = offsetEast,
            scaleWest = scaleWest,
            offsetWest = offsetWest,
            drawType = drawType,
        };
}