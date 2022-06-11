using RimWorld;
using Verse;

namespace RimFlix
{
    internal class JobDriver_WatchRimFlix : JobDriver_WatchTelevision
    {
        public override void WatchTickAction()
        {
            Building thing = (Building)base.TargetA.Thing;

            RimFlix.CompScreen screen = thing.TryGetComp<RimFlix.CompScreen>();
            if (screen != null)
            {
                screen.SleepTimer = 10;
            }

            base.WatchTickAction();
        }
    }
}