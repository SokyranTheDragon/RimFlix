using RimWorld;
using Verse;

namespace RimFlix;

internal class JobDriver_WatchRimFlix : JobDriver_WatchTelevision
{
    public override void WatchTickAction()
    {
        var thing = (Building)TargetA.Thing;

        var screen = thing.TryGetComp<CompScreen>();
        if (screen != null)
            screen.SleepTimer = 10;

        base.WatchTickAction();
    }
}