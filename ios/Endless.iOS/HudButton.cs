using UIKit;
using CoreGraphics;
namespace Endless.Apple.iOS;

/// <summary>Darken all button artwork while held; cancellation restores its normal appearance.</summary>
public sealed class HudButton : UIButton
{
    private readonly UIView shade = new() { BackgroundColor=UIColor.FromRGBA(0,0,0,55),UserInteractionEnabled=false,Hidden=true };
    public HudButton():base(){AddSubview(shade);}
    public override bool Selected
    {get=>base.Selected;set{base.Selected=value;shade.Hidden=!(value||Highlighted);BringSubviewToFront(shade);}}
    public override bool Highlighted
    {
        get=>base.Highlighted;
        set {base.Highlighted=value;shade.Hidden=!(value||Selected);BringSubviewToFront(shade);}
    }
    public override void LayoutSubviews(){base.LayoutSubviews();shade.Frame=Bounds;BringSubviewToFront(shade);}
}
