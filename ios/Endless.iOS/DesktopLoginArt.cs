using Endless.Apple.World;
using Foundation;
using UIKit;

namespace Endless.Apple.iOS;

internal sealed class DesktopLoginArt(string root)
{
    private readonly EgfArchive archive = new(Path.Combine(root,"classic","gfx001.egf"));
    public UIImage Crop(int graphic,int x,int y,int width,int height)
    {
        var source=archive.Bitmap(graphic);
        if(x<0||y<0||width<1||height<1||x+width>source.Width||y+height>source.Height)
            throw new ArgumentOutOfRangeException(nameof(graphic));
        var pixels=new byte[width*height*4];
        for(var row=0;row<height;row++)
            Buffer.BlockCopy(source.Rgba,((y+row)*source.Width+x)*4,pixels,row*width*4,width*4);
        using var data=NSData.FromArray(WorldRenderer.Png(new Raster(width,height,pixels)));
        return UIImage.LoadFromData(data)!;
    }
    public UIImage CleanPanel()
    {
        // Retain only the outer ten pixels of the original frame. Its inner painted bands
        // are not framing and must not be stretched over the credential wells.
        var source=archive.Bitmap(23);var hud=new EgfArchive(Path.Combine(root,"classic","gfx002.egf")).Bitmap(28);
        var pixels=(byte[])source.Rgba.Clone();
        for(int y=10;y<source.Height-10;y++)for(int x=10;x<source.Width-10;x++)
            Buffer.BlockCopy(hud.Rgba,(((y-10)%64+28)*hud.Width+(x-10)%64+12)*4,pixels,(y*source.Width+x)*4,4);
        using var data=NSData.FromArray(WorldRenderer.Png(new Raster(source.Width,source.Height,pixels)));
        using var image=UIImage.LoadFromData(data)!;
        return image.CreateResizableImage(new UIEdgeInsets(10,10,10,10),UIImageResizingMode.Stretch);
    }
    public UIImage BlankPlaque()
    {
        // gfx001/13: keep the original edge/screws and extend its adjacent blank grain
        // through the lettering area. Custom labels never stretch original word art.
        var source=archive.Bitmap(13);const int width=180,height=40;var pixels=new byte[width*height*4];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            var sx=x>=23&&x<156&&y>=5&&y<35?22:x;
            Buffer.BlockCopy(source.Rgba,(y*source.Width+sx)*4,pixels,(y*width+x)*4,4);
        }
        using var data=NSData.FromArray(WorldRenderer.Png(new Raster(width,height,pixels)));
        using var image=UIImage.LoadFromData(data)!;
        return image.CreateResizableImage(new UIEdgeInsets(5,23,5,24));
    }
    public UIImage Lettering(int graphic,int x,int y,int width,int height)
    {
        var source=archive.Bitmap(graphic);var pixels=new byte[width*height*4];
        for(int row=0;row<height;row++)for(int col=0;col<width;col++)
        {
            var src=((row+y)*source.Width+col+x)*4;var dst=(row*width+col)*4;
            var r=source.Rgba[src];var g=source.Rgba[src+1];var b=source.Rgba[src+2];
            // Classic caption is cream with near-black edging; discard its brown board.
            if((r>170&&g>150&&b>105)||(r<38&&g<38&&b<38))
                Buffer.BlockCopy(source.Rgba,src,pixels,dst,4);
        }
        using var data=NSData.FromArray(WorldRenderer.Png(new Raster(width,height,pixels)));
        return UIImage.LoadFromData(data)!;
    }
    public void Button(UIButton button,int graphic,int row,int width,int height)
    {
        button.SetTitle(null,UIControlState.Normal);button.SetAttributedTitle(null,UIControlState.Normal);
        // Keep bitmap lettering at its original aspect ratio; stretching the
        // background image distorted Connect/Credits across the phone width.
        button.SetBackgroundImage(null,UIControlState.Normal);
        button.SetBackgroundImage(null,UIControlState.Highlighted);
        button.SetImage(Crop(graphic,0,row*height,width,height).ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal),UIControlState.Normal);
        button.SetImage(Crop(graphic,width,row*height,width,height).ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal),UIControlState.Highlighted);
        if(button.ImageView is {} image){image.ContentMode=UIViewContentMode.ScaleAspectFit;image.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;image.Layer.MinificationFilter=CoreAnimation.CALayer.FilterNearest;}
        button.ImageView?.Layer.SetNeedsDisplay();
    }
}
