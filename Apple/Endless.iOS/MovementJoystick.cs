using CoreGraphics;
using Foundation;
using UIKit;
namespace Endless.Apple.iOS;

/// <summary>Isometric X-pad. Screen diagonals match the four world axes.</summary>
public sealed class MovementJoystick : UIView
{
    private readonly HudButton[] buttons=new HudButton[4];
    private readonly Action<int> move;
    private NSTimer? timer;
    private int? direction;
    private int submittedSteps;
    private long lastStep=long.MinValue/2;
    private bool canMove;
    public bool CanMove
    {
        get=>canMove;
        set {var ready=value&&!canMove;canMove=value;if(ready)Step();}
    }
    public MovementJoystick(UIColor wood, UIImage panel, Action<int> move)
    {
        this.move=move; AccessibilityLabel="Movement pad";
        for(int i=0;i<4;i++)
        {
            int d=i;
            var b=new HudButton { BackgroundColor=wood, ExclusiveTouch=true };
            b.SetBackgroundImage(panel,UIControlState.Normal);
            var arrow=new DirectionArrow(i){UserInteractionEnabled=false,Opaque=false,BackgroundColor=UIColor.Clear,Tag=601};
            b.AddSubview(arrow);
            b.SetTitleColor(UIColor.FromRGB(245,226,177),UIControlState.Normal);
            b.TitleLabel!.Font=UIFont.BoldSystemFontOfSize(30)!;
            b.Layer.BorderWidth=2;b.Layer.BorderColor=UIColor.FromRGB(137,100,48).CGColor;
            b.Layer.CornerRadius=0;b.ClipsToBounds=true;
            b.AccessibilityLabel=new[]{"Move southwest","Move northwest","Move northeast","Move southeast"}[i];
            b.TouchDown+=(_,_)=>{Stop();direction=d;Step();timer=NSTimer.CreateRepeatingTimer(TimeSpan.FromMilliseconds(25),_=>Step());NSRunLoop.Main.AddTimer(timer,NSRunLoopMode.Common);};
            b.TouchUpInside+=(_,_)=>Stop();b.TouchUpOutside+=(_,_)=>Stop();b.TouchCancel+=(_,_)=>Stop();b.TouchDragExit+=(_,_)=>Stop();
            buttons[i]=b;AddSubview(b);
        }
    }
    private void Step()
    {
        var now=Environment.TickCount64;
        if(CanMove && direction is {} value && now-lastStep>=250)
        {lastStep=now;submittedSteps++;move(value);}
    }
    public void Stop(){direction=null;timer?.Invalidate();timer?.Dispose();timer=null;}
    public override void LayoutSubviews()
    {
        base.LayoutSubviews();var size=(Bounds.Width-8)/2;
        for(int i=0;i<4;i++)
        {
            buttons[i].Frame=new CGRect(i>=2?size+8:0,i is 0 or 3?size+8:0,size,size);
            buttons[i].ViewWithTag(601)!.Frame=new CGRect(12,12,size-24,size-24);
        }
    }
#if DEBUG
    public async Task VerifyHoldAsync()
    {
        Stop();CanMove=true;
        var before=submittedSteps;
        buttons[0].SendActionForControlEvents(UIControlEvent.TouchDown);
        await Task.Delay(1100);
        var count=submittedSteps-before;
        if(count<2 || count>5)throw new InvalidOperationException("Movement hold cadence out of bounds.");
        CanMove=false;
        var pending=submittedSteps;
        await Task.Delay(300);
        if(submittedSteps!=pending)throw new InvalidOperationException("Movement bypassed pending confirmation.");
        buttons[0].SendActionForControlEvents(UIControlEvent.TouchUpInside);
        CanMove=true;
        await Task.Delay(300);
        if(submittedSteps!=pending)throw new InvalidOperationException("Movement continued after release.");
        Stop();
        Console.WriteLine("Movement hold checks passed: bounded repeat, confirmation gate, release cancellation.");
    }
#endif
    public override void WillMoveToWindow(UIWindow? window){if(window==null)Stop();base.WillMoveToWindow(window);}
    protected override void Dispose(bool disposing){if(disposing)Stop();base.Dispose(disposing);}
}
