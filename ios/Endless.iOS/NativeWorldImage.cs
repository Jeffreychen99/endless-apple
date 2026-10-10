using CoreGraphics;
using Foundation;
using System.Runtime.InteropServices;
using UIKit;
using Endless.Apple.World;
namespace Endless.Apple.iOS;

/// <summary>Present the opaque world buffer directly, without a PNG encode/decode per frame.</summary>
internal static class NativeWorldImage
{
    public static UIImage Create(Raster raster)
    {
        if(raster.Width<1||raster.Height<1||raster.Rgba.Length!=checked(raster.Width*raster.Height*4))
            throw new ArgumentException("Invalid world raster.",nameof(raster));
        using var colorSpace=CGColorSpace.CreateSrgb();
        using var provider=new CGDataProvider(raster.Rgba);
        using var image=new CGImage(raster.Width,raster.Height,8,32,raster.Width*4,colorSpace,
            CGBitmapFlags.ByteOrder32Big|CGBitmapFlags.NoneSkipLast,provider,null,false,CGColorRenderingIntent.Default);
        // UIImage retains CGImage; CGImage retains the provider backing the immutable buffer.
        return UIImage.FromImage(image);
    }
#if DEBUG
    public static void Verify()
    {
        byte[] expected=[255,0,0,255,0,255,0,255,0,0,255,255,255,255,255,255];
        var raster=new Raster(2,2,expected);
        using var native=Create(raster);
        using var png=NSData.FromArray(WorldRenderer.Png(raster));
        using var decoded=UIImage.LoadFromData(png)!;
        byte[] Pixels(UIImage image)
        {
            using var color=CGColorSpace.CreateSrgb();
            using var context=new CGBitmapContext(IntPtr.Zero,2,2,8,8,color,CGBitmapFlags.ByteOrder32Big|CGBitmapFlags.PremultipliedLast);
            context.DrawImage(new CGRect(0,0,2,2),image.CGImage!);
            var bytes=new byte[16];Marshal.Copy(context.Data,bytes,0,bytes.Length);return bytes;
        }
        if(!Pixels(native).SequenceEqual(Pixels(decoded)))throw new Exception("Native world image changed pixel colors or orientation.");
        var pixels=new byte[320*480*4];
        for(var i=0;i<pixels.Length;i+=4){pixels[i]=(byte)(i%251);pixels[i+1]=(byte)(i%127);pixels[i+2]=(byte)(i%67);pixels[i+3]=255;}
        var frame=new Raster(320,480,pixels);
        var clock=System.Diagnostics.Stopwatch.StartNew();
        for(var i=0;i<10;i++){using var data=NSData.FromArray(WorldRenderer.Png(frame));using var image=UIImage.LoadFromData(data);}
        var pngMs=clock.Elapsed.TotalMilliseconds;clock.Restart();
        for(var i=0;i<10;i++){using var image=Create(frame);}
        Console.WriteLine($"Native world image checks passed: equal RGB/orientation. Ten frame presentations: PNG {pngMs:F1} ms; native {clock.Elapsed.TotalMilliseconds:F1} ms.");
    }
#endif
}
