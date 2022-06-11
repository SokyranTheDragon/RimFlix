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
        Scribe_Values.Look(ref scaleSouth, "scaleSouth");
        Scribe_Values.Look(ref offsetSouth, "offsetSouth");
        Scribe_Values.Look(ref scaleNorth, "scaleNorth");
        Scribe_Values.Look(ref offsetNorth, "offsetNorth");
        Scribe_Values.Look(ref scaleEast, "scaleEast");
        Scribe_Values.Look(ref offsetEast, "offsetEast");
        Scribe_Values.Look(ref scaleWest, "scaleWest");
        Scribe_Values.Look(ref offsetWest, "offsetWest");
        Scribe_Values.Look(ref drawType, "drawType");
    }

    public Vector2? Scale(Rot4 rotation)
    {
        if (rotation == Rot4.South)
            return scaleSouth;
        if (rotation == Rot4.North)
            return scaleNorth;
        if (rotation == Rot4.East)
            return scaleEast;
        if (rotation == Rot4.West)
            return scaleWest;
        return null;
    }

    public Vector2? Offset(Rot4 rotation)
    {
        if (rotation == Rot4.South)
            return offsetSouth;
        if (rotation == Rot4.North)
            return offsetNorth;
        if (rotation == Rot4.East)
            return offsetEast;
        if (rotation == Rot4.West)
            return offsetWest;
        return null;
    }

    public bool IsRotationSupported(Rot4 rotation)
    {
        if (rotation == Rot4.South)
            return this is { offsetSouth: { }, scaleSouth: { } };
        if (rotation == Rot4.North)
            return this is { offsetNorth: { }, scaleNorth: { } };
        if (rotation == Rot4.East)
            return this is { offsetEast: { }, scaleEast: { } };
        if (rotation == Rot4.West)
            return this is { offsetWest: { }, scaleWest: { } };
        return false;
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