using CoreGraphics;
using UIKit;
using Foundation;
using Endless.Apple.Protocol;
using Endless.Apple.World;
namespace Endless.Apple.iOS;

/// <summary>The complete desktop 276x123 card stays at one uniform scale.</summary>
internal sealed class CharacterCard : UIView
{
    private readonly UIImageView panel,portrait;
    private readonly UILabel name,level;
    private readonly HudButton login=new();
    private readonly List<UIImage> images=[];
    public bool Pending {set=>login.Enabled=!value;}
    public CharacterCard(RechargedCharacter character,DesktopLoginArt art,WorldArtwork artwork,Action select)
    {
        panel=new UIImageView(art.Crop(11,0,0,276,123)){ContentMode=UIViewContentMode.ScaleAspectFit};
        images.Add(panel.Image!);panel.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;AddSubview(panel);
        portrait=new UIImageView{ContentMode=UIViewContentMode.ScaleToFill};
        if(character.Appearance is {} appearance)
        {
            using var png=NSData.FromArray(WorldRenderer.Png(WorldRenderer.Portrait(appearance,artwork)));
            portrait.Image=UIImage.LoadFromData(png);images.Add(portrait.Image!);
        }
        portrait.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;AddSubview(portrait);
        name=new UILabel{Text=character.Name,TextAlignment=UITextAlignment.Center,TextColor=UIColor.FromRGB(237,221,176),AdjustsFontSizeToFitWidth=true,MinimumScaleFactor=.7f};AddSubview(name);
        level=new UILabel{Text=character.Level?.ToString()??"—",TextColor=UIColor.FromRGB(237,221,176)};AddSubview(level);
        AddSubview(login);art.Button(login,15,2,91,29);
        login.TouchUpInside+=(_,_)=>select();login.AccessibilityLabel=$"Login as {character.Name}";
    }
    public override void LayoutSubviews()
    {
        base.LayoutSubviews();var scale=Bounds.Width/276;panel.Frame=new CGRect(0,0,Bounds.Width,123*scale);
        // Same source-coordinate regions as desktop CharacterInfoPanel; no independent stretching.
        name.Frame=new CGRect(165*scale,26*scale,89*scale,22*scale);name.Font=EoTypography.Body(11*scale);
        level.Frame=new CGRect(29*scale,99*scale,40*scale,16*scale);level.Font=EoTypography.Body(11*scale);
        portrait.Frame=new CGRect(27*scale,8*scale,100*scale,110*scale);
        login.Frame=new CGRect(161*scale,62*scale,91*scale,44);
    }
    protected override void Dispose(bool disposing){if(disposing)foreach(var image in images)image.Dispose();base.Dispose(disposing);}
}
