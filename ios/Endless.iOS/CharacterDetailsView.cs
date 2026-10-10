using CoreGraphics;
using Foundation;
using UIKit;
using Endless.Apple.Protocol;
using Endless.Apple.World;
namespace Endless.Apple.iOS;

/// <summary>Read-only equipped appearance; item actions require server confirmation.</summary>
internal sealed class CharacterDetailsView(string root,ItemCatalog catalog):UIView
{
    private readonly UIImageView portrait=new(){ContentMode=UIViewContentMode.ScaleAspectFit};
    private readonly UITextView equipment=new(){Editable=false,Selectable=false,BackgroundColor=UIColor.Clear,TextColor=UIColor.FromRGB(240,225,180),Font=EoTypography.Body(13.75f)};
    private WorldArtwork? artwork;
    private string? appearanceKey;
    public void Update(WorldActor? player)
    {
        if(portrait.Superview==null){AddSubview(portrait);AddSubview(equipment);portrait.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;equipment.AccessibilityIdentifier="paperdoll-equipment";}
        if(player==null)return;
        var key=$"{player.Gender}/{player.Skin}/{player.HairStyle}/{player.HairColor}/{string.Join(',',player.Equipment)}";
        if(key==appearanceKey)return;appearanceKey=key;
        artwork??=new WorldArtwork(root);
        var raster=WorldRenderer.Portrait(player with {Direction=0,AttackStartedMilliseconds=null},artwork);
        using var png=NSData.FromArray(WorldRenderer.Png(raster));var old=portrait.Image;portrait.Image=UIImage.LoadFromData(png);old?.Dispose();
        var names=new[]{"Boots","Armor","Hat","Weapon","Shield"};
        equipment.Text=string.Join("\n\n",names.Select((name,i)=>$"{name}: {(player.Equipment.ElementAtOrDefault(i)>0?catalog.Find(player.Equipment[i])?.Name??"Unknown item":"—")}"));
    }
    public override void LayoutSubviews(){base.LayoutSubviews();portrait.Frame=new CGRect(0,0,Math.Min(105,Bounds.Width*.38),Bounds.Height);equipment.Frame=new CGRect(portrait.Frame.Right+8,0,Bounds.Width-portrait.Frame.Right-8,Bounds.Height);}
    protected override void Dispose(bool disposing){if(disposing)portrait.Image?.Dispose();base.Dispose(disposing);}
}
