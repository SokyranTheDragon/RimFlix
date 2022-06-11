using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimFlix;

internal class Dialog_AddShow : Window
{
    private RimFlixSettings settings;
    private RimFlixMod mod;
    private UserShowDef currentUserShow;

    // Widget sizes
    private float padding = 8;

    private float scrollBarWidth = 16;
    private float drivesWidth = 80;
    private float filesWidth = 370;
    private float optionsWidth = 250;
    private float pathHeight = Text.LineHeight;
    private float buttonsHeight = 32;
    private float timeInputWidth;
    private float timeUnitWidth;

    // File explorer panel
    private string[] drives;

    private string currentPath;
    private string lastPath = "";
    private Regex pathValidator;
    private Texture2D refreshTex;
    private bool dirInfoDirty = true;
    private DirectoryInfo dirInfo;
    private IEnumerable<DirectoryInfo> dirs;
    private IEnumerable<FileInfo> files;
    private Color drivesBackgroundColor = new(0.17f, 0.18f, 0.19f);
    private Color filesBackgroundColor = new(0.08f, 0.09f, 0.11f);
    private Color filesTextColor = new(0.6f, 0.6f, 0.6f);
    private Vector2 drivesScrollPos = Vector2.zero;
    private Vector2 filesScrollPos = Vector2.zero;

    // Make abnormally large directories manageable
    private int fileCount = 0;

    private int dirCount = 0;
    private int maxFileCount = 1000;

    // Options panel
    private int framesCount;

    private string showName;
    private float timeValue;
    private TimeUnit timeUnit = TimeUnit.Second;
    private string[] timeUnitLabels;
    private List<FloatMenuOption> timeUnitMenu;
    private bool playTube;
    private bool playFlat;
    private bool playMega;
    private bool playUltra;

    public Dialog_AddShow(UserShowDef userShow = null, RimFlixMod mod = null)
    {
        // Window properties
        doCloseX = true;
        doCloseButton = false;
        forcePause = true;
        absorbInputAroundWindow = true;

        // Initialize object
        settings = LoadedModManager.GetMod<RimFlixMod>().GetSettings<RimFlixSettings>();
        refreshTex = ContentFinder<Texture2D>.Get("UI/Buttons/Refresh", true);
        var v1 = Text.CalcSize("RimFlix_TimeSeconds".Translate());
        var v2 = Text.CalcSize("RimFlix_TimeTicks".Translate());
        timeUnitWidth = Math.Max(v1.x, v2.x) + padding * 2;
        timeInputWidth = optionsWidth - timeUnitWidth - padding * 2;
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
        if (userShow != null)
        {
            showName = userShow.label;
            currentPath = userShow.path;
            timeValue = userShow.secondsBetweenFrames;
            playTube = userShow.televisionDefStrings.Contains("TubeTelevision");
            playFlat = userShow.televisionDefStrings.Contains("FlatscreenTelevision");
            playMega = userShow.televisionDefStrings.Contains("MegascreenTelevision");
            playUltra = userShow.televisionDefStrings.Contains("UltrascreenTV");
        }
        else
        {
            showName = "RimFlix_DefaultName".Translate();
            currentPath = Directory.Exists(settings.lastPath) ? settings.lastPath : settings.defaultPath;
            timeValue = 10;
            playTube = false;
            playFlat = false;
            playMega = false;
            playUltra = false;
        }
        currentUserShow = userShow;
        this.mod = mod;
    }

    public override Vector2 InitialSize => new(736f, 536f);

    private void DoExplorer(Rect rect)
    {
        var rectPath = rect.TopPartPixels(pathHeight);
        DoPath(rectPath);

        var rectItems = rect.BottomPartPixels(rect.height - pathHeight - padding);
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
        var labelWidth = Text.CalcSize("RimFlix_CurrentDirectoryLabel".Translate()).x + padding;
        Widgets.Label(rect.LeftPartPixels(labelWidth), "RimFlix_CurrentDirectoryLabel".Translate());
        var rightRect = rect.RightPartPixels(rect.width - labelWidth);
        var refreshRect = rightRect.RightPartPixels(rightRect.height);
        if (Widgets.ButtonImage(refreshRect, refreshTex, Color.gray, GenUI.SubtleMouseoverColor))
        {
            dirInfoDirty = true;
            soundAppear.PlayOneShotOnCamera(null);
        }
        // Using Color.gray for button changes default GUI color, so we need to change it back
        // to white
        GUI.color = Color.white;
        currentPath = Widgets.TextField(rightRect.LeftPartPixels(rightRect.width - refreshRect.width - padding), currentPath, int.MaxValue, pathValidator);
    }

    private void DoDrives(Rect rect)
    {
        //Widgets.DrawBox(rect);
        Widgets.DrawBoxSolid(rect, drivesBackgroundColor);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        float buttonHeight = 24;
        var rectView = new Rect(0, 0, rect.width - scrollBarWidth, buttonHeight * drives.Count());
        Widgets.BeginScrollView(rect, ref drivesScrollPos, rectView);
        var index = 0;
        foreach (var drive in drives)
        {
            var rectButton = new Rect(rectView.x, rectView.y + index * buttonHeight, rectView.width + scrollBarWidth, buttonHeight);
            if (Widgets.ButtonText(rectButton, $" {drive}", false, false, true))
            {
                currentPath = drive;
                dirInfoDirty = true;
                soundAmbient.PlayOneShotOnCamera(null);
            }
            if (drive == Path.GetPathRoot(currentPath))
            {
                Widgets.DrawHighlightSelected(rectButton);
            }
            else
            {
                Widgets.DrawHighlightIfMouseover(rectButton);
            }
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
        settings.lastPath = currentPath;
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
        if (fileCount > maxFileCount)
        {
            rectView = new Rect(0, 0, rect.width - scrollBarWidth, buttonHeight * dirCount + 1);
        }
        else
        {
            rectView = new Rect(0, 0, rect.width - scrollBarWidth, buttonHeight * count);
        }

        Widgets.BeginScrollView(rect, ref filesScrollPos, rectView);
        var index = 0;

        // Parent directory
        var rectButton = new Rect(rectView.x, rectView.y + index * buttonHeight, rectView.width + scrollBarWidth, buttonHeight);
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
        if (fileCount > maxFileCount)
        {
            // Too many files to display
            Widgets.Label(new Rect(rectView.x, rectView.y + buttonHeight, rectView.width + scrollBarWidth, buttonHeight),
                $"Too many files to display ({fileCount} files, max {maxFileCount})");
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
                if (Widgets.ButtonText(rectButton, $" {f.Name}", false, false, filesTextColor, true))
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
        var x = rect.x + padding;
        var width = rect.width - padding;

        // Frame count
        string framesText = framesCount == 1 ? "RimFlix_FrameLabel".Translate(framesCount) : "RimFlix_FramesLabel".Translate(framesCount);
        Widgets.Label(new Rect(x, y, width, Text.LineHeight), framesText);
        y += Text.LineHeight + padding / 2;
        Widgets.Label(new Rect(x, y, width, Text.LineHeight), "RimFlix_InfoLabel".Translate());
        y += Text.LineHeight + padding * 4;

        // Show name
        Widgets.Label(new Rect(x, y, width, Text.LineHeight), "RimFlix_NameLabel".Translate());
        y += Text.LineHeight + padding / 2;
        showName = Widgets.TextField(new Rect(x, y, width, Text.LineHeight), showName);
        y += Text.LineHeight + padding * 4;

        // Time between frames
        Widgets.Label(new Rect(x, y, width, Text.LineHeight), "RimFlix_TimeLabel".Translate());
        y += Text.LineHeight + padding / 2;
        var rectTimeInput = new Rect(x, y, width, Text.LineHeight);
        var buffer = timeValue.ToString();
        Widgets.TextFieldNumeric(new Rect(rectTimeInput.LeftPartPixels(timeInputWidth)), ref timeValue, ref buffer, 1f, float.MaxValue - 1);
        if (Widgets.ButtonText(new Rect(rectTimeInput.RightPartPixels(timeUnitWidth)), timeUnitLabels[(int)timeUnit]))
        {
            Find.WindowStack.Add(new FloatMenu(timeUnitMenu));
        }
        y += Text.LineHeight;
        Widgets.Label(new Rect(x, y, width, Text.LineHeight), string.Format("(1 {0} = {1} {2})", "RimFlix_TimeSecond".Translate(), 60, "RimFlix_TimeTicks".Translate()));
        y += Text.LineHeight + padding * 4;

        // Television types
        var checkX = x + padding;
        var checkWidth = width - padding * 2;
        Widgets.Label(new Rect(x, y, width, Text.LineHeight), "RimFlix_PlayLabel".Translate());
        y += Text.LineHeight + padding / 2;
        var rectTube = new Rect(checkX, y, checkWidth, Text.LineHeight);
        Widgets.DrawHighlightIfMouseover(rectTube);
        Widgets.CheckboxLabeled(rectTube, ThingDef.Named("TubeTelevision").LabelCap, ref playTube);
        y += Text.LineHeight;
        var rectFlat = new Rect(checkX, y, checkWidth, Text.LineHeight);
        Widgets.DrawHighlightIfMouseover(rectFlat);
        Widgets.CheckboxLabeled(rectFlat, ThingDef.Named("FlatscreenTelevision").LabelCap, ref playFlat);
        y += Text.LineHeight;
        var rectMega = new Rect(checkX, y, checkWidth, Text.LineHeight);
        Widgets.DrawHighlightIfMouseover(rectMega);
        Widgets.CheckboxLabeled(rectMega, ThingDef.Named("MegascreenTelevision").LabelCap, ref playMega);
        y += Text.LineHeight;
        var rectUltra = new Rect(checkX, y, checkWidth, Text.LineHeight);
        Widgets.DrawHighlightIfMouseover(rectUltra);
        Widgets.CheckboxLabeled(rectUltra, ThingDef.Named("UltrascreenTV").LabelCap, ref playUltra);
        y += Text.LineHeight + padding * 4;

        // Note
        GUI.color = Color.gray;
        Widgets.Label(new Rect(x, y, width, Text.LineHeight * 3), "RimFlix_Note01".Translate());
        GUI.color = Color.white;
    }

    private void DoButtons(Rect rect)
    {
        var cancelSize = Text.CalcSize("RimFlix_CancelButton".Translate());
        cancelSize.Set(cancelSize.x + 4 * padding, cancelSize.y + 1 * padding);
        var rectCancel = new Rect(rect.x + rect.width - cancelSize.x, rect.y, cancelSize.x, cancelSize.y);
        TooltipHandler.TipRegion(rectCancel, "RimFlix_CancelTooltip".Translate());
        if (Widgets.ButtonText(rectCancel, "RimFlix_CancelButton".Translate(), true, false, true))
        {
            Close(false);
        }

        var createSize = Text.CalcSize("RimFlix_AddShowButton".Translate());
        createSize.Set(createSize.x + 4 * padding, createSize.y + 1 * padding);
        var rectCreate = new Rect(rect.x + rect.width - cancelSize.x - createSize.x - padding, rect.y, createSize.x, createSize.y);
        TooltipHandler.TipRegion(rectCreate, "RimFlix_AddShowTooltip2".Translate());
        string buttonLabel = currentUserShow == null ? "RimFlix_AddShowButton".Translate() : "RimFlix_EditShowButton".Translate();
        if (Widgets.ButtonText(rectCreate, buttonLabel, true, false, true))
        {
            if (CreateShow())
            {
                Close(true);
            }
        }
    }

    public override void DoWindowContents(Rect rect)
    {
        var rectExplorer = rect.LeftPartPixels(drivesWidth + filesWidth);
        var rectRight = rect.RightPartPixels(optionsWidth);
        var rectOptions = rectRight.TopPartPixels(rectRight.height - buttonsHeight);
        var rectButtons = rectRight.BottomPartPixels(buttonsHeight);

        DoExplorer(rectExplorer);
        DoOptions(rectOptions);
        DoButtons(rectButtons);
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

        // Check if at least one television type is selected
        if (!(playTube || playFlat || playMega || playUltra))
        {
            Find.WindowStack.Add(new Dialog_MessageBox("RimFlix_NoTelevisionType".Translate()));
            return false;
        }

        // Create or modify user show
        var userShow = currentUserShow ?? new UserShowDef() { defName = UserContent.GetUniqueId() };
        userShow.path = currentPath;
        userShow.label = showName;
        userShow.description = $"{"RimFlix_CustomDescPrefix".Translate(currentPath, DateTime.Now.ToString())}";
        userShow.secondsBetweenFrames = (timeUnit == TimeUnit.Tick) ? timeValue / 60f : timeValue;
        userShow.televisionDefStrings = new List<string>();
        if (playTube)
        {
            userShow.televisionDefStrings.Add("TubeTelevision");
        }
        if (playFlat)
        {
            userShow.televisionDefStrings.Add("FlatscreenTelevision");
        }
        if (playMega)
        {
            userShow.televisionDefStrings.Add("MegascreenTelevision");
        }
        if (playUltra)
        {
            userShow.televisionDefStrings.Add("UltrascreenTV");
        }
        // Load show assets and add to def database

        if (!UserContent.LoadUserShow(userShow))
        {
            Log.Message($"RimFlix: Unable to load assets for {userShow.defName} : {userShow.label}");
            Find.WindowStack.Add(new Dialog_MessageBox($"{"RimFlix_LoadError".Translate()}"));
            return false;
        }
        // Add show to settings
        if (currentUserShow == null)
        {
            settings.UserShows.Add(userShow);
        }

        // Mark shows as dirty
        RimFlixSettings.showUpdateTime = RimFlixSettings.TotalSeconds;

        // If editing a show, avoid requerying shows to keep sort order
        if (currentUserShow != null)
        {
            userShow.SortName = $"{"RimFlix_UserShowLabel".Translate()} : {userShow.label}";
            mod.ShowUpdateTime = RimFlixSettings.showUpdateTime;
        }
        return true;
    }

    public override void OnAcceptKeyPressed()
    {
        if (CreateShow())
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