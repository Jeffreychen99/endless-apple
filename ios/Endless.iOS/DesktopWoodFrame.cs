using CoreGraphics;
using UIKit;
namespace Endless.Apple.iOS;

/// <summary>Tiled perimeter pieces; never stretches a decorated panel through its center.</summary>
internal sealed class DesktopWoodFrame: UIView
{
    private readonly UIView left=new(),right=new(),top=new(),bottom=new();
    private readonly List<UIImage> images=[];
    private readonly UIImageView upperLeft=new(),upperRight=new(),roundedTopStrip=new();
    public bool RoundedTop {get;set;}
    public nfloat TopInset {get;set;}
    public nfloat CornerWidth {get;set;}=38;
    public nfloat EdgeWidth {get;set;}=10;
    private bool showsTop=true;
    public bool ShowsTop {set{showsTop=value;top.Hidden=!value;}}
    public DesktopWoodFrame(string root)
    {
        UserInteractionEnabled=false;BackgroundColor=UIColor.Clear;
        var art=new DesktopLoginArt(root);
        void Set(UIView view,int x,int y,int w,int h)
        {var image=art.Crop(30,x,y,w,h);images.Add(image);view.BackgroundColor=UIColor.FromPatternImage(image);AddSubview(view);}
        Set(left,0,100,10,160);Set(right,630,100,10,160);
        Set(top,330,0,100,8);Set(bottom,330,472,100,8);
        var source=new Endless.Apple.World.EgfArchive(Path.Combine(root,"classic","gfx002.egf")).Bitmap(1);
        UIImage Corner(int x){var pixels=new byte[64*54*4];for(int y=0;y<54;y++)for(int col=0;col<64;col++){var i=(y*64+col)*4;Buffer.BlockCopy(source.Rgba,(y*source.Width+x+col)*4,pixels,i,4);if(pixels[i]==0&&pixels[i+1]==0&&pixels[i+2]==0)pixels[i+3]=0;}using var data=Foundation.NSData.FromArray(Endless.Apple.World.WorldRenderer.Png(new Endless.Apple.World.Raster(64,54,pixels)));return UIImage.LoadFromData(data)!;}
        upperLeft.Image=Corner(0);upperRight.Image=Corner(576);
        var strip=new DesktopHudArt(root).Crop(1,64,0,512,8);images.Add(strip);roundedTopStrip.Image=strip;AddSubview(roundedTopStrip);
        foreach(var corner in new[]{upperLeft,upperRight}){images.Add(corner.Image!);corner.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;AddSubview(corner);}
    }
    public override void LayoutSubviews()
    {
        base.LayoutSubviews();left.Frame=new CGRect(0,0,EdgeWidth,Bounds.Height);right.Frame=new CGRect(Bounds.Width-EdgeWidth,0,EdgeWidth,Bounds.Height);
        upperLeft.Hidden=upperRight.Hidden=roundedTopStrip.Hidden=!RoundedTop;
        top.Hidden=RoundedTop||!showsTop;
        var cornerHeight=CornerWidth*54/64;
        roundedTopStrip.Frame=new CGRect(CornerWidth,TopInset,Bounds.Width-2*CornerWidth,8*CornerWidth/64);
        if(RoundedTop)
        {
            var start=TopInset+cornerHeight;
            left.Frame=new CGRect(0,start,EdgeWidth,Math.Max(0,Bounds.Height-start));
            right.Frame=new CGRect(Bounds.Width-EdgeWidth,start,EdgeWidth,Math.Max(0,Bounds.Height-start));
        }
        upperLeft.Frame=new CGRect(0,TopInset,CornerWidth,cornerHeight);
        upperRight.Frame=new CGRect(Bounds.Width-CornerWidth,TopInset,CornerWidth,cornerHeight);
        top.Frame=new CGRect(0,0,Bounds.Width,8);
        bottom.Frame=new CGRect(0,Bounds.Height-8,Bounds.Width,8);
    }
    protected override void Dispose(bool disposing){if(disposing)foreach(var image in images)image.Dispose();base.Dispose(disposing);}
}
