using Endless.Apple.World;
using Foundation;
using UIKit;
namespace Endless.Apple.iOS;

internal sealed class DesktopHudArt(string root)
{
    private readonly EgfArchive archive=new(Path.Combine(root,"classic","gfx002.egf"));
    public UIImage Crop(int id,int x,int y,int width,int height)
    {
        var source=archive.Bitmap(id);var rgba=new byte[width*height*4];
        for(int row=0;row<height;row++)Buffer.BlockCopy(source.Rgba,((y+row)*source.Width+x)*4,rgba,row*width*4,width*4);
        using var data=NSData.FromArray(WorldRenderer.Png(new Raster(width,height,rgba)));
        return UIImage.LoadFromData(data)!;
    }
    public UIImage QuestIcon() => InsetIcon(66,433,28,13,.82);
    public UIImage InventoryIcon() => InsetIcon(69,334,20,12,.72);

    private UIImage InsetIcon(int left,int top,int width,int height,double brightness)
    {
        var source=archive.Bitmap(1);
        var pixels=new byte[width*height*4];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int from=((top+y)*source.Width+left+x)*4,to=(y*width+x)*4;
            Buffer.BlockCopy(source.Rgba,from,pixels,to,4);
            var r=pixels[to];var g=pixels[to+1];var b=pixels[to+2];
            // Classic plaque amber is background; black is the glyph's outline,
            // not the world-sprite transparency key. Preserve its full silhouette.
            pixels[to+3]=(byte)(Math.Abs(r-197)<28 && Math.Abs(g-139)<28 && Math.Abs(b-57)<28 ? 0 : 255);
            for(int c=0;c<3;c++)pixels[to+c]=(byte)(pixels[to+c]*brightness);
        }
        using var data=NSData.FromArray(WorldRenderer.Png(new Raster(width,height,pixels)));
        return UIImage.LoadFromData(data)!;
    }

    public UIImage Button()
    {
        // Original small amber navigation plaque; remove its bag glyph, retaining its perimeter.
        var source=archive.Bitmap(1);const int w=37,h=20;var pixels=new byte[w*h*4];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {var interior=x>=3&&x<34&&y>=3&&y<17;var sx=interior?7:x;var sy=interior?8:y;Buffer.BlockCopy(source.Rgba,((330+sy)*source.Width+60+sx)*4,pixels,(y*w+x)*4,4);}
        using var data=NSData.FromArray(WorldRenderer.Png(new Raster(w,h,pixels)));
        using var image=UIImage.LoadFromData(data)!;
        return image.CreateResizableImage(new UIEdgeInsets(5,8,5,8),UIImageResizingMode.Stretch);
    }
}
