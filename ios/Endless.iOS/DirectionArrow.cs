using CoreGraphics;
using UIKit;
namespace Endless.Apple.iOS;

/// <summary>Custom pixel arrow in EO's cream/gold palette; original wood surrounds it.</summary>
public sealed class DirectionArrow(int direction) : UIView
{
    private static readonly string[] pixels=["001111111","000111111","000011111","000111111","001110111","011100011","111000001","110000000","100000000"];
    public override void Draw(CGRect rect)
    {
        using var context=UIGraphics.GetCurrentContext();
        if(context is null)return;
        context.SetShouldAntialias(false);
        var unit=Math.Max(1,Math.Floor(Math.Min((double)Bounds.Width,(double)Bounds.Height)/11));
        for(int pass=0;pass<2;pass++)
        {
            context.SetFillColor((pass==0?UIColor.FromRGB(66,43,19):UIColor.FromRGB(237,207,126)).CGColor);
            for(int y=0;y<9;y++)for(int x=0;x<9;x++)if(pixels[y][x]=='1')
            {
                var px=direction is 0 or 1?8-x:x;
                var py=direction is 0 or 3?8-y:y;
                context.FillRect(new CGRect((Bounds.Width-9*unit)/2+px*unit+(pass==0?unit:0),(Bounds.Height-9*unit)/2+py*unit+(pass==0?unit:0),unit,unit));
            }
        }
    }
}
