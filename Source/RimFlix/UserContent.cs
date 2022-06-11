using RimWorld.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimFlix;

[StaticConstructorOnStartup]
public static class UserContent
{
    public static ModContentPack RimFlixMod => LoadedModManager.RunningMods.First(mod => mod.Name == "RimFlix");
    public static ModContentHolder<Texture2D> RimFlixContent => RimFlixMod.GetContentHolder<Texture2D>();

    static UserContent()
    {
        // Load user shows
        LoadUserShowDefs();

        // Set disabled status from settings
        ResolveDisabledShows();

        RimFlixSettings.showUpdateTime = RimFlixSettings.TotalSeconds;
        RimFlixSettings.screenUpdateTime = RimFlixSettings.TotalSeconds;

        //AllTexturesLoaded();
    }

    public static void ResolveDisabledShows()
    {
        var settings = LoadedModManager.GetMod<RimFlixMod>().GetSettings<RimFlixSettings>();
        var shows = DefDatabase<ShowDef>.AllDefs;

        foreach (var show in shows)
            show.disabled = settings.disabledShows?.Contains(show.defName) == true;
    }

    public static void LoadUserShowDefs()
    {
        var settings = LoadedModManager.GetMod<RimFlixMod>().GetSettings<RimFlixSettings>();
        var invalidShows = new List<UserShowDef>();
        var count = 0;

        foreach (var userShow in settings.userShows)
        {
            if (LoadUserShow(userShow))
            {
                count++;
            }
            else
            {
                invalidShows.Add(userShow);
                Log.Message($"Removed {userShow.defName} : {userShow.label} from list.");
            }
        }

        if (count != settings.userShows.Count)
            Log.Message($"RimFlix: {count} out of {settings.userShows.Count} UserShowDefs loaded.");

        settings.userShows.RemoveAll(show => invalidShows.Contains(show));
    }

    public static bool LoadUserShow(UserShowDef userShow) //, bool addToDefDatabase = true)
    {
        // Get images in path
        string[] filePaths;

        if (!Directory.Exists(userShow.path))
        {
            Log.Message($"RimFlix {userShow.defName} : {userShow.label}: Path <{userShow.path}> does not exist.");
            return false;
        }

        try
        {
            var dirInfo = new DirectoryInfo(userShow.path);
            filePaths = dirInfo.GetFiles()
                .Where(file => file.Name.ToLower().EndsWith(".jpg") || file.Name.ToLower().EndsWith(".png"))
                .Where(file => (file.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                .Select(file => file.FullName)
                .ToArray();
        }
        catch
        {
            Log.Message($"RimFlix {userShow.defName} : {userShow.label}: Error trying to load files from <{userShow.path}>.");
            return false;
        }

        if (!filePaths.Any())
        {
            Log.Message($"RimFlix {userShow.defName} : {userShow.label}: No images found in <{userShow.path}>.");
            // User may want to keep a show with an empty directory for future use.
            //return false;
        }

        // Load textures for images
        userShow.frames = new List<GraphicData>();
        foreach (var filePath in filePaths)
        {
            // RimWorld sets internalPath to filePath without extension This causes problems
            // with files that have same name but different extension (file.jpg, file.png)
            var internalPath = filePath.Replace('\\', '/');

            if (!RimFlixContent.contentList.ContainsKey(internalPath))
            {
                var path = Path.GetDirectoryName(filePath);
                var file = Path.GetFileName(filePath);
                var virtualFile = AbstractFilesystem.GetDirectory(path).GetFile(file);
                var loadedContentItem = ModContentLoader<Texture2D>.LoadItem(virtualFile);
                RimFlixContent.contentList.Add(internalPath, loadedContentItem.contentItem);
            }

            userShow.frames.Add(new GraphicData
            {
                texPath = internalPath,
                graphicClass = typeof(Graphic_Single)
            });
        }

        // Create televisionDefs list
        userShow.televisionDefs = new List<ThingDef>();
        foreach (var televisionDefString in userShow.televisionDefStrings)
            userShow.televisionDefs.Add(ThingDef.Named(televisionDefString));

        // Add user show to def database
        if (!DefDatabase<ShowDef>.AllDefs.Contains(userShow))
            DefDatabase<ShowDef>.Add(userShow);
        return true;
    }

    public static void RemoveUserShow(UserShowDef userShow)
    {
        // Remove from settings
        var settings = LoadedModManager.GetMod<RimFlixMod>().GetSettings<RimFlixSettings>();
        if (!settings.userShows.Contains(userShow))
        {
            Log.Message($"RimFlix: Could not find show {userShow.defName} : {userShow.label}");
            return;
        }

        settings.userShows.Remove(userShow);

        // We can't delete from DefDatabase, so mark as deleted
        userShow.deleted = true;

        // Do not remove graphic data if there are other shows with same path
        var twins = DefDatabase<UserShowDef>.AllDefs.Where(s => (s.path?.Equals(userShow.path) ?? false) && !s.deleted);
        if (!twins.Any())
        {
            foreach (var frame in userShow.frames)
                RimFlixContent.contentList.Remove(frame.texPath);
        }

        RimFlixSettings.showUpdateTime = RimFlixSettings.TotalSeconds;
    }

    public static void AllTexturesLoaded()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("There are " + RimFlixContent.contentList.Count + " graphics loaded for RimFlix.");
        var num = 0;
        foreach (var entry in RimFlixContent.contentList)
        {
            stringBuilder.AppendLine($"{num} - {entry.Key} : {entry.Value}");
            if (num % 50 == 49)
            {
                Log.Message(stringBuilder.ToString());
                stringBuilder = new StringBuilder();
            }

            num++;
        }

        Log.Message(stringBuilder.ToString());
    }

    public static string GetUniqueId()
    {
        // 31,536,000 seconds in a year 10 digit number string allows ids to remain sorted for
        // ~300 years
        var diffStr = Math.Floor(RimFlixSettings.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        return $"UserShow_{diffStr.PadLeft(10, '0')}";
    }
}