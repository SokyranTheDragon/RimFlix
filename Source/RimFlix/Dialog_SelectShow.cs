using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimFlix;

public class Dialog_SelectShow : Window
{
    private CompScreen screen;
    private Vector2 scrollPosition;
    private float buttonHeight = 32f;
    private float buttonMargin = 2f;

    public Dialog_SelectShow(CompScreen screen)
    {
        this.screen = screen;
        doCloseButton = true;
        doCloseX = true;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize
    {
        get
        {
            return new Vector2(340f, 580f);
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;
        var outRect = new Rect(inRect);
        outRect.yMin += 20f;
        outRect.yMax -= 40f;
        outRect.xMax -= 16f;
        var viewHeight = (buttonHeight + buttonMargin) * screen.Shows.Count + 80f;
        var viewWidth = viewHeight > outRect.height ? outRect.width - 32f : outRect.width - 16f;
        var viewRect = new Rect(0f, 0f, viewWidth, viewHeight);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);
        try
        {
            float y = 0;
            foreach (var show in screen.Shows)
            {
                var rect = new Rect(16f, y, viewRect.width, buttonHeight);
                TooltipHandler.TipRegion(rect, show.description);
                if (Widgets.ButtonText(rect, show.label, true, false, true))
                {
                    screen.ChangeShow(show);
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                    Close(true);
                }
                y += (buttonHeight + buttonMargin);
            }
        }
        finally
        {
            Widgets.EndScrollView();
        }
    }
}