using UIKit;
using Foundation;
namespace Endless.Apple.iOS;

/// <summary>Native outline adaptation of the desktop's un-smoothed 11px bitmap atlas.</summary>
internal static class EoTypography
{
    public static UIFont Body(nfloat size)=>UIFont.FromName("EODesktopPixel-Regular",size)
        ?? throw new InvalidOperationException("The bundled EO desktop font is missing.");
    // Only for labels without a recovered desktop word sprite; original words take priority.
    public static NSAttributedString Title(string text,nfloat size)=>new(text,new UIStringAttributes
    {
        Font=UIFont.FromName("CarterOne",size) ?? UIFont.BoldSystemFontOfSize(size),
        ForegroundColor=UIColor.FromRGB(255,244,200),StrokeColor=UIColor.FromRGB(37,24,12),StrokeWidth=-3
    });
}
