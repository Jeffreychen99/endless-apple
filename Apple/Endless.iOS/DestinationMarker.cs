using CoreGraphics;
using UIKit;
namespace Endless.Apple.iOS;

/// <summary>Quiet gold tile brackets; UI feedback, not an invented desktop asset.</summary>
public sealed class DestinationMarker : UIView
{
    public DestinationMarker()
    {
        UserInteractionEnabled=false;Opaque=false;BackgroundColor=UIColor.Clear;Hidden=true;
        ContentMode=UIViewContentMode.Redraw;
    }
    public override void Draw(CGRect rect)
    {
        using var context=UIGraphics.GetCurrentContext();
        if(context is null)return;
        var w=Bounds.Width;var h=Bounds.Height;
        CGPoint[] corners=[new(w/2,2),new(w-2,h/2),new(w/2,h-2),new(2,h/2)];
        context.SetStrokeColor(UIColor.FromRGBA(242,211,115,215).CGColor);
        context.SetLineWidth(1.5f);
        for(int i=0;i<4;i++)
        {
            var c=corners[i];var prev=corners[(i+3)%4];var next=corners[(i+1)%4];
            context.MoveTo(c.X+(prev.X-c.X)*.3f,c.Y+(prev.Y-c.Y)*.3f);
            context.AddLineToPoint(c.X,c.Y);
            context.AddLineToPoint(c.X+(next.X-c.X)*.3f,c.Y+(next.Y-c.Y)*.3f);
        }
        context.StrokePath();
    }
}
