using CoreGraphics;
using Foundation;
using UIKit;
using Endless.Apple.Protocol;
using Endless.Apple.World;
namespace Endless.Apple.iOS;

internal sealed class InventoryGridView:UIScrollView
{
    private readonly ItemCatalog catalog;
    private readonly EgfArchive archive;
    private readonly Board board=new();
    private readonly Dictionary<int,UIImage?> images=[];
    private readonly List<HudButton> buttons=[];
    private IReadOnlyList<InventoryEntry> entries=[];
    private nfloat laidOutWidth=-1;
    public event Action<string>? SelectionChanged;
    public InventoryGridView(string root,ItemCatalog catalog)
    {
        AccessibilityIdentifier="inventory-grid";
        this.catalog=catalog;archive=new EgfArchive(Path.Combine(root,"gfx","gfx026.egf"));
        BackgroundColor=UIColor.FromRGB(126,91,40);Layer.BorderWidth=2;Layer.BorderColor=UIColor.FromRGB(57,36,16).CGColor;
        AddSubview(board);AlwaysBounceVertical=true;ShowsVerticalScrollIndicator=true;
    }
    public void Update(IReadOnlyList<InventoryEntry> next)
    {
        entries=next;laidOutWidth=-1;
        foreach(var button in buttons){button.RemoveFromSuperview();button.Dispose();}buttons.Clear();
        for(int i=0;i<entries.Count;i++)
        {
            var entry=entries[i];var item=catalog.Find(entry.ItemId);var name=item?.Name??$"Item {entry.ItemId}";
            var button=new HudButton{AccessibilityIdentifier=$"inventory-item-{i}",AccessibilityLabel=$"{name}, quantity {entry.Amount}",BackgroundColor=UIColor.Clear};
            var graphic=item?.InventoryGraphic??0;
            if(!images.TryGetValue(graphic,out var image))
            {
                var raster=graphic>0?archive.OptionalBitmap(graphic*2):null;
                if(raster!=null)
                {
                    var rgba=(byte[])raster.Rgba.Clone();for(int pixel=0;pixel<rgba.Length;pixel+=4)
                        if(rgba[pixel]==0&&rgba[pixel+1]==0&&rgba[pixel+2]==0)rgba[pixel+3]=0;
                    using var data=NSData.FromArray(WorldRenderer.Png(new Raster(raster.Width,raster.Height,rgba)));
                    image=UIImage.LoadFromData(data);
                }
                images[graphic]=image;
            }
            if(image!=null){var icon=new UIImageView(image){ContentMode=UIViewContentMode.ScaleAspectFit,Tag=1,UserInteractionEnabled=false};icon.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;icon.Layer.MinificationFilter=CoreAnimation.CALayer.FilterNearest;button.AddSubview(icon);}
            else button.AddSubview(new UILabel{Text=name,Font=EoTypography.Body(11),TextColor=UIColor.FromRGB(245,225,178),Lines=2,TextAlignment=UITextAlignment.Center,Tag=1});
            if(entry.Amount>1)button.AddSubview(new UILabel{Text=entry.Amount.ToString("N0"),Font=EoTypography.Body(11),TextColor=UIColor.FromRGB(255,240,185),ShadowColor=UIColor.Black,ShadowOffset=new CGSize(1,1),TextAlignment=UITextAlignment.Right,Tag=2,UserInteractionEnabled=false});
            button.TouchUpInside+=(_,_)=>{
                foreach(var other in buttons)other.Layer.BorderWidth=0;
                button.Layer.BorderColor=UIColor.FromRGB(245,215,135).CGColor;button.Layer.BorderWidth=2;
                SelectionChanged?.Invoke($"{name} · ×{entry.Amount:N0}");
            };
            board.AddSubview(button);buttons.Add(button);
        }
        ContentOffset=CGPoint.Empty;SetNeedsLayout();
    }
    public override void LayoutSubviews()
    {
        base.LayoutSubviews();if(Bounds.Width<1||Bounds.Width==laidOutWidth)return;laidOutWidth=Bounds.Width;
        var columns=Math.Clamp((int)(Bounds.Width/44),2,12);var cell=Bounds.Width/columns;
        var footprints=entries.Select(e=>catalog.Find(e.ItemId)?.Footprint??(1,1)).ToArray();
        var placements=InventoryLayout.Pack(footprints,columns);var rows=Math.Max(3,placements.Count==0?0:placements.Max(p=>p.Y+p.Height));
        board.Frame=new CGRect(0,0,Bounds.Width,rows*cell);board.Cell=cell;board.SetNeedsDisplay();
        foreach(var p in placements)
        {
            var button=buttons[p.Index];button.Frame=new CGRect(p.X*cell+1,p.Y*cell+1,p.Width*cell-2,p.Height*cell-2);
            var content=button.ViewWithTag(1)!;
            if(content is UIImageView icon && icon.Image is {} image)
            {
                var scale=Math.Min(cell/26,Math.Min((button.Bounds.Width-6)/image.Size.Width,(button.Bounds.Height-6)/image.Size.Height));
                var width=image.Size.Width*scale;var height=image.Size.Height*scale;
                content.Frame=new CGRect((button.Bounds.Width-width)/2,(button.Bounds.Height-height)/2,width,height);
            }
            else content.Frame=new CGRect(3,3,button.Bounds.Width-6,button.Bounds.Height-6);
            if(button.ViewWithTag(2) is {} quantity)quantity.Frame=new CGRect(2,button.Bounds.Height-17,button.Bounds.Width-5,15);
        }
        ContentSize=board.Bounds.Size;
    }
    protected override void Dispose(bool disposing){if(disposing)foreach(var image in images.Values)image?.Dispose();base.Dispose(disposing);}
    private sealed class Board:UIView
    {
        public nfloat Cell {get;set;}
        public Board(){BackgroundColor=UIColor.FromRGB(126,91,40);Opaque=true;}
        public override void Draw(CGRect rect)
        {
            using var context=UIGraphics.GetCurrentContext();if(context==null||Cell<=0)return;
            context.SetStrokeColor(UIColor.FromRGB(58,38,18).CGColor);context.SetLineWidth(1);
            for(nfloat x=0;x<=Bounds.Width;x+=Cell){context.MoveTo(x,0);context.AddLineToPoint(x,Bounds.Height);}
            for(nfloat y=0;y<=Bounds.Height;y+=Cell){context.MoveTo(0,y);context.AddLineToPoint(Bounds.Width,y);}
            context.StrokePath();
        }
    }
}
