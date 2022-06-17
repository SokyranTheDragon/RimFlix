using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
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
    public List<ShowStatus> showStatus = new();

    public Dictionary<string, Values> defValues = new();

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

        Scribe_Collections.Look(ref userShows, "userShows", LookMode.Deep);
        Scribe_Collections.Look(ref defValues, "defValues", LookMode.Value, LookMode.Deep);
        Scribe_Collections.Look(ref showStatus, "showStatus", LookMode.Deep);

        showStatus ??= new List<ShowStatus>();
        userShows ??= new List<UserShowDef>();
        defValues ??= new Dictionary<string, Values>();

        base.ExposeData();
    }

    private Texture2D adjustTex;

    // Setting window
    private Texture2D AdjustTex => adjustTex ??= ContentFinder<Texture2D>.Get("UI/Buttons/OpenSpecificTab");

    private string[] drawTypeNames;

    public string[] DrawTypeNames => drawTypeNames ??= new string[]
    {
        "RimFlix_DrawTypeStretch".Translate(),
        "RimFlix_DrawTypeFit".Translate(),
        "RimFlix_DrawTypeFill".Translate()
    };

    private List<FloatMenuOption> drawTypeMenu;

    public List<FloatMenuOption> DrawTypeMenu => drawTypeMenu ??= drawTypeMenu = new List<FloatMenuOption>
    {
        new("RimFlix_DrawTypeStretch".Translate(), () =>
        {
            drawType = DrawType.Stretch;
            screenUpdateTime = TotalSeconds;
        }),
        new("RimFlix_DrawTypeFit".Translate(), () =>
        {
            drawType = DrawType.Fit;
            screenUpdateTime = TotalSeconds;
        }),
        //new("RimFlix_DrawTypeFill".Translate(), () => 
        //{
        //    this.Settings.DrawType = DrawType.Fill;
        //    RimFlixSettings.screenUpdateTime = RimFlixSettings.TotalSeconds;
        //})
    };

    private List<ShowDef> shows = new();

    private List<ShowDef> Shows
    {
        get
        {
            if (!(ShowUpdateTime < showUpdateTime))
                return shows;

            shows = DefDatabase<ShowDef>.AllDefs.Where(s => !s.deleted).ToList();
            foreach (var show in shows)
            {
                if (show.modContentPack == null)
                    show.SortName = $"{"RimFlix_UserShowLabel".Translate()} : {show.label}";
                else
                    show.SortName = $"{show.modContentPack.Name} : {show.label}";

                show.RecheckDisabled();
            }

            shows = GetSortedShows(false);
            ShowUpdateTime = showUpdateTime;
            return shows;
        }
    }

    public double ShowUpdateTime = 0;

    private Texture disabledTex;
    private Texture DisabledTex => disabledTex ??= SolidColorMaterials.NewSolidColorTexture(new Color(0.067f, 0.079f, 0.091f, 0.5f));

    private readonly Color disabledTextColor = new(0.616f, 0.443f, 0.451f);
    private readonly Color disabledLineColor = new(0.616f, 0.443f, 0.451f, 0.3f);

    private Vector2 scrollPos = Vector2.zero;
    private float scrollBarWidth = 16f;

    private SortType sortType = SortType.None;
    private bool[] sortAsc = new bool[Enum.GetNames(typeof(SortType)).Length];

    // Use cached shows to avoid possible infinite recursion
    private List<ShowDef> GetSortedShows(bool toggleAsc)
    {
        var i = (int)sortType;
        if (toggleAsc) 
            sortAsc[i] = !sortAsc[i];

        return sortType switch
        {
            SortType.Name => sortAsc[i]
                ? shows.OrderBy(s => s.SortName).ToList()
                : shows.OrderByDescending(s => s.SortName).ToList(),
            SortType.Frames => sortAsc[i]
                ? shows.OrderBy(s => s.frames.Count).ToList()
                : shows.OrderByDescending(s => s.frames.Count).ToList(),
            SortType.Time => sortAsc[i]
                ? shows.OrderBy(s => s.secondsBetweenFrames).ToList()
                : shows.OrderByDescending(s => s.secondsBetweenFrames).ToList(),
            SortType.Action => sortAsc[i]
                ? shows.OrderBy(s => s.disabled).ToList()
                : shows.OrderByDescending(s => s.disabled).ToList(),
            _ => shows
        };
    }

    public void DoSettingsWindowContents(Rect rect)
    {
        // 864 x 584
        const float optionsWidth = 500f;
        const float statusWidth = 250f;
        const float optionsHeight = 150f;
        const float showsHeight = 400f;

        // Adjust screen button
        Text.Font = GameFont.Medium;
        var titleSize = Text.CalcSize("RimFlix_Title".Translate());
        var adjustRect = new Rect(titleSize.x + 180, 2, 24, 24);
        TooltipHandler.TipRegion(adjustRect, "RimFlix_AdjustSreenTitle".Translate());
        if (Widgets.ButtonImage(adjustRect, AdjustTex))
        {
            var defs = DefDatabase<ThingDef>.AllDefs.Select(def => (def, props: def.GetCompProperties<CompProperties_Screen>())).Where(x => x.props != null).ToArray();

            foreach (var (def, props) in defs)
            {
                if (!defValues.ContainsKey(def.defName))
                    defValues[def.defName] = props.defaultValues ?? new Values();
            }

            var options = defs.Select(tuple =>
            {
                var def = tuple.def;
                void OpenDialog() => Find.WindowStack.Add(new Dialog_AdjustScreen(def));

                return new FloatMenuOption(def.LabelCap, OpenDialog, def);
            }).ToList();

            Find.WindowStack.Add(new FloatMenu(options));
        }

        Text.Font = GameFont.Small;
        var topRect = rect.TopPartPixels(optionsHeight);
        var optionsRect = topRect.LeftPartPixels(optionsWidth);
        var statusRect = topRect.RightPartPixels(statusWidth);
        var showsRect = rect.BottomPartPixels(showsHeight);

        DoOptions(optionsRect);
        DoStatus(statusRect);
        DoShows(showsRect);
    }

    private void DoOptions(Rect rect)
    {
        // 500 x 150
        const float labelWidth = 330f;
        const float inputWidth = 100f;
        const float unitWidth = 70f;
        const float padding = 6f;

        rect.x += padding;
        var list = new Listing_Standard(GameFont.Small);
        list.Begin(rect);
        list.Gap(padding);
        {
            var lineRect = list.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(lineRect);
            TooltipHandler.TipRegion(lineRect, "RimFlix_PlayAlwaysTooltip".Translate());

            var checkRect = lineRect.LeftPartPixels(labelWidth + inputWidth);
            Widgets.CheckboxLabeled(checkRect, "RimFlix_PlayAlwaysLabel".Translate(), ref playAlways);
            list.Gap(padding);
        }
        {
            var buffer = secondsBetweenShows.ToString(CultureInfo.InvariantCulture);
            var lineRect = list.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(lineRect);
            TooltipHandler.TipRegion(lineRect, "RimFlix_SecondsBetweenShowsTooltip".Translate());

            var labelRect = lineRect.LeftPartPixels(labelWidth);
            var tmpRect = lineRect.RightPartPixels(lineRect.width - labelRect.width);
            var inputRect = tmpRect.LeftPartPixels(inputWidth);
            var unitRect = tmpRect.RightPartPixels(unitWidth);
            Widgets.Label(labelRect, "RimFlix_SecondsBetweenShowsLabel".Translate());
            Widgets.TextFieldNumeric(inputRect, ref secondsBetweenShows, ref buffer, 1, 10000);
            Widgets.Label(unitRect, " " + "RimFlix_SecondsBetweenShowsUnits".Translate());
            list.Gap(padding);
        }
        {
            var buffer = powerConsumptionOn.ToString(CultureInfo.InvariantCulture);
            var lineRect = list.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(lineRect);
            TooltipHandler.TipRegion(lineRect, "RimFlix_PowerConsumptionOnTooltip".Translate());

            var labelRect = lineRect.LeftPartPixels(labelWidth);
            var tmpRect = lineRect.RightPartPixels(lineRect.width - labelRect.width);
            var inputRect = tmpRect.LeftPartPixels(inputWidth);
            var unitRect = tmpRect.RightPartPixels(unitWidth);
            Widgets.Label(labelRect, "RimFlix_PowerConsumptionOnLabel".Translate());
            Widgets.TextFieldNumeric(inputRect, ref powerConsumptionOn, ref buffer, 0, 10000);
            Widgets.Label(unitRect, " %");
            list.Gap(padding);
        }
        {
            var buffer = powerConsumptionOff.ToString(CultureInfo.InvariantCulture);
            var lineRect = list.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(lineRect);
            TooltipHandler.TipRegion(lineRect, "RimFlix_PowerConsumptionOffTooltip".Translate());

            var labelRect = lineRect.LeftPartPixels(labelWidth);
            var tmpRect = lineRect.RightPartPixels(lineRect.width - labelRect.width);
            var inputRect = tmpRect.LeftPartPixels(inputWidth);
            var unitRect = tmpRect.RightPartPixels(unitWidth);
            Widgets.Label(labelRect, "RimFlix_PowerConsumptionOffLabel".Translate());
            Widgets.TextFieldNumeric(inputRect, ref powerConsumptionOff, ref buffer, 0, 10000);
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
            if (Widgets.ButtonText(buttonRect, DrawTypeNames[(int)drawType]))
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
        var padding = 2f;
        var tableRect = new Rect(rect.x, rect.y + headerRect.height, rect.width, (Text.LineHeight + padding) * 4);

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
        const float cellPadding = 2;
        const float framesWidth = 80;
        const float timeWidth = 80;
        const float editWidth = 50;
        const float deleteWidth = 60;
        const float nameWidth = 864 - framesWidth - timeWidth - editWidth - deleteWidth - (4 * cellPadding);

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

        var actionRect = new Rect(x, y, editWidth + deleteWidth + cellPadding, cellHeight);
        //Widgets.Label(actionRect, "RimFlix_ActionsHeader".Translate());
        if (GUI.Button(actionRect, "RimFlix_ActionsHeader".Translate(), GUI.skin.GetStyle("Label")))
        {
            sortType = SortType.Action;
            shows = GetSortedShows(true);
        }

        var editRect = new Rect(x, y, editWidth, cellHeight);
        var deleteRect = new Rect(x + editWidth + cellPadding, y, deleteWidth, cellHeight);
        var bigEditRect = new Rect(x, y, editWidth + cellPadding + deleteWidth, cellHeight);

        // Table rows
        Text.Anchor = TextAnchor.UpperLeft;
        var tableRect = rect.BottomPartPixels(rect.height - cellHeight - cellPadding);
        x = tableRect.x;
        y = tableRect.y + cellPadding;
        var viewHeight = Shows.Count * (cellHeight + cellPadding) + 50f;
        var viewRect = new Rect(x, y, tableRect.width - scrollBarWidth, viewHeight);
        var rowRect = new Rect(x, y, rect.width, cellHeight);
        Widgets.BeginScrollView(tableRect, ref scrollPos, viewRect);
        var index = 0;
        foreach (var show in Shows)
        {
            rowRect.y = nameRect.y = framesRect.y = timeRect.y = actionRect.y = editRect.y = deleteRect.y = bigEditRect.y = y + (cellHeight + cellPadding) * index++;
            var userShow = show as UserShowDef;

            if (index % 2 == 1)
            {
                //Widgets.DrawAltRect(rectRow);
                Widgets.DrawAltRect(nameRect);
                Widgets.DrawAltRect(framesRect);
                Widgets.DrawAltRect(timeRect);

                if (userShow != null)
                {
                    Widgets.DrawAltRect(editRect);
                    Widgets.DrawAltRect(deleteRect);
                }
                else Widgets.DrawAltRect(bigEditRect);
            }

            if (show.disabled)
                GUI.color = disabledTextColor;

            Widgets.DrawHighlightIfMouseover(rowRect);

            Text.Anchor = TextAnchor.MiddleLeft;
            TooltipHandler.TipRegion(nameRect, show.description);
            Widgets.Label(nameRect, show.SortName);

            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(framesRect, $"{show.frames.Count} ");
            Widgets.Label(timeRect, $"{show.secondsBetweenFrames:F2} ");

            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;

            if (Widgets.ButtonText(userShow == null ? bigEditRect : editRect, "RimFlix_EditButton".Translate()))
            {
                Find.WindowStack.Add(new Dialog_AddShow(show));
            }

            if (userShow != null)
            {
                if (Widgets.ButtonText(deleteRect, "RimFlix_DeleteButton".Translate()))
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "RimFlix_ConfirmDelete".Translate(userShow.SortName), () => UserContent.RemoveUserShow(userShow), true));
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
}