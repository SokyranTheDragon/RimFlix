using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimFlix;

public class RimFlixMod : Mod
{
    public static RimFlixSettings Settings { get; private set; }

    private List<ShowDef> shows = new();

    private List<ShowDef> Shows
    {
        get
        {
            if (!(ShowUpdateTime < RimFlixSettings.showUpdateTime))
                return shows;

            shows = DefDatabase<ShowDef>.AllDefs.Where(s => !s.deleted).ToList();
            foreach (var show in shows)
            {
                if (show.modContentPack == null)
                    show.SortName = $"{"RimFlix_UserShowLabel".Translate()} : {show.label}";
                else
                    show.SortName = $"{show.modContentPack.Name} : {show.label}";

                show.disabled = Settings.disabledShows != null && Settings.disabledShows.Contains(show.defName);
            }

            shows = GetSortedShows(false);
            ShowUpdateTime = RimFlixSettings.showUpdateTime;
            showCountsDirty = true;
            return shows;
        }
    }

    public double ShowUpdateTime = 0;

    private readonly Dictionary<string, int> showCounts = new()
    {
        { "TubeTelevision", 0 },
        { "FlatscreenTelevision", 0 },
        { "MegascreenTelevision", 0 },
        { "UltrascreenTV", 0 }
    };

    private Dictionary<string, int> ShowCounts
    {
        get
        {
            if (!showCountsDirty)
                return showCounts;

            foreach (var key in showCounts.Keys.ToList())
            {
                showCounts[key] = Shows.Count(s => s.televisionDefs.Contains(ThingDef.Named(key)));
            }

            showCountsDirty = false;
            return showCounts;
        }
    }

    private bool showCountsDirty = true;

    private List<FloatMenuOption> drawTypeMenu;

    public List<FloatMenuOption> DrawTypeMenu
        => drawTypeMenu ??= drawTypeMenu = new List<FloatMenuOption>
        {
            new("RimFlix_DrawTypeStretch".Translate(), delegate
            {
                Settings.drawType = DrawType.Stretch;
                RimFlixSettings.screenUpdateTime = RimFlixSettings.TotalSeconds;
            }),
            new("RimFlix_DrawTypeFit".Translate(), delegate
            {
                Settings.drawType = DrawType.Fit;
                RimFlixSettings.screenUpdateTime = RimFlixSettings.TotalSeconds;
            })
            /*
                    new FloatMenuOption("RimFlix_DrawTypeFill".Translate(), delegate {
                        this.Settings.DrawType = DrawType.Fill;
                        RimFlixSettings.screenUpdateTime = RimFlixSettings.TotalSeconds;
                    })*/
        };

    private string[] drawTypeNames;

    public string[] DrawTypeNames
        => drawTypeNames ??= new string[]
        {
            "RimFlix_DrawTypeStretch".Translate(),
            "RimFlix_DrawTypeFit".Translate(),
            "RimFlix_DrawTypeFill".Translate()
        };

    // We need to delay loading textures until game has fully loaded
    private Texture tubeTex;
    private Texture TubeTex => tubeTex ??= ThingDef.Named("TubeTelevision").graphic.MatSouth.mainTexture;

    private Texture frameTex;
    private Texture FlatTex => frameTex ??= ThingDef.Named("FlatscreenTelevision").graphic.MatSouth.mainTexture;

    private Texture megaTex;
    private Texture MegaTex => megaTex ??= ThingDef.Named("MegascreenTelevision").graphic.MatSouth.mainTexture;

    private Texture ultraTex;
    private Texture UltraTex => ultraTex ??= ThingDef.Named("UltrascreenTV").graphic.MatSouth.mainTexture;

    private Texture disabledTex;
    private Texture DisabledTex => disabledTex ??= SolidColorMaterials.NewSolidColorTexture(new Color(0.067f, 0.079f, 0.091f, 0.5f));

    private readonly Color disabledTextColor = new(0.616f, 0.443f, 0.451f);
    private readonly Color disabledLineColor = new(0.616f, 0.443f, 0.451f, 0.3f);

    private Texture2D adjustTex;

    private Texture2D AdjustTex
    {
        get
        {
            if (adjustTex == null)
            {
                adjustTex = ContentFinder<Texture2D>.Get("UI/Buttons/OpenSpecificTab");
            }

            return adjustTex;
        }
    }

    private Vector2 scrollPos = Vector2.zero;
    private float scrollBarWidth = 16f;

    private SortType sortType = SortType.None;
    private bool[] sortAsc = new bool[Enum.GetNames(typeof(SortType)).Length];

    public RimFlixMod(ModContentPack content) : base(content) => Settings = GetSettings<RimFlixSettings>();

    private void DoOptions(Rect rect)
    {
        // 500 x 150
        var labelWidth = 330f;
        var inputWidth = 100f;
        var unitWidth = 70f;
        var padding = 6f;

        rect.x += padding;
        var list = new Listing_Standard(GameFont.Small);
        list.Begin(rect);
        list.Gap(padding);
        {
            var lineRect = list.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(lineRect);
            TooltipHandler.TipRegion(lineRect, "RimFlix_PlayAlwaysTooltip".Translate());

            var checkRect = lineRect.LeftPartPixels(labelWidth + inputWidth);
            Widgets.CheckboxLabeled(checkRect, "RimFlix_PlayAlwaysLabel".Translate(), ref Settings.playAlways, false, null, null, false);
            list.Gap(padding);
        }
        {
            var buffer = Settings.secondsBetweenShows.ToString(CultureInfo.InvariantCulture);
            var lineRect = list.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(lineRect);
            TooltipHandler.TipRegion(lineRect, "RimFlix_SecondsBetweenShowsTooltip".Translate());

            var labelRect = lineRect.LeftPartPixels(labelWidth);
            var tmpRect = lineRect.RightPartPixels(lineRect.width - labelRect.width);
            var inputRect = tmpRect.LeftPartPixels(inputWidth);
            var unitRect = tmpRect.RightPartPixels(unitWidth);
            Widgets.Label(labelRect, "RimFlix_SecondsBetweenShowsLabel".Translate());
            Widgets.TextFieldNumeric(inputRect, ref Settings.secondsBetweenShows, ref buffer, 1, 10000);
            Widgets.Label(unitRect, " " + "RimFlix_SecondsBetweenShowsUnits".Translate());
            list.Gap(padding);
        }
        {
            var buffer = Settings.powerConsumptionOn.ToString(CultureInfo.InvariantCulture);
            var lineRect = list.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(lineRect);
            TooltipHandler.TipRegion(lineRect, "RimFlix_PowerConsumptionOnTooltip".Translate());

            var labelRect = lineRect.LeftPartPixels(labelWidth);
            var tmpRect = lineRect.RightPartPixels(lineRect.width - labelRect.width);
            var inputRect = tmpRect.LeftPartPixels(inputWidth);
            var unitRect = tmpRect.RightPartPixels(unitWidth);
            Widgets.Label(labelRect, "RimFlix_PowerConsumptionOnLabel".Translate());
            Widgets.TextFieldNumeric(inputRect, ref Settings.powerConsumptionOn, ref buffer, 0, 10000);
            Widgets.Label(unitRect, " %");
            list.Gap(padding);
        }
        {
            var buffer = Settings.powerConsumptionOff.ToString(CultureInfo.InvariantCulture);
            var lineRect = list.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(lineRect);
            TooltipHandler.TipRegion(lineRect, "RimFlix_PowerConsumptionOffTooltip".Translate());

            var labelRect = lineRect.LeftPartPixels(labelWidth);
            var tmpRect = lineRect.RightPartPixels(lineRect.width - labelRect.width);
            var inputRect = tmpRect.LeftPartPixels(inputWidth);
            var unitRect = tmpRect.RightPartPixels(unitWidth);
            Widgets.Label(labelRect, "RimFlix_PowerConsumptionOffLabel".Translate());
            Widgets.TextFieldNumeric(inputRect, ref Settings.powerConsumptionOff, ref buffer, 0, 10000);
            Widgets.Label(unitRect, " %");
            list.Gap(padding);
        }
        {
            var lineRect = list.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(lineRect);
            TooltipHandler.TipRegion(lineRect, "RimFlix_DrawTypeTooltip".Translate());

            var labelRect = lineRect.LeftPartPixels(labelWidth);
            var tmpRect = lineRect.RightPartPixels(lineRect.width - labelRect.width);
            var buttonRect = tmpRect.LeftPartPixels(inputWidth);
            Widgets.Label(labelRect, "RimFlix_DrawTypeLabel".Translate());
            if (Widgets.ButtonText(buttonRect, DrawTypeNames[(int)Settings.drawType]))
            {
                Find.WindowStack.Add(new FloatMenu(DrawTypeMenu));
            }

            list.Gap(padding);
        }
        list.End();
    }

    private void DoStatus(Rect rect)
    {
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperCenter;
        var headerRect = new Rect(rect.x, rect.y - 8, rect.width, Text.LineHeight);
        Widgets.Label(headerRect, "RimFlix_ShowsLabel".Translate());

        Text.Font = GameFont.Small;
        float labelWidth = 200;
        var padding = 2f;
        var tableRect = new Rect(rect.x, rect.y + headerRect.height, rect.width, (Text.LineHeight + padding) * 4);
        GUI.BeginGroup(tableRect);
        {
            var labelRect = new Rect(0, 0, labelWidth, Text.LineHeight);
            var countRect = new Rect(labelWidth, 0, tableRect.width - labelWidth - 16, Text.LineHeight);
            var i = 0;
            foreach (var item in ShowCounts)
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, ThingDef.Named(item.Key).LabelCap);
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(countRect, $"{item.Value}");
                labelRect.y = countRect.y = (Text.LineHeight + padding) * ++i;
            }
        }
        GUI.EndGroup();

        var addRect = new Rect(rect.x, rect.y + headerRect.height + tableRect.height + 8, rect.width - 16, Text.LineHeight + 4);
        TooltipHandler.TipRegion(addRect, "RimFlix_AddShowTooltip".Translate());
        if (Widgets.ButtonText(addRect, "RimFlix_AddShowButton".Translate()))
        {
            Find.WindowStack.Add(new Dialog_AddShow());
        }

        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DoShows(Rect rect)
    {
        // 864 x 418
        var cellHeight = Text.LineHeight;
        float cellPadding = 2;
        float nameWidth = 400;
        float framesWidth = 80;
        float timeWidth = 80;
        float tubeWidth = 40;
        float flatWidth = 40;
        float megaWidth = 40;
        float ultraWidth = 40;
        float editWidth = 50;
        float deleteWidth = 60;

        // Header row
        GUI.skin.GetStyle("Label").alignment = TextAnchor.MiddleCenter;
        Text.Anchor = TextAnchor.MiddleCenter;
        var headerRect = rect.TopPartPixels(cellHeight);
        //Widgets.DrawMenuSection(headerRect);
        Widgets.DrawLineHorizontal(headerRect.x, headerRect.y + cellHeight, headerRect.width);
        var x = headerRect.x;
        var y = headerRect.y;

        var nameRect = new Rect(x, y, nameWidth, cellHeight);
        TooltipHandler.TipRegion(nameRect, $"{"RimFlix_SortHeader".Translate()} {"RimFlix_NameHeader".Translate()}");
        if (GUI.Button(nameRect, $"RimFlix_NameHeader".Translate(), GUI.skin.GetStyle("Label")))
        {
            sortType = SortType.Name;
            shows = GetSortedShows(true);
        }

        x += nameWidth + cellPadding;

        var framesRect = new Rect(x, y, framesWidth, cellHeight);
        TooltipHandler.TipRegion(framesRect, $"{"RimFlix_SortHeader".Translate()} {"RimFlix_FramesHeader".Translate()}");
        if (GUI.Button(framesRect, "RimFlix_FramesHeader".Translate(), GUI.skin.GetStyle("Label")))
        {
            sortType = SortType.Frames;
            shows = GetSortedShows(true);
        }

        x += framesWidth + cellPadding;

        var timeRect = new Rect(x, y, framesWidth, cellHeight);
        TooltipHandler.TipRegion(timeRect, $"{"RimFlix_SortHeader".Translate()} {"RimFlix_SecFrameHeader".Translate()}");
        if (GUI.Button(timeRect, "RimFlix_SecFrameHeader".Translate(), GUI.skin.GetStyle("Label")))
        {
            sortType = SortType.Time;
            shows = GetSortedShows(true);
        }

        x += timeWidth + cellPadding;

        var tubeRect = new Rect(x, y, tubeWidth, cellHeight);
        Widgets.DrawTextureFitted(tubeRect, TubeTex, 0.6f);
        TooltipHandler.TipRegion(tubeRect, ThingDef.Named("TubeTelevision").LabelCap);
        if (GUI.Button(tubeRect, "", GUI.skin.GetStyle("Label")))
        {
            sortType = SortType.Tube;
            shows = GetSortedShows(true);
        }

        x += tubeWidth + cellPadding;

        var flatRect = new Rect(x, y, flatWidth, cellHeight);
        Widgets.DrawTextureFitted(flatRect, FlatTex, 0.55f);
        TooltipHandler.TipRegion(flatRect, ThingDef.Named("FlatscreenTelevision").LabelCap);
        if (GUI.Button(flatRect, "", GUI.skin.GetStyle("Label")))
        {
            sortType = SortType.Flat;
            shows = GetSortedShows(true);
        }

        x += flatWidth + cellPadding;

        var megaRect = new Rect(x, y, megaWidth, cellHeight);
        Widgets.DrawTextureFitted(megaRect, MegaTex, 0.9f);
        TooltipHandler.TipRegion(megaRect, ThingDef.Named("MegascreenTelevision").LabelCap);
        if (GUI.Button(megaRect, "", GUI.skin.GetStyle("Label")))
        {
            sortType = SortType.Mega;
            shows = GetSortedShows(true);
        }

        x += megaWidth + cellPadding;

        var ultraRect = new Rect(x, y, ultraWidth, cellHeight);
        Widgets.DrawTextureFitted(ultraRect, UltraTex, 0.75f);
        TooltipHandler.TipRegion(ultraRect, ThingDef.Named("UltrascreenTV").LabelCap);
        if (GUI.Button(ultraRect, "", GUI.skin.GetStyle("Label")))
        {
            sortType = SortType.Ultra;
            shows = GetSortedShows(true);
        }

        x += ultraWidth + cellPadding;

        var actionRect = new Rect(x, y, editWidth + deleteWidth + cellPadding, cellHeight);
        //Widgets.Label(actionRect, "RimFlix_ActionsHeader".Translate());
        if (GUI.Button(actionRect, "RimFlix_ActionsHeader".Translate(), GUI.skin.GetStyle("Label")))
        {
            sortType = SortType.Action;
            shows = GetSortedShows(true);
        }

        var editRect = new Rect(x, y, editWidth, cellHeight);
        var deleteRect = new Rect(x + editWidth + cellPadding, y, deleteWidth, cellHeight);

        // Table rows
        Text.Anchor = TextAnchor.UpperLeft;
        var tableRect = rect.BottomPartPixels(rect.height - cellHeight - cellPadding);
        x = tableRect.x;
        y = tableRect.y + cellPadding;
        var viewHeight = Shows.Count * (cellHeight + cellPadding) + 50f;
        var viewRect = new Rect(x, y, tableRect.width - scrollBarWidth, viewHeight);
        var rowRect = new Rect(x, y, rect.width, cellHeight);
        Widgets.BeginScrollView(tableRect, ref scrollPos, viewRect, true);
        var index = 0;
        foreach (var show in Shows)
        {
            rowRect.y = nameRect.y = framesRect.y =
                timeRect.y = tubeRect.y = flatRect.y = megaRect.y = ultraRect.y = actionRect.y = editRect.y = deleteRect.y = y + (cellHeight + cellPadding) * index++;

            if (index % 2 == 1)
            {
                //Widgets.DrawAltRect(rectRow);
                Widgets.DrawAltRect(nameRect);
                Widgets.DrawAltRect(framesRect);
                Widgets.DrawAltRect(timeRect);
                Widgets.DrawAltRect(tubeRect);
                Widgets.DrawAltRect(flatRect);
                Widgets.DrawAltRect(megaRect);
                Widgets.DrawAltRect(ultraRect);
                Widgets.DrawAltRect(editRect);
                Widgets.DrawAltRect(deleteRect);
            }

            if (show.disabled)
            {
                GUI.color = disabledTextColor;
            }

            Widgets.DrawHighlightIfMouseover(rowRect);

            Text.Anchor = TextAnchor.MiddleLeft;
            TooltipHandler.TipRegion(nameRect, show.description);
            Widgets.Label(nameRect, show.SortName);

            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(framesRect, $"{show.frames.Count} ");
            Widgets.Label(timeRect, $"{show.secondsBetweenFrames:F2} ");

            Text.Anchor = TextAnchor.MiddleCenter;
            TooltipHandler.TipRegion(tubeRect, ThingDef.Named("TubeTelevision").LabelCap);
            if (show.televisionDefs.Contains(ThingDef.Named("TubeTelevision")))
            {
                Widgets.DrawTextureFitted(tubeRect, TubeTex, 0.7f);
                TooltipHandler.TipRegion(tubeRect, ThingDef.Named("TubeTelevision").LabelCap);
            }

            TooltipHandler.TipRegion(flatRect, ThingDef.Named("FlatscreenTelevision").LabelCap);
            if (show.televisionDefs.Contains(ThingDef.Named("FlatscreenTelevision")))
            {
                Widgets.DrawTextureFitted(flatRect, FlatTex, 0.55f);
                TooltipHandler.TipRegion(flatRect, ThingDef.Named("FlatscreenTelevision").LabelCap);
            }

            TooltipHandler.TipRegion(megaRect, ThingDef.Named("MegascreenTelevision").LabelCap);
            if (show.televisionDefs.Contains(ThingDef.Named("MegascreenTelevision")))
            {
                Widgets.DrawTextureFitted(megaRect, MegaTex, 0.9f);
                TooltipHandler.TipRegion(megaRect, ThingDef.Named("MegascreenTelevision").LabelCap);
            }

            TooltipHandler.TipRegion(ultraRect, ThingDef.Named("UltrascreenTV").LabelCap);
            if (show.televisionDefs.Contains(ThingDef.Named("UltrascreenTV")))
            {
                Widgets.DrawTextureFitted(ultraRect, UltraTex, 0.75f);
                TooltipHandler.TipRegion(ultraRect, ThingDef.Named("UltrascreenTV").LabelCap);
            }

            GUI.color = Color.white;
            if (show is UserShowDef userShow)
            {
                if (Widgets.ButtonText(editRect, "RimFlix_EditButton".Translate()))
                {
                    Find.WindowStack.Add(new Dialog_AddShow(userShow, this));
                }

                if (Widgets.ButtonText(deleteRect, "RimFlix_DeleteButton".Translate()))
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "RimFlix_ConfirmDelete".Translate(userShow.SortName), delegate { UserContent.RemoveUserShow(userShow); }, true, null));
                }
            }
            else
            {
                if (show.disabled)
                {
                    if (Widgets.ButtonText(actionRect, "RimFlix_EnableButton".Translate()))
                    {
                        show.disabled = false;
                        Settings.disabledShows.Remove(show.defName);
                        // We want to alert CompScreen of show update, but avoid messing up sort
                        // order by requerying here
                        ShowUpdateTime = RimFlixSettings.showUpdateTime = RimFlixSettings.TotalSeconds;
                    }
                }
                else
                {
                    if (Widgets.ButtonText(actionRect, "RimFlix_DisableButton".Translate()))
                    {
                        show.disabled = true;
                        Settings.disabledShows.Add(show.defName);
                        // We want to alert CompScreen of show update, but avoid messing up sort
                        // order by requerying here
                        ShowUpdateTime = RimFlixSettings.showUpdateTime = RimFlixSettings.TotalSeconds;
                    }
                }
            }

            if (show.disabled)
            {
                GUI.DrawTexture(rowRect, DisabledTex);
                var leftVec = new Vector2(rowRect.x, rowRect.y + cellHeight / 2);
                var rightVec = new Vector2(rowRect.x + rowRect.width - scrollBarWidth, rowRect.y + cellHeight / 2);
                Widgets.DrawLine(leftVec, rightVec, disabledLineColor, 2f);
            }
        }

        Widgets.EndScrollView();
        GUI.skin.GetStyle("Label").alignment = TextAnchor.UpperLeft;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    public override void DoSettingsWindowContents(Rect rect)
    {
        // 864 x 584
        var optionsWidth = 500f;
        var statusWidth = 250f;
        var optionsHeight = 150f;
        var showsHeight = 400f;

        // Adjust screen button
        Text.Font = GameFont.Medium;
        var titleSize = Text.CalcSize("RimFlix_Title".Translate());
        var adjustRect = new Rect(titleSize.x + 180, 2, 24, 24);
        TooltipHandler.TipRegion(adjustRect, "RimFlix_AdjustSreenTitle".Translate());
        if (Widgets.ButtonImage(adjustRect, AdjustTex))
        {
            Find.WindowStack.Add(new Dialog_AdjustScreen());
        }

        Text.Font = GameFont.Small;
        var topRect = rect.TopPartPixels(optionsHeight);
        var optionsRect = topRect.LeftPartPixels(optionsWidth);
        var statusRect = topRect.RightPartPixels(statusWidth);
        var showsRect = rect.BottomPartPixels(showsHeight);

        DoOptions(optionsRect);
        DoStatus(statusRect);
        DoShows(showsRect);

        base.DoSettingsWindowContents(rect);
    }

    public override string SettingsCategory()
    {
        return "RimFlix_Title".Translate();
    }

    // Use cached shows to avoid possible infinite recursion
    private List<ShowDef> GetSortedShows(bool toggleAsc)
    {
        var i = (int)sortType;
        if (toggleAsc)
        {
            sortAsc[i] = !sortAsc[i];
        }

        if (sortType == SortType.Name)
        {
            return sortAsc[i]
                ? shows.OrderBy(s => s.SortName).ToList()
                : shows.OrderByDescending(s => s.SortName).ToList();
        }

        if (sortType == SortType.Frames)
        {
            return sortAsc[i]
                ? shows.OrderBy(s => s.frames.Count).ToList()
                : shows.OrderByDescending(s => s.frames.Count).ToList();
        }

        if (sortType == SortType.Time)
        {
            return sortAsc[i]
                ? shows.OrderBy(s => s.secondsBetweenFrames).ToList()
                : shows.OrderByDescending(s => s.secondsBetweenFrames).ToList();
        }

        if (sortType == SortType.Tube)
        {
            return sortAsc[i]
                ? shows.OrderBy(s => s.televisionDefs.Contains(ThingDef.Named("TubeTelevision"))).ToList()
                : shows.OrderByDescending(s => s.televisionDefs.Contains(ThingDef.Named("TubeTelevision"))).ToList();
        }

        if (sortType == SortType.Flat)
        {
            return sortAsc[i]
                ? shows.OrderBy(s => s.televisionDefs.Contains(ThingDef.Named("FlatscreenTelevision"))).ToList()
                : shows.OrderByDescending(s => s.televisionDefs.Contains(ThingDef.Named("FlatscreenTelevision"))).ToList();
        }

        if (sortType == SortType.Mega)
        {
            return sortAsc[i]
                ? shows.OrderBy(s => s.televisionDefs.Contains(ThingDef.Named("MegascreenTelevision"))).ToList()
                : shows.OrderByDescending(s => s.televisionDefs.Contains(ThingDef.Named("MegascreenTelevision"))).ToList();
        }

        if (sortType == SortType.Ultra)
        {
            return sortAsc[i]
                ? shows.OrderBy(s => s.televisionDefs.Contains(ThingDef.Named("UltrascreenTV"))).ToList()
                : shows.OrderByDescending(s => s.televisionDefs.Contains(ThingDef.Named("UltrascreenTV"))).ToList();
        }

        if (sortType == SortType.Action)
        {
            return sortAsc[i]
                ? shows.OrderBy(s => s.disabled).ToList()
                : shows.OrderByDescending(s => s.disabled).ToList();
        }

        return shows;
    }
}

public enum SortType
{
    None,
    Name,
    Frames,
    Time,
    Tube,
    Flat,
    Mega,
    Ultra,
    Action
}

public enum DrawType
{
    Stretch,
    Fit,
    Fill
}