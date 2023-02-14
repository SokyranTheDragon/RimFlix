using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimFlix;

internal class Dialog_AddShow : Window
{
    private readonly ShowDef currentShow;
    private readonly UserShowDef currentUserShow;

    // Widget sizes
    private const float Padding = 8;

    private const float ScrollBarWidth = 16;
    private readonly float drivesWidth = 80;
    private readonly float filesWidth = 370;
    private const float OptionsWidth = 250;
    private readonly float pathHeight = Text.LineHeight;
    private const float ButtonsHeight = 32;
    private readonly float timeInputWidth;
    private readonly float timeUnitWidth;
    private readonly float configTvWidth;

    // File explorer panel
    private readonly string[] drives;

    private string currentPath;
    private string lastPath = "";
    private readonly Regex pathValidator;
    private readonly Texture2D refreshTex;
    private bool dirInfoDirty = true;
    private DirectoryInfo dirInfo;
    private IEnumerable<DirectoryInfo> dirs;
    private IEnumerable<FileInfo> files;
    private readonly Color drivesBackgroundColor = new(0.17f, 0.18f, 0.19f);
    private readonly Color filesBackgroundColor = new(0.08f, 0.09f, 0.11f);
    private readonly Color filesTextColor = new(0.6f, 0.6f, 0.6f);
    private Vector2 drivesScrollPos = Vector2.zero;
    private Vector2 filesScrollPos = Vector2.zero;

    // Make abnormally large directories manageable
    private int fileCount = 0;

    private int dirCount = 0;
    private const int MaxFileCount = 1000;

    // Options panel
    private int framesCount;

    private string showName;
    private float timeValue;
    private TimeUnit timeUnit = TimeUnit.Second;
    private readonly string[] timeUnitLabels;
    private readonly List<FloatMenuOption> timeUnitMenu;
    private readonly ShowStatus status;

    public Dialog_AddShow(ShowDef show = null)
    {
        // Window properties
        doCloseX = true;
        doCloseButton = false;
        forcePause = true;
        absorbInputAroundWindow = true;
        currentShow = show;

        // Initialize object
        refreshTex = ContentFinder<Texture2D>.Get("UI/Buttons/Refresh", true);
        var v1 = Text.CalcSize("RimFlix_TimeSeconds".Translate());
        var v2 = Text.CalcSize("RimFlix_TimeTicks".Translate());
        timeUnitWidth = Math.Max(v1.x, v2.x) + Padding * 2;
        timeInputWidth = OptionsWidth - timeUnitWidth - Padding * 2;
        configTvWidth = Text.CalcSize("RimFlix_ConfigureSupportedTv".Translate()).x + Padding * 2;
        timeUnitMenu = new List<FloatMenuOption>
        {
            new("RimFlix_TimeSeconds".Translate(), delegate { timeUnit = TimeUnit.Second; }),
            new("RimFlix_TimeTicks".Translate(), delegate { timeUnit = TimeUnit.Tick; })
        };
        timeUnitLabels = new string[]
        {
            "RimFlix_TimeSeconds".Translate(),
            "RimFlix_TimeTicks".Translate()
        };
        var s = new string(Path.GetInvalidFileNameChars());
        pathValidator = new Regex(string.Format("[^{0}]*", Regex.Escape(s)));
        try
        {
            drives = Directory.GetLogicalDrives();
        }
        catch (Exception ex)
        {
            Log.Message($"RimFlix: Exception for GetLogicalDrives():\n{ex}");
            filesWidth += drivesWidth;
            drivesWidth = 0;
        }

        // Show properties
        if (currentShow != null)
        {
            showName = currentShow.label;
            timeValue = currentShow.secondsBetweenFrames;
            currentUserShow = currentShow as UserShowDef;

            status = RimFlixMod.Settings.showStatus.FirstOrDefault(x => x.showDefName == currentShow.defName);
            status.FillDefaults();
        }
        else
        {
            showName = "RimFlix_DefaultName".Translate();
            timeValue = 10;

            status = new ShowStatus();
            status.FillDefaults();
        }

        var settings = RimFlixMod.Settings;
        currentPath = Directory.Exists(settings.lastPath) ? settings.lastPath : settings.defaultPath;
    }

    public override Vector2 InitialSize => new(736f, 536f);

    private void DoExplorer(Rect rect)
    {
        var rectPath = rect.TopPartPixels(pathHeight);
        DoPath(rectPath);

        var rectItems = rect.BottomPartPixels(rect.height - pathHeight - Padding);
        if (drives != null)
        {
            var rectDrives = rectItems.LeftPartPixels(drivesWidth);
            DoDrives(rectDrives);
        }

        var rectFiles = rectItems.RightPartPixels(filesWidth);
        DoFiles(rectFiles);
    }

    private void DoPath(Rect rect)
    {
        //Widgets.DrawBox(rect);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        var labelWidth = Text.CalcSize("RimFlix_CurrentDirectoryLabel".Translate()).x + Padding;
        Widgets.Label(rect.LeftPartPixels(labelWidth), "RimFlix_CurrentDirectoryLabel".Translate());
        var rightRect = rect.RightPartPixels(rect.width - labelWidth);
        var refreshRect = rightRect.RightPartPixels(rightRect.height);
        if (Widgets.ButtonImage(refreshRect, refreshTex, Color.gray, GenUI.SubtleMouseoverColor))
        {
            dirInfoDirty = true;
            soundAppear.PlayOneShotOnCamera();
        }

        // Using Color.gray for button changes default GUI color, so we need to change it back
        // to white
        GUI.color = Color.white;
        currentPath = Widgets.TextField(rightRect.LeftPartPixels(rightRect.width - refreshRect.width - Padding), currentPath, int.MaxValue, pathValidator);
    }

    private void DoDrives(Rect rect)
    {
        //Widgets.DrawBox(rect);
        Widgets.DrawBoxSolid(rect, drivesBackgroundColor);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        float buttonHeight = 24;
        var rectView = new Rect(0, 0, rect.width - ScrollBarWidth, buttonHeight * drives.Count());
        Widgets.BeginScrollView(rect, ref drivesScrollPos, rectView);
        var index = 0;
        foreach (var drive in drives)
        {
            var rectButton = new Rect(rectView.x, rectView.y + index * buttonHeight, rectView.width + ScrollBarWidth, buttonHeight);
            if (Widgets.ButtonText(rectButton, $" {drive}", false, false, true))
            {
                currentPath = drive;
                dirInfoDirty = true;
                soundAmbient.PlayOneShotOnCamera();
            }

            if (drive == Path.GetPathRoot(currentPath))
                Widgets.DrawHighlightSelected(rectButton);
            else
                Widgets.DrawHighlightIfMouseover(rectButton);

            index++;
        }

        Widgets.EndScrollView();
    }

    private void UpdateDirInfo(string path)
    {
        if (!dirInfoDirty && path.Equals(lastPath))
        {
            return;
        }

        try
        {
            dirInfo = new DirectoryInfo(currentPath);

            dirs = from dir in dirInfo.GetDirectories()
                where (dir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden
                orderby dir.Name
                select dir;

            files = from file in dirInfo.GetFiles()
                where file.Name.ToLower().EndsWith(".jpg") || file.Name.ToLower().EndsWith(".png")
                where (file.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden
                orderby file.Name
                select file;
        }
        catch
        {
            dirs = Enumerable.Empty<DirectoryInfo>();
            files = Enumerable.Empty<FileInfo>();
        }

        fileCount = files.Count();
        dirCount = dirs.Count();
        lastPath = currentPath;
        RimFlixMod.Settings.lastPath = currentPath;
        dirInfoDirty = false;
    }

    private void DoFiles(Rect rect)
    {
        Widgets.DrawBoxSolid(rect, filesBackgroundColor);
        framesCount = 0;
        if (!Directory.Exists(currentPath))
        {
            return;
        }

        Text.Font = GameFont.Small;
        UpdateDirInfo(currentPath);
        var count = fileCount + dirCount;
        if (dirInfo.Parent != null)
        {
            count++;
        }

        var buttonHeight = Text.LineHeight;

        // Fix for directories with abnormally large file counts
        Rect rectView;
        if (fileCount > MaxFileCount)
        {
            rectView = new Rect(0, 0, rect.width - ScrollBarWidth, buttonHeight * dirCount + 1);
        }
        else
        {
            rectView = new Rect(0, 0, rect.width - ScrollBarWidth, buttonHeight * count);
        }

        Widgets.BeginScrollView(rect, ref filesScrollPos, rectView);
        var index = 0;

        // Parent directory
        var rectButton = new Rect(rectView.x, rectView.y + index * buttonHeight, rectView.width + ScrollBarWidth, buttonHeight);
        if (dirInfo.Parent != null)
        {
            //Rect rectButton = new Rect(rectView.x, rectView.y + index * buttonHeight, rectView.width + this.scrollBarWidth, buttonHeight);
            Widgets.DrawAltRect(rectButton);
            Widgets.DrawHighlightIfMouseover(rectButton);
            if (Widgets.ButtonText(rectButton, " ..", false, false, Color.white, true))
            {
                currentPath = dirInfo.Parent.FullName;
                dirInfoDirty = true;
            }

            rectButton.y += buttonHeight;
            index++;
        }

        // Directories
        foreach (var d in dirs)
        {
            //Rect rectButton = new Rect(rectView.x, rectView.y + index * buttonHeight, rectView.width + this.scrollBarWidth, buttonHeight);
            if (index % 2 == 0)
            {
                Widgets.DrawAltRect(rectButton);
            }

            Widgets.DrawHighlightIfMouseover(rectButton);
            if (Widgets.ButtonText(rectButton, $" {d.Name}", false, false, Color.white, true))
            {
                d.Refresh();
                if (d.Exists)
                {
                    currentPath = d.FullName;
                    dirInfoDirty = true;
                }
                else
                {
                    Find.WindowStack.Add(new Dialog_MessageBox($"{"RimFlix_DirNotFound".Translate()}: {d.FullName}"));
                    dirInfoDirty = true;
                }
            }

            rectButton.y += buttonHeight;
            index++;
        }

        // Files
        if (fileCount > MaxFileCount)
        {
            // Too many files to display
            Widgets.Label(new Rect(rectView.x, rectView.y + buttonHeight, rectView.width + ScrollBarWidth, buttonHeight),
                $"Too many files to display ({fileCount} files, max {MaxFileCount})");
        }
        else
        {
            foreach (var f in files)
            {
                framesCount++;
                //Rect rectButton = new Rect(rectView.x, rectView.y + index * buttonHeight, rectView.width + this.scrollBarWidth, buttonHeight);
                if (index % 2 == 0)
                {
                    Widgets.DrawAltRect(rectButton);
                }

                Widgets.DrawHighlightIfMouseover(rectButton);
                if (Widgets.ButtonText(rectButton, $" {f.Name}", false, false, filesTextColor, false))
                {
                    f.Refresh();
                    if (f.Exists)
                    {
                        Find.WindowStack.Add(new Dialog_Preview(f.FullName, f.Name));
                    }
                    else
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox($"{"RimFlix_FileNotFound".Translate()}: {f.FullName}"));
                        dirInfoDirty = true;
                    }
                }

                rectButton.y += buttonHeight;
                index++;
            }
        }

        Widgets.EndScrollView();
    }

    private void DoOptions(Rect rect)
    {
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        var y = rect.y;
        var x = rect.x + Padding;
        var width = rect.width - Padding;
        var isUserShow = currentUserShow == null;

        // Frame count
        string framesText = framesCount == 1 ? "RimFlix_FrameLabel".Translate(framesCount) : "RimFlix_FramesLabel".Translate(framesCount);
        Widgets.Label(new Rect(x, y, width, Text.LineHeight), framesText);
        y += Text.LineHeight + Padding / 2;
        Widgets.Label(new Rect(x, y, width, Text.LineHeight), "RimFlix_InfoLabel".Translate());
        y += Text.LineHeight + Padding * 4;

        // Show name
        Widgets.Label(new Rect(x, y, width, Text.LineHeight), "RimFlix_NameLabel".Translate());
        y += Text.LineHeight + Padding / 2;
        showName = Widgets.TextArea(new Rect(x, y, width, Text.LineHeight), showName, !isUserShow);
        y += Text.LineHeight + Padding * 4;

        // Time between frames
        Widgets.Label(new Rect(x, y, width, Text.LineHeight), "RimFlix_TimeLabel".Translate());
        y += Text.LineHeight + Padding / 2;
        var rectTimeInput = new Rect(x, y, width, Text.LineHeight);

        var textFieldRect = new Rect(rectTimeInput.LeftPartPixels(timeInputWidth));
        var buffer = timeValue.ToString(CultureInfo.InvariantCulture);
        if (isUserShow)
            Widgets.TextFieldNumeric(textFieldRect, ref timeValue, ref buffer, 1f, float.MaxValue - 1);
        else
            Widgets.TextArea(new Rect(textFieldRect), buffer, true);

        var buttonRect = new Rect(rectTimeInput.RightPartPixels(timeUnitWidth));
        if (Widgets.ButtonText(buttonRect, timeUnitLabels[(int)timeUnit], active: isUserShow))
            Find.WindowStack.Add(new FloatMenu(timeUnitMenu));
        y += Text.LineHeight;

        Widgets.Label(new Rect(x, y, width, Text.LineHeight), $"(1 {"RimFlix_TimeSecond".Translate()} = {60} {"RimFlix_TimeTicks".Translate()})");
        y += Text.LineHeight + Padding * 4;

        // Television types
        buttonRect = new Rect(rectTimeInput.RightPartPixels(configTvWidth))
        {
            y = y
        };
        if (Widgets.ButtonText(buttonRect, "RimFlix_ConfigureSupportedTv".Translate(), active: status.status.Count > 0))
        {
            var options = status.status
                .Select(kvp => (def: DefDatabase<ThingDef>.GetNamedSilentFail(kvp.Key), status: kvp.Value))
                .Where(tuple => tuple.def != null)
                .OrderBy(tuple => tuple.def.LabelCap.ToString())
                .Select(tuple =>
            {
                var (def, enabled) = tuple;
                var label = (enabled ? "RimFlix_SupportedTvEnabled" : "RimFlix_SupportedTvDisabled").Translate(def.LabelCap);

                void Toggle() => status.status[def.defName] = !status.status[def.defName];

                return new FloatMenuOption(label, Toggle, def);
            }).ToList();

            Find.WindowStack.Add(new FloatMenu(options));
        }

        y += Text.LineHeight;

        // Note
        if (isUserShow)
        {
            GUI.color = Color.gray;
            Widgets.Label(new Rect(x, y, width, Text.LineHeight * 3), "RimFlix_Note01".Translate());
            GUI.color = Color.white;
        }
    }

    private void DoButtons(Rect rect)
    {
        var cancelSize = Text.CalcSize("RimFlix_CancelButton".Translate());
        cancelSize.Set(cancelSize.x + 4 * Padding, cancelSize.y + 1 * Padding);
        var rectCancel = new Rect(rect.x + rect.width - cancelSize.x, rect.y, cancelSize.x, cancelSize.y);
        TooltipHandler.TipRegion(rectCancel, "RimFlix_CancelTooltip".Translate());
        if (Widgets.ButtonText(rectCancel, "RimFlix_CancelButton".Translate(), true, false))
        {
            Close(false);
        }

        var createSize = Text.CalcSize("RimFlix_AddShowButton".Translate());
        createSize.Set(createSize.x + 4 * Padding, createSize.y + 1 * Padding);
        var rectCreate = new Rect(rect.x + rect.width - cancelSize.x - createSize.x - Padding, rect.y, createSize.x, createSize.y);
        TooltipHandler.TipRegion(rectCreate, "RimFlix_AddShowTooltip2".Translate());
        string buttonLabel = currentShow == null ? "RimFlix_AddShowButton".Translate() : "RimFlix_EditShowButton".Translate();

        if (Widgets.ButtonText(rectCreate, buttonLabel, true, false))
        {
            if (AcceptShow())
            {
                Close();
            }
        }
    }

    public override void DoWindowContents(Rect rect)
    {
        var rectExplorer = rect.LeftPartPixels(drivesWidth + filesWidth);
        var rectRight = rect.RightPartPixels(OptionsWidth);
        var rectOptions = rectRight.TopPartPixels(rectRight.height - ButtonsHeight);
        var rectButtons = rectRight.BottomPartPixels(ButtonsHeight);

        if (currentUserShow != null || currentShow == null)
            DoExplorer(rectExplorer);
        DoOptions(rectOptions);
        DoButtons(rectButtons);
    }

    private bool AcceptShow()
    {
        // New show or editing existing user show
        if (currentShow == null || currentUserShow != null)
            return CreateShow();

        // Built-in show
        return true;
    }

    private bool CreateShow()
    {
        // Check if path contains images
        if (!Directory.Exists(currentPath))
        {
            Find.WindowStack.Add(new Dialog_MessageBox("RimFlix_DirNotFound".Translate(currentPath)));
            return false;
        }

        // Check if name exists
        if (showName.NullOrEmpty())
        {
            Find.WindowStack.Add(new Dialog_MessageBox("RimFlix_NoShowName".Translate()));
            return false;
        }

        // Create or modify user show
        var userShow = currentUserShow ?? new UserShowDef { defName = UserContent.GetUniqueId() };
        userShow.path = currentPath;
        userShow.label = showName;
        userShow.description = $"{"RimFlix_CustomDescPrefix".Translate(currentPath, DateTime.Now.ToString())}";
        userShow.secondsBetweenFrames = (timeUnit == TimeUnit.Tick) ? timeValue / 60f : timeValue;
        userShow.televisionDefStrings = status.status.Where(x => x.Value).Select(x => x.Key).ToList();

        // Load show assets and add to def database
        if (!UserContent.LoadUserShow(userShow))
        {
            Log.Message($"RimFlix: Unable to load assets for {userShow.defName} : {userShow.label}");
            Find.WindowStack.Add(new Dialog_MessageBox($"{"RimFlix_LoadError".Translate()}"));
            return false;
        }

        // Add show to settings
        if (currentShow == null)
            RimFlixMod.Settings.userShows.Add(userShow);

        // Mark shows as dirty
        RimFlixSettings.showUpdateTime = RimFlixSettings.TotalSeconds;

        // If editing a show, avoid requerying shows to keep sort order
        if (currentShow != null)
        {
            userShow.SortName = $"{"RimFlix_UserShowLabel".Translate()} : {userShow.label}";
            RimFlixMod.Settings.ShowUpdateTime = RimFlixSettings.showUpdateTime;
        }

        status.showDefName = userShow.defName;
        userShow.RecheckDisabled();
        RimFlixMod.Settings.showStatus.Add(status);

        return true;
    }

    public override void OnAcceptKeyPressed()
    {
        if (AcceptShow())
        {
            base.OnAcceptKeyPressed();
        }
    }
}

public enum TimeUnit
{
    Second = 0,
    Tick = 1,
}