using CoreGraphics;
using Foundation;
using UIKit;
using Endless.Apple.World;
namespace Endless.Apple.iOS;

/// <summary>One classic desktop scene, sliced at continuous source rows to fit portrait.</summary>
public sealed class DesktopLoginBackdrop : UIView
{
    private readonly UIImageView top=new(), middle=new(), bottom=new(), splash=new(), bottomWood=new();
    private readonly List<UIImage> images=[];
    private readonly List<UIImage> splashes=[];
    private int current;
    private readonly DesktopWoodFrame frame;
    public bool SelectionMode {get;set;}
    public nfloat ArtworkTop {get;set;}
    public DesktopLoginBackdrop(string root)
    {
        UserInteractionEnabled=false;
        splash.IsAccessibilityElement=true;splash.AccessibilityIdentifier="login-splash";splash.AccessibilityLabel="Endless Online illustration";
        var art=new DesktopLoginArt(root);
        // Consecutive source rows: no patch of unrelated landscape behind the logo.
        top.Image=art.Crop(30,330,0,100,8).CreateResizableImage(UIEdgeInsets.Zero,UIImageResizingMode.Tile);
        middle.Image=art.Crop(30,0,10,640,190);
        bottom.Image=art.Crop(30,0,200,640,270);
        foreach(var view in new[]{top,middle,bottom})
        {images.Add(view.Image!);view.ContentMode=UIViewContentMode.ScaleToFill;AddSubview(view);}
        var archive=new EgfArchive(Path.Combine(root,"classic","gfx001.egf"));
        for(int id=41;id<=44;id++)
        {
            var raster=archive.OptionalBitmap(id);if(raster==null)continue;
            var rgba=(byte[])raster.Rgba.Clone();
            for(int i=0;i<rgba.Length;i+=4)if(rgba[i]==0&&rgba[i+1]==0&&rgba[i+2]==0)rgba[i+3]=0;
            // Trim transparent source padding so visible art, rather than its canvas, is centered.
            int left=raster.Width,right=0,topEdge=raster.Height,bottomEdge=0;
            for(int y=0;y<raster.Height;y++)for(int x=0;x<raster.Width;x++)if(rgba[(y*raster.Width+x)*4+3]>0){left=Math.Min(left,x);right=Math.Max(right,x);topEdge=Math.Min(topEdge,y);bottomEdge=Math.Max(bottomEdge,y);}
            int width=right-left+1,height=bottomEdge-topEdge+1;var trimmed=new byte[width*height*4];
            for(int y=0;y<height;y++)Buffer.BlockCopy(rgba,((topEdge+y)*raster.Width+left)*4,trimmed,y*width*4,width*4);
            using var data=NSData.FromArray(WorldRenderer.Png(new Raster(width,height,trimmed)));
            var image=UIImage.LoadFromData(data)!;images.Add(image);splashes.Add(image);
        }
        current=Random.Shared.Next(splashes.Count);splash.Image=splashes[current];AddSubview(splash);
        // Exclude the painted wood from the stretched background. Draw its complete
        // band once, above both the illustration and side frame.
        bottomWood.Image=art.Crop(30,0,470,640,10);images.Add(bottomWood.Image!);
        frame=new DesktopWoodFrame(root);AddSubview(frame);AddSubview(bottomWood);
    }
    public override void LayoutSubviews()
    {
        base.LayoutSubviews();var scale=Bounds.Width/640;var cap=SafeAreaInsets.Top+10*scale;
        top.Frame=new CGRect(0,0,Bounds.Width,cap);
        middle.Frame=new CGRect(0,cap,Bounds.Width,190*scale);
        bottom.Frame=new CGRect(0,cap+190*scale,Bounds.Width,Math.Max(0,Bounds.Height-cap-190*scale));
        splash.ClipsToBounds=true;
        splash.ContentMode=UIViewContentMode.ScaleAspectFit;
        // Both scenes reserve an illustration region, entirely behind the enclosing wood.
        var maxHeight=Math.Max(0,Math.Min(Bounds.Height*(SelectionMode?.48:.43),Bounds.Height-ArtworkTop));
        var image=splash.Image!;var artScale=Math.Min((Bounds.Width-24)/image.Size.Width,maxHeight/image.Size.Height);
        var artWidth=image.Size.Width*artScale;var artHeight=image.Size.Height*artScale;
        splash.Frame=new CGRect((Bounds.Width-artWidth)/2,Bounds.Height-artHeight,artWidth,artHeight);
        var woodHeight=10*scale;
        bottomWood.Frame=new CGRect(0,Bounds.Height-woodHeight,Bounds.Width,woodHeight);
        frame.EdgeWidth=10*scale;
        frame.Frame=new CGRect(0,cap+190*scale,Bounds.Width,Bounds.Height-cap-190*scale);
        // Only side/bottom edges here; the background already owns its rounded top edge.
        frame.ShowsTop=false;

    }
    public void RefreshSplash(){current=(current+Random.Shared.Next(1,splashes.Count))%splashes.Count;splash.Image=splashes[current];}
    protected override void Dispose(bool disposing){if(disposing)foreach(var image in images)image.Dispose();base.Dispose(disposing);}
}
