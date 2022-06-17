using System.IO;
using UnityEngine;
using Verse;

namespace RimFlix;

internal class Dialog_Preview : Window
{
    private float padding = 12;
    private float headerHeight = 40;
    private float texDim = 80;

    private string path;
    private string name;
    private Texture2D frameTex;

    private Texture tubeTex;
    private Texture flatTex;
    private Texture megaTex;
    private Texture ultraTex;

    private Vector3 tubeVec;
    private Vector3 flatVec;
    private Vector3 megaVec;
    private Vector3 ultraVec;

    private RimFlixSettings settings;

    public Dialog_Preview(string path, string name)
    {
        doCloseX = true;
        doCloseButton = true;
        forcePause = true;
        absorbInputAroundWindow = true;

        this.path = path;
        this.name = name;
        frameTex = LoadPNG(this.path);

        tubeTex = ThingDef.Named("TubeTelevision").graphic.MatSouth.mainTexture;
        flatTex = ThingDef.Named("FlatscreenTelevision").graphic.MatSouth.mainTexture;
        megaTex = ThingDef.Named("MegascreenTelevision").graphic.MatSouth.mainTexture;
        ultraTex = ThingDef.Named("UltrascreenTV").graphic.MatSouth.mainTexture;

        tubeVec = ThingDef.Named("TubeTelevision").graphicData.drawSize;
        flatVec = ThingDef.Named("FlatscreenTelevision").graphicData.drawSize;
        megaVec = ThingDef.Named("MegascreenTelevision").graphicData.drawSize;
        ultraVec = ThingDef.Named("UltrascreenTV").graphicData.drawSize;

        settings = LoadedModManager.GetMod<RimFlixMod>().GetSettings<RimFlixSettings>();
    }

    public override Vector2 InitialSize => new(610f, 245f);

    private void DoHeader(Rect rect)
    {
        var labelSize = Text.CalcSize("RimFlix_PreviewLabel".Translate());
        var labelRect = rect.LeftPartPixels(labelSize.x);
        var nameRect = rect.RightPartPixels(rect.width - labelRect.width - padding / 2);

        GUI.color = Color.gray;
        Widgets.Label(labelRect, "RimFlix_PreviewLabel".Translate());
        GUI.color = Color.white;
        Widgets.Label(nameRect, name);
    }

    private void DoMain(Rect rect)
    {
        // Building textures
        var x = rect.x;
        var y = rect.y;
        var tubeRect = new Rect(x, y, texDim * tubeVec.x, texDim * tubeVec.y);
        x += tubeRect.width + padding * 2;
        var flatRect = new Rect(x, y, texDim * flatVec.x, texDim * flatVec.y);
        x += flatRect.width + padding * 2;
        var megaRect = new Rect(x, y, texDim * megaVec.x, texDim * megaVec.y);
        x += megaRect.width + padding * 2;
        var ultraRect = new Rect(x, y, texDim * ultraVec.x, texDim * ultraVec.y);

        GUI.DrawTexture(tubeRect, tubeTex);
        GUI.DrawTexture(flatRect, flatTex);
        GUI.DrawTexture(megaRect, megaTex);
        GUI.DrawTexture(ultraRect, ultraTex);

        // Overlay textures
        // var tubeFrame = new Rect(tubeRect.position, GetSize(tubeVec, RimFlixSettings.TubeScale))
        // {
        //     center = tubeRect.center + texDim * RimFlixSettings.TubeOffset
        // };
        // var flatFrame = new Rect(flatRect.position, GetSize(flatVec, RimFlixSettings.FlatScale))
        // {
        //     center = flatRect.center + texDim * RimFlixSettings.FlatOffset
        // };
        // var megaFrame = new Rect(megaRect.position, GetSize(megaVec, RimFlixSettings.MegaScale))
        // {
        //     center = megaRect.center + texDim * RimFlixSettings.MegaOffset
        // };
        // var ultraFrame = new Rect(ultraRect.position, GetSize(ultraVec, RimFlixSettings.UltraScale))
        // {
        //     center = ultraRect.center + texDim * RimFlixSettings.UltraOffset
        // };
        //
        // GUI.DrawTexture(tubeFrame, frameTex);
        // GUI.DrawTexture(flatFrame, frameTex);
        // GUI.DrawTexture(megaFrame, frameTex);
        // GUI.DrawTexture(ultraFrame, frameTex);
        // // Draw borders on mouseover
        // if (Mouse.IsOver(tubeRect))
        // {
        //     Widgets.DrawBox(tubeFrame);
        // }
        // if (Mouse.IsOver(flatRect))
        // {
        //     Widgets.DrawBox(flatFrame);
        // }
        // if (Mouse.IsOver(megaRect))
        // {
        //     Widgets.DrawBox(megaFrame);
        // }
        // if (Mouse.IsOver(ultraRect))
        // {
        //     Widgets.DrawBox(ultraFrame);
        // }
    }

    public override void DoWindowContents(Rect rect)
    {
        // Avoid Close button overlap
        rect.yMax -= 50;

        // Header title
        var headerRect = rect.TopPartPixels(headerHeight);
        DoHeader(headerRect);

        // Main previews
        var mainRect = rect.BottomPartPixels(texDim + padding * 4);
        var innerWidth = texDim * (tubeVec.x + flatVec.x + megaVec.x + ultraVec.x) + padding * 4;
        mainRect.xMin += (mainRect.width - innerWidth) / 2;
        mainRect.yMin += padding * 2;
        DoMain(mainRect);
    }

    private Vector2 GetSize(Vector2 parentSize, Vector2 scale)
    {
        var screenSize = Vector2.Scale(parentSize, scale) * texDim;
        var frameSize = new Vector2(frameTex.width, frameTex.height);
        if (settings.drawType == DrawType.Fit)
        {
            if (frameSize.x / screenSize.x > frameSize.y / screenSize.y)
            {
                screenSize.y = screenSize.x * frameSize.y / frameSize.x;
            }
            else
            {
                screenSize.x = screenSize.y * frameSize.x / frameSize.y;
            }
        }
        return screenSize;
    }

    private Texture2D LoadPNG(string filePath)
    {
        Texture2D texture2D = null;
        if (File.Exists(filePath))
        {
            var data = File.ReadAllBytes(filePath);
            texture2D = new Texture2D(2, 2, TextureFormat.Alpha8, true);
            texture2D.LoadImage(data);
            texture2D.Compress(true);
            texture2D.name = Path.GetFileNameWithoutExtension(filePath);
            texture2D.filterMode = FilterMode.Bilinear;
            texture2D.anisoLevel = 2;
            texture2D.Apply(true, true);
        }
        return texture2D;
    }
}