using System;
using UnityEngine;
using Verse;

namespace RimFlix;

internal class Dialog_AdjustScreen : Window
{
    private float inRectWidth = 840;
    private float inRectHeight = 680;
    private float headerHeight = 40;
    private float texDim;

    private Texture tubeTex;
    private Texture flatTex;
    private Texture megaTex;
    private Texture ultraTex;

    private Vector3 tubeVec;
    private Vector3 flatVec;
    private Vector3 megaVec;
    private Vector3 ultraVec;

    public Dialog_AdjustScreen()
    {
        doCloseX = true;
        doCloseButton = true;
        forcePause = true;
        absorbInputAroundWindow = true;

        tubeTex = ThingDef.Named("TubeTelevision").graphic.MatSouth.mainTexture;
        flatTex = ThingDef.Named("FlatscreenTelevision").graphic.MatSouth.mainTexture;
        megaTex = ThingDef.Named("MegascreenTelevision").graphic.MatSouth.mainTexture;
        ultraTex = ThingDef.Named("UltrascreenTV").graphic.MatSouth.mainTexture;

        tubeVec = ThingDef.Named("TubeTelevision").graphicData.drawSize;
        flatVec = ThingDef.Named("FlatscreenTelevision").graphicData.drawSize;
        megaVec = ThingDef.Named("MegascreenTelevision").graphicData.drawSize;
        ultraVec = ThingDef.Named("UltrascreenTV").graphicData.drawSize;

        // Get fluid dim (Listing column padding is 17 pixels)
        var maxVecX = Math.Max(tubeVec.x, Math.Max(flatVec.x, ultraVec.x));
        texDim = (inRectWidth - 34f) / 3f / maxVecX;
    }

    public override Vector2 InitialSize => new(inRectWidth + 36f, inRectHeight + 36f);

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
        var headerRect = inRect.TopPartPixels(headerHeight);
        Widgets.Label(inRect, "RimFlix_AdjustSreenTitle".Translate());

        // Use inRect for main Listing so screen overlay does not get cut off by header when
        // moved up
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperCenter;
        var list = new Listing_Standard() { ColumnWidth = (inRect.width - 34) / 4 };
        list.Begin(inRect);

        list.Gap(headerRect.height);
        list.Label($"{ThingDef.Named("TubeTelevision").LabelCap}");
        {
            // Tube tv and frame
            var outRect = list.GetRect(texDim);
            Vector2 tvSize = tubeVec * texDim;
            var frameSize = Vector2.Scale(tvSize, RimFlixSettings.TubeScale);
            var tvRect = new Rect(Vector2.zero, tvSize);
            var frameRect = new Rect(Vector2.zero, frameSize);
            tvRect.center = outRect.center;
            frameRect.center = outRect.center + RimFlixSettings.TubeOffset * texDim;
            Widgets.DrawTextureFitted(tvRect, tubeTex, 1f, tubeVec, new Rect(0f, 0f, 1f, 1f), 0f, null);
            Widgets.DrawBoxSolid(frameRect, new Color(1f, 1f, 1f, 0.3f));
            Widgets.DrawBox(frameRect);
        }
        list.Gap(8f);
        list.Label($"{"RimFlix_XScale".Translate()}: {Math.Round(RimFlixSettings.TubeScale.x, 3):F3}");
        RimFlixSettings.TubeScale.x = list.Slider(RimFlixSettings.TubeScale.x, 0.1f, 1.0f);
        list.Gap(4f);
        list.Label($"{"RimFlix_YScale".Translate()}: {Math.Round(RimFlixSettings.TubeScale.y, 3):F3}");
        RimFlixSettings.TubeScale.y = list.Slider(RimFlixSettings.TubeScale.y, 0.1f, 1.0f);
        list.Gap(4f);
        list.Label($"{"RimFlix_XOffset".Translate()}: {Math.Round(RimFlixSettings.TubeOffset.x, 3):F3}");
        RimFlixSettings.TubeOffset.x = list.Slider(RimFlixSettings.TubeOffset.x, -1.0f, 1.0f);
        list.Gap(4f);
        list.Label($"{"RimFlix_YOffset".Translate()}: {Math.Round(RimFlixSettings.TubeOffset.y, 3):F3}");
        RimFlixSettings.TubeOffset.y = list.Slider(RimFlixSettings.TubeOffset.y, -1.0f, 1.0f);
        list.Gap(4f);
        if (list.ButtonText("RimFlix_DefaultScreen".Translate()))
        {
            RimFlixSettings.TubeScale = RimFlixSettings.TubeScaleDefault;
            RimFlixSettings.TubeOffset = RimFlixSettings.TubeOffsetDefault;
        }
        list.NewColumn();

        list.Gap(headerRect.height);
        list.Label($"{ThingDef.Named("FlatscreenTelevision").LabelCap}");
        {
            // Flatscreen tv and frame
            var outRect = list.GetRect(texDim);
            Vector2 tvSize = flatVec * texDim;
            var frameSize = Vector2.Scale(tvSize, RimFlixSettings.FlatScale);
            var tvRect = new Rect(Vector2.zero, tvSize);
            var frameRect = new Rect(Vector2.zero, frameSize);
            tvRect.center = outRect.center;
            frameRect.center = outRect.center + RimFlixSettings.FlatOffset * texDim;
            Widgets.DrawTextureFitted(tvRect, flatTex, 1f, flatVec, new Rect(0f, 0f, 1f, 1f), 0f, null);
            Widgets.DrawBoxSolid(frameRect, new Color(1f, 1f, 1f, 0.3f));
            Widgets.DrawBox(frameRect);
        }
        list.Gap(8f);
        list.Label($"{"RimFlix_XScale".Translate()}: {Math.Round(RimFlixSettings.FlatScale.x, 3):F3}");
        RimFlixSettings.FlatScale.x = list.Slider(RimFlixSettings.FlatScale.x, 0.1f, 1.0f);
        list.Gap(4f);
        list.Label($"{"RimFlix_YScale".Translate()}: {Math.Round(RimFlixSettings.FlatScale.y, 3):F3}");
        RimFlixSettings.FlatScale.y = list.Slider(RimFlixSettings.FlatScale.y, 0.1f, 1.0f);
        list.Gap(4f);
        list.Label($"{"RimFlix_XOffset".Translate()}: {Math.Round(RimFlixSettings.FlatOffset.x, 3):F3}");
        RimFlixSettings.FlatOffset.x = list.Slider(RimFlixSettings.FlatOffset.x, -1.0f, 1.0f);
        list.Gap(4f);
        list.Label($"{"RimFlix_YOffset".Translate()}: {Math.Round(RimFlixSettings.FlatOffset.y, 3):F3}");
        RimFlixSettings.FlatOffset.y = list.Slider(RimFlixSettings.FlatOffset.y, -1.0f, 1.0f);
        list.Gap(4f);
        if (list.ButtonText("RimFlix_DefaultScreen".Translate()))
        {
            RimFlixSettings.FlatScale = RimFlixSettings.FlatScaleDefault;
            RimFlixSettings.FlatOffset = RimFlixSettings.FlatOffsetDefault;
        }
        list.NewColumn();

        list.Gap(headerRect.height);
        list.Label($"{ThingDef.Named("MegascreenTelevision").LabelCap}");
        {
            // Megascreen tv and frame
            var outRect = list.GetRect(texDim);
            Vector2 tvSize = megaVec * texDim;
            var frameSize = Vector2.Scale(tvSize, RimFlixSettings.MegaScale);
            var tvRect = new Rect(Vector2.zero, tvSize);
            var frameRect = new Rect(Vector2.zero, frameSize * 0.75f);
            tvRect.center = outRect.center;
            frameRect.center = outRect.center + RimFlixSettings.MegaOffset * texDim;
            Widgets.DrawTextureFitted(tvRect, megaTex, 0.75f, megaVec, new Rect(0f, 0f, 1f, 1f), 0f, null);
            Widgets.DrawBoxSolid(frameRect, new Color(1f, 1f, 1f, 0.25f));
            Widgets.DrawBox(frameRect);
        }
        list.Gap(8f);
        list.Label($"{"RimFlix_XScale".Translate()}: {Math.Round(RimFlixSettings.MegaScale.x, 3):F3}");
        RimFlixSettings.MegaScale.x = list.Slider(RimFlixSettings.MegaScale.x, 0.1f, 1.0f);
        list.Gap(4f);
        list.Label($"{"RimFlix_YScale".Translate()}: {Math.Round(RimFlixSettings.MegaScale.y, 3):F3}");
        RimFlixSettings.MegaScale.y = list.Slider(RimFlixSettings.MegaScale.y, 0.1f, 1.0f);
        list.Gap(4f);
        list.Label($"{"RimFlix_XOffset".Translate()}: {Math.Round(RimFlixSettings.MegaOffset.x, 3):F3}");
        RimFlixSettings.MegaOffset.x = list.Slider(RimFlixSettings.MegaOffset.x, -1.0f, 1.0f);
        list.Gap(4f);
        list.Label($"{"RimFlix_YOffset".Translate()}: {Math.Round(RimFlixSettings.MegaOffset.y, 3):F3}");
        RimFlixSettings.MegaOffset.y = list.Slider(RimFlixSettings.MegaOffset.y, -1.0f, 1.0f);

        list.Gap(4f);
        if (list.ButtonText("RimFlix_DefaultScreen".Translate()))
        {
            RimFlixSettings.MegaScale = RimFlixSettings.MegaScaleDefault;
            RimFlixSettings.MegaOffset = RimFlixSettings.MegaOffsetDefault;
        }

        list.NewColumn();

        list.Gap(headerRect.height);
        list.Label($"{ThingDef.Named("UltrascreenTV").LabelCap}");
        {
            // Ultrascreentv and frame
            var outRect = list.GetRect(texDim);
            Vector2 tvSize = ultraVec * texDim;
            var frameSize = Vector2.Scale(tvSize, RimFlixSettings.UltraScale);
            var tvRect = new Rect(Vector2.zero, tvSize);
            var frameRect = new Rect(Vector2.zero, frameSize * 0.4f); //.ContractedBy(0.5f);
            tvRect.center = outRect.center;
            frameRect.center = outRect.center + RimFlixSettings.UltraOffset * texDim;
            Widgets.DrawTextureFitted(tvRect, ultraTex, 0.4f, ultraVec, new Rect(0f, 0f, 1f, 1f), 0f, null);
            Widgets.DrawBoxSolid(frameRect, new Color(1f, 1f, 1f, 0.25f));
            Widgets.DrawBox(frameRect);
        }
        list.Gap(8f);
        list.Label($"{"RimFlix_XScale".Translate()}: {Math.Round(RimFlixSettings.UltraScale.x, 3):F3}");
        RimFlixSettings.UltraScale.x = list.Slider(RimFlixSettings.UltraScale.x, 0.1f, 1.0f);
        list.Gap(4f);
        list.Label($"{"RimFlix_YScale".Translate()}: {Math.Round(RimFlixSettings.UltraScale.y, 3):F3}");
        RimFlixSettings.UltraScale.y = list.Slider(RimFlixSettings.UltraScale.y, 0.1f, 1.0f);
        list.Gap(4f);
        list.Label($"{"RimFlix_XOffset".Translate()}: {Math.Round(RimFlixSettings.UltraOffset.x, 3):F3}");
        RimFlixSettings.UltraOffset.x = list.Slider(RimFlixSettings.UltraOffset.x, -1.0f, 1.0f);
        list.Gap(4f);
        list.Label($"{"RimFlix_YOffset".Translate()}: {Math.Round(RimFlixSettings.UltraOffset.y, 3):F3}");
        RimFlixSettings.UltraOffset.y = list.Slider(RimFlixSettings.UltraOffset.y, -1.0f, 1.0f);
        list.Gap(4f);
        if (list.ButtonText("RimFlix_DefaultScreen".Translate()))
        {
            RimFlixSettings.UltraScale = RimFlixSettings.UltraScaleDefault;
            RimFlixSettings.UltraOffset = RimFlixSettings.UltraOffsetDefault;
        }

        Text.Anchor = TextAnchor.UpperLeft;
        list.End();
    }
}