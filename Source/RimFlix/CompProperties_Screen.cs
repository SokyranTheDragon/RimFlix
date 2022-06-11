using System.Collections.Generic;
using Verse;

namespace RimFlix;

public class CompProperties_Screen : CompProperties
{
    public Values defaultValues = new();
    
    public CompProperties_Screen() => compClass = typeof(CompScreen);
    
    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (var configError in base.ConfigErrors(parentDef))
            yield return configError;

        if (defaultValues == null)
            yield return $"{nameof(defaultValues)} cannot be null!";
    }
}