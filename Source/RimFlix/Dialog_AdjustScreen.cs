using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimFlix;

internal class Dialog_AdjustScreen : Window
{
    private readonly float inRectWidth;
    private readonly float inRectHeight;
    private const float HeaderHeight = 40;
    private readonly float texDim;

    private readonly ThingDef def;
    private readonly Values config;
    private readonly Dictionary<Rot4, Texture> textures = new();
    private readonly Vector2 drawSize;
    private readonly int count;

    public Dialog_AdjustScreen(ThingDef tvDef)
    {
        doCloseX = true;
        doCloseButton = true;
        forcePause = true;
        absorbInputAroundWindow = true;

        def = tvDef;

        if (!RimFlixMod.Settings.defValues.TryGetValue(def.defName, out config))
            config = RimFlixMod.Settings.defValues[def.defName] = def.GetCompProperties<CompProperties_Screen>()?.defaultValues ?? new Values();
        else config.RefreshValues(def);
        
        drawSize = def.graphicData.drawSize;

        if (config.IsRotationSupported(Rot4.South))
            textures[Rot4.South] = def.graphic.MatSouth.mainTexture;
        if (config.IsRotationSupported(Rot4.North))
            textures[Rot4.North] = def.graphic.MatNorth.mainTexture;
        if (config.IsRotationSupported(Rot4.East))
            textures[Rot4.East] = def.graphic.MatEast.mainTexture;
        if (config.IsRotationSupported(Rot4.West))
            textures[Rot4.West] = def.graphic.MatWest.mainTexture;

        const float tempDim = 810F / 4f;
        texDim = tempDim / drawSize.x;
        count = textures.Count;
        if (count <= 0)
            count = 1;

        inRectWidth = (tempDim + 36f) * count + 36f;
        inRectHeight = 700f + drawSize.y;
    }

    public override Vector2 InitialSize => new(inRectWidth, inRectHeight);

    public override void Close(bool doCloseSound = true)
    {
        RimFlixSettings.screenUpdateTime = RimFlixSettings.TotalSeconds;
        base.Close(doCloseSound);
    }

    public override void DoWindowContents(Rect inRect)
    {
        // Avoid Close button overlap
        inRect.yMax -= 50;
        
        // Header title
        Text.Font = GameFont.Medium;
        var headerRect = inRect.TopPartPixels(HeaderHeight);
        Widgets.Label(inRect, "RimFlix_AdjustSreenTitle".Translate());
        
        // Use inRect for main Listing so screen overlay does not get cut off by header when
        // moved up
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperCenter;
        var list = new Listing_Standard { ColumnWidth = (inRect.width - 50) / count };
        inRect.yMin += headerRect.height + 25;
        list.Begin(inRect);

        var gap = list.Label(def.LabelCap).height;

        var current = textures.Count - 1;
        foreach (var (rot, texture) in textures)
        {
            ref var offset = ref config.GetOffset(rot);
            ref var scale = ref config.GetScale(rot);

            if (offset == null || scale == null)
                continue;
            
            var tempOffset = offset.Value;
            var tempScale = scale.Value;

            list.Label(rot.ToStringHuman());
            // Texture drawing
            var outRect = list.GetRect(drawSize.x * texDim);
            var tvSize = drawSize * texDim;
            var frameSize = Vector2.Scale(tvSize, tempScale);

            var tvRect = new Rect(Vector2.zero, tvSize);
            var frameRect = new Rect(Vector2.zero, frameSize);
            
            tvRect.center = outRect.center;
            frameRect.center = outRect.center + tempOffset * texDim;

            Widgets.DrawTextureFitted(tvRect, texture, 1f, drawSize, new Rect(0f, 0f, 1f, 1f));
            Widgets.DrawBoxSolid(frameRect, new Color(1f, 1f, 1f, 0.3f));
            Widgets.DrawBox(frameRect);

            // Sliders drawing
            list.Gap(8f);
            list.Label($"{"RimFlix_XScale".Translate()}: {Math.Round(tempScale.x, 3):F3}");
            tempScale.x = list.Slider(tempScale.x, 0.1f, 1.0f);
            list.Gap(4f);
            list.Label($"{"RimFlix_YScale".Translate()}: {Math.Round(tempScale.y, 3):F3}");
            tempScale.y = list.Slider(tempScale.y, 0.1f, 1.0f);
            list.Gap(4f);
            list.Label($"{"RimFlix_XOffset".Translate()}: {Math.Round(tempOffset.x, 3):F3}");
            tempOffset.x = list.Slider(tempOffset.x, -1.0f, 1.0f);
            list.Gap(4f);
            list.Label($"{"RimFlix_YOffset".Translate()}: {Math.Round(tempOffset.y, 3):F3}");
            tempOffset.y = list.Slider(tempOffset.y, -1.0f, 1.0f);
            list.Gap(4f);

            if (offset.Value != tempOffset) offset = tempOffset;
            if (scale.Value != tempScale) scale = tempScale;
            
            if (list.ButtonText("RimFlix_DefaultScreen".Translate()))
            {
                var props = def.GetCompProperties<CompProperties_Screen>();
                offset = props.defaultValues.GetOffset(rot);
                scale = props.defaultValues.GetScale(rot);

                offset ??= Vector2.zero;
                scale ??= Vector2.one;
            }

            if (current > 0)
            {
                list.NewColumn();
                list.Gap(gap);
            }

            current--;
        }
        
        Text.Anchor = TextAnchor.UpperLeft;
        list.End();
    }
}