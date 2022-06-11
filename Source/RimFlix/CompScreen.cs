using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimFlix;

// Defines a screen space on a TV
public class CompScreen : ThingComp
{
    // RimFlix settings
    private RimFlixSettings settings;

    private double screenUpdateTime;
    private double showUpdateTime;

    // Current show info
    private int showIndex = 0;

    private int showTicks = 0;
    private int frameIndex = 0;
    private int frameTicks = 0;

    // Current frame graphic
    private bool frameDirty = true;

    private Graphic frameGraphic;

    private Graphic FrameGraphic
    {
        get
        {
            if ((Show?.frames?.Count ?? 0) == 0)
                return null;

            if (frameDirty || screenUpdateTime < RimFlixSettings.screenUpdateTime)
            {
                var graphic = Show.frames[frameIndex % Show.frames.Count].Graphic;
                var frameSize = GetSize(graphic);
                frameGraphic = graphic.GetCopy(frameSize, null);
                screenUpdateTime = RimFlixSettings.screenUpdateTime;
                frameDirty = false;
            }

            return frameGraphic;
        }
    }

    // Available shows for this television
    private List<ShowDef> shows;

    public List<ShowDef> Shows
    {
        get
        {
            if (showUpdateTime < RimFlixSettings.showUpdateTime)
            {
                shows = (from show in DefDatabase<ShowDef>.AllDefs
                    where show.televisionDefs.Contains(parent.def) && !show.deleted && !show.disabled
                    select show).ToList();
                showUpdateTime = RimFlixSettings.showUpdateTime;
                frameDirty = true;
                ResolveShowDefName();
            }

            return shows;
        }
    }

    private string showDefName;
    private ShowDef show;

    private ShowDef Show
    {
        get
        {
            if ((Shows?.Count ?? 0) == 0)
            {
                return null;
            }

            show = Shows[showIndex % Shows.Count];
            showDefName = show.defName;
            return show;
        }
    }

    // Gizmo toggle and Scribe ref
    public bool allowPawn = true;

    // Power consumption tweaks
    private CompPowerTrader compPowerTrader;

    private float powerOutputOn;
    private float powerOutputOff;

    // Flag indicating whether or not TV is being watched, set by JobDriver on each tick
    public int SleepTimer { get; set; }

    public CompProperties_Screen Props => (CompProperties_Screen)props;

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);

        settings = RimFlixMod.Settings;
        compPowerTrader = parent.GetComp<CompPowerTrader>();

        if (!settings.defValues.ContainsKey(parent.def)) 
            settings.defValues[parent.def] = Props.defaultValues ?? new Values();

        if (compPowerTrader != null)
        {
            powerOutputOn = -1f * compPowerTrader.Props.basePowerConsumption * settings.powerConsumptionOn / 100f;
            powerOutputOff = -1f * compPowerTrader.Props.basePowerConsumption * settings.powerConsumptionOff / 100f;
        }
        
        screenUpdateTime = 0;
        showUpdateTime = 0;
    }

    /*private void ResolveShow()
    {
        if (this.show == null)
        {
            return;
        }
        int i = this.shows.IndexOf(this.show);
        if (i >= 0)
        {
            this.showIndex = i;
        }
    }
    */
    // Use Show.defName instead of Show in case user deletes show midgame
    private void ResolveShowDefName()
    {
        if (showDefName == null)
        {
            return;
        }

        var i = shows.FindIndex(s => s.defName.Equals(showDefName));
        if (i >= 0)
        {
            showIndex = i;
        }
    }

    private Vector2 GetSize(Graphic frame)
    {
        if (!RimFlixMod.Settings.defValues.TryGetValue(parent.def, out var values))
            RimFlixMod.Settings.defValues[parent.def] = values = Props.defaultValues ?? new Values();
        
        var screenSize = Vector2.Scale(values.Scale(parent.Rotation) ?? Vector2.zero, parent.Graphic.drawSize);
        var frameSize = new Vector2(frame.MatSingle.mainTexture.width, frame.MatSingle.mainTexture.height);
        var isWide = (frameSize.x / screenSize.x > frameSize.y / screenSize.y);

        return settings.drawType switch
        {
            // Stretch: resize image to fill frame, ignoring aspect ratio
            DrawType.Stretch => screenSize,
            // Fit: resize image to fit within frame while maintaining aspect ratio
            DrawType.Fit => isWide 
                ? new Vector2(screenSize.x, screenSize.x * frameSize.y / frameSize.x) 
                : new Vector2(screenSize.y * frameSize.x / frameSize.y, screenSize.y),
            // Fill: resize image to fill frame while maintaining aspect ratio (can be larger than parent)
            DrawType.Fill => isWide 
                ? new Vector2(screenSize.y * frameSize.x / frameSize.y, screenSize.y) 
                : new Vector2(screenSize.x, frameSize.y / frameSize.x * screenSize.x),
            _ => screenSize
        };
    }

    private Vector3 GetOffset()
    {
        // Altitude layers are 0.046875f For more info refer to `Verse.Altitudes` and `Verse.SectionLayer`
        const float y = 0.0234375f;
        
        if (!RimFlixMod.Settings.defValues.TryGetValue(parent.def, out var values))
            RimFlixMod.Settings.defValues[parent.def] = values = Props.defaultValues ?? new Values();

        var offset = values.Offset(parent.Rotation) ?? default;
        return new Vector3(offset.x, y, offset.y);
    }

    private bool IsPlaying()
    {
        // Is not supported rotation
        if (!Props.defaultValues.IsRotationSupported(parent.Rotation))
            return false;

        // No pawn watching, and PlayAlways is false
        if (SleepTimer == 0 && !settings.playAlways)
            return false;

        // No shows available, or show has no frames
        if ((Show?.frames?.Count ?? 0) == 0)
            return false;

        // Not powered
        if (!compPowerTrader.PowerOn)
            return false;

        return true;
    }

    public void ChangeShow(int i)
    {
        if (i >= 0 && i < Shows.Count)
        {
            showIndex = i;
            frameIndex = showTicks = frameTicks = 0;
            frameDirty = true;
        }
    }

    public void ChangeShow(ShowDef s) => ChangeShow(Shows.IndexOf(s));

    // Process show and frame ticks Should only be called when tv is playing (show exists and
    // has frames)
    private void RunShow()
    {
        if (SleepTimer > 0 && allowPawn && ++showTicks > settings.secondsBetweenShows.SecondsToTicks())
        {
            // Pawn changed show
            showIndex = (showIndex + 1) % Shows.Count;
            frameIndex = showTicks = frameTicks = 0;
            frameDirty = true;
        }
        else if (++frameTicks > Show.secondsBetweenFrames.SecondsToTicks())
        {
            // Frame change in current show
            frameIndex = (frameIndex + 1) % Show.frames.Count;
            frameTicks = 0;
            frameDirty = true;
        }
    }

    public override void PostDraw()
    {
        base.PostDraw();

        if (IsPlaying())
        {
            var drawPos = parent.DrawPos + GetOffset();
            FrameGraphic.Draw(drawPos, parent.Rotation.Opposite, parent);
        }

        if (!Find.TickManager.Paused)
            SleepTimer = SleepTimer > 0 ? SleepTimer - 1 : 0;
    }

    public override void CompTick()
    {
        if (IsPlaying())
        {
            RunShow();
            compPowerTrader.PowerOutput = powerOutputOn;
        }
        else
            compPowerTrader.PowerOutput = powerOutputOff;

        base.CompTick();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var c in base.CompGetGizmosExtra())
        {
            yield return c;
        }

        if ((Shows.Count > 0 && parent.Faction == Faction.OfPlayer) || Prefs.DevMode)
        {
            yield return new Command_Toggle
            {
                hotKey = KeyBindingDefOf.Misc6,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/AllowPawn"),
                defaultLabel = "RimFlix_AllowPawnLabel".Translate(),
                defaultDesc = "RimFlix_AllowPawnDesc".Translate(),
                isActive = () => allowPawn,
                toggleAction = () => allowPawn = !allowPawn
            };
            yield return new Command_Action
            {
                hotKey = KeyBindingDefOf.Misc7,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/SelectShow"),
                defaultLabel = "RimFlix_SelectShowLabel".Translate(),
                defaultDesc = "RimFlix_SelectShowDesc".Translate(),
                action = () => Find.WindowStack.Add(new Dialog_SelectShow(this))
            };
            yield return new Command_Action
            {
                hotKey = KeyBindingDefOf.Misc8,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/NextShow"),
                activateSound = SoundDefOf.Click,
                defaultLabel = "RimFlix_NextShowLabel".Translate(),
                defaultDesc = "RimFlix_NextShowDesc".Translate(),
                action = () => ChangeShow((showIndex + 1) % Shows.Count)
            };
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref showDefName, "RimFlix_ShowDefName");
        Scribe_Values.Look(ref frameIndex, "RimFlix_FrameIndex");
        Scribe_Values.Look(ref showTicks, "RimFlix_ShowTicks");
        Scribe_Values.Look(ref frameTicks, "RimFlix_FrameTicks");
        Scribe_Values.Look(ref allowPawn, "RimFlix_AllowPawn", true);
    }
}