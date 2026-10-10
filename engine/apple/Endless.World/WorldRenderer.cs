using System.Buffers.Binary;
using System.IO.Compression;
using Endless.Apple.Protocol;

namespace Endless.Apple.World;

public static class WorldRenderer
{
    public static (WorldMap Map, Raster Image) LoadPreview(RechargedWelcome welcome, string assetRoot)
    {
        var bytes = File.ReadAllBytes(Path.Combine(assetRoot,"data","maps",$"{welcome.MapId:D5}.emf"));
        if (bytes.Length < 7 || bytes.Length != welcome.MapFileSize
            || EoWire.Number(bytes.AsSpan(3,2)) != welcome.MapRevisionLow
            || EoWire.Number(bytes.AsSpan(5,2)) != welcome.MapRevisionHigh)
            throw new InvalidDataException("Cached map differs from the server. A map download is required.");
        var map = WorldMap.Load(bytes);
        return (map,Render(map,assetRoot));
    }

    public static Raster Render(WorldMap map, string assetRoot, IReadOnlyCollection<WorldActor>? actors = null, WorldCamera? camera = null, WorldArtwork? artwork = null, IReadOnlyCollection<WorldNpc>? npcs = null, int animationFrame = 0, WorldMotion? motion = null, double seconds = 0)
    {
        artwork ??= new WorldArtwork(assetRoot);
        ScenePosition Position(bool npc,int id,int x,int y) => motion?.Position(npc,id,seconds) ?? new ScenePosition(x,y);
        var result = RenderTerrain(map,artwork.Archive(4),camera,animationFrame);
        var sceneCamera = camera ?? new WorldCamera((map.Width-1)/2.0,(map.Height-1)/2.0,result.Width,result.Height);
        // Current map order follows 0x462eee..0x4656b0, not the classic nine-layer order.
        // Archive IDs are verified at each official renderer's 0x6e9464 call.
        var archiveIds = new[] {4,25,5,6,7,8,4,7,9,9,7,10,8,7};
        var scenery = Enumerable.Range(1,13).Where(l => archiveIds[l] != 0)
            .SelectMany(l => map.Layers[l].Select(t => (Layer:l,Tile:t)))
            .OrderBy(e => e.Layer is 1 or 2 ? 0 : e.Layer is 11 or 12 or 13 ? 2 : 1)
            .ThenBy(e => e.Tile.X+e.Tile.Y).ThenBy(e => e.Tile.X).ThenBy(e => e.Layer);
        var elevations = map.Layers[3].ToDictionary(t => (t.X,t.Y),t => t.Height);
        int Elevation(int x,int y) => elevations.GetValueOrDefault((x,y));
        var entities = (actors ?? []).Select(a => (a.X,a.Y,Draw:(Action)(() => DrawActor(a))))
            .Concat((npcs ?? []).Select(n => (n.X,n.Y,Draw:(Action)(() => DrawNpc(n)))))
            .OrderBy(e => e.X+e.Y).ThenBy(e => e.X).ToList();
        void DrawNpc(WorldNpc npc)
        {
            var graphic = artwork.Npcs.Graphic(npc.DefinitionId);
            if (graphic is <= 0 or > 1600 || npc.Direction is < 0 or > 3) return;
            var back = npc.Direction is 1 or 2;
            var baseGraphic = (graphic-1)*40+1;
            var walking = motion?.WalkingFrame(true,npc.Id,seconds);
            // Official 0x6e7ab4: four walking frames, front +4 and rear +8.
            var sprite = artwork.Archive(24).OptionalBitmap(baseGraphic+(walking is {} frame ? (back ? 8 : 4)+frame : back ? 2 : 0))
                ?? artwork.Archive(24).OptionalBitmap(baseGraphic+(back ? 2 : 0));
            if (sprite == null) return;
            var position = Position(true,npc.Id,npc.X,npc.Y);
            var (x,y) = sceneCamera.Project(position.X,position.Y);
            Blit(result.Rgba,result.Width,result.Height,sprite,x-sprite.Width/2,y+16-sprite.Height-Elevation(npc.X,npc.Y),
                sprite.Width,sprite.Height,flip:npc.Direction is 2 or 3);
        }
        void DrawActor(WorldActor actor)
        {
            if (actor.X < 0 || actor.Y < 0 || actor.X > 249 || actor.Y > 249) return;
            var position = Position(false,actor.Id,actor.X,actor.Y);
            var (x,y) = sceneCamera.Project(position.X,position.Y);
            var attackAge=actor.AttackStartedMilliseconds is {} started ? Environment.TickCount64-started : -1;
            var layers=attackAge is >=0 and <400
                ? CharacterAppearance.Attacking(actor,(int)(attackAge/200),artwork.Items,
                    l => artwork.Archive(l.Archive).HasVisibleBitmap(l.Graphic))
                : motion?.WalkingFrame(false,actor.Id,seconds) is {} walkFrame
                    ? CharacterAppearance.Walking(actor,walkFrame,artwork.Items) : CharacterAppearance.Standing(actor,artwork.Items);
            if(attackAge is >=200 and <400 && WeaponSlash.Layer(actor,artwork.Items) is {} slash)
                layers=layers.Append(slash).ToArray();
            foreach (var layer in layers)
            {
                var sprite = artwork.Archive(layer.Archive).OptionalBitmap(layer.Graphic);
                if (sprite == null) continue;
                var width = layer.Width ?? sprite.Width;
                var height = layer.Height ?? sprite.Height;
                var left = layer.Flip ? -layer.Left-width : layer.Left;
                Blit(result.Rgba,result.Width,result.Height,sprite,x+left,y+16-58+layer.Top-Elevation(actor.X,actor.Y),
                    width,height,layer.SourceX,layer.SourceY,layer.Flip,layer.Archive is 18 or 19 ? 32*sprite.Width+1 : null,layer.Opacity);
            }
        }
        var entityIndex=0;
        foreach (var e in scenery)
        {
            if (e.Layer is not (1 or 2))
                while (entityIndex < entities.Count && (e.Layer is 11 or 12 or 13 || entities[entityIndex].X+entities[entityIndex].Y < e.Tile.X+e.Tile.Y))
                { entities[entityIndex++].Draw(); }
            if (e.Tile.Graphic == 0) continue;
            var id = archiveIds[e.Layer];
            var archive = artwork.Archive(id);
            var sprite = archive.Bitmap(e.Tile.Graphic);
            var (x,y) = sceneCamera.Project(e.Tile.X,e.Tile.Y);
            y -= 16;
            var frameWidth = (e.Layer is 8 or 9) && sprite.Width > 68 ? sprite.Width/4 : sprite.Width;
            var sourceX = 0;
            // Raised-tile sheets: official renderer 0x6faa78 uses 64-pixel frames.
            if (e.Layer == 3 && sprite.Width >= 128)
            {
                var count = sprite.Width >= 256 ? 4 : sprite.Width == 128 ? 2 : 1;
                if (count > 1) { frameWidth = 64; sourceX = Math.Min((int)((uint)animationFrame%4),count-1)*64; }
            }
            var left = x-frameWidth/2; var top = y+32-sprite.Height;
            if (e.Layer is 2 or 6) {left=x-32;top=y;frameWidth=Math.Min(64,sprite.Width);}
            if (e.Layer is 8 or 9) {left=x-32+(e.Layer==9 ? 32:0);top=y+32-sprite.Height;}
            if (e.Layer == 11) {left=x-32;top=y-64;}
            Blit(result.Rgba,result.Width,result.Height,sprite,left,top,frameWidth,sprite.Height,sourceX:sourceX,opacity:e.Layer == 1 ? 128 : 255);
        }
        while (entityIndex < entities.Count) entities[entityIndex++].Draw();
        return result;
    }

    public static Raster RenderTerrain(WorldMap map, EgfArchive terrain, WorldCamera? camera = null, int animationFrame = 0)
    {
        // Isometric projection: each tile spans 64 by 32 pixels.
        var width = camera?.Width ?? Math.Min(2048,(map.Width+map.Height)*32+64);
        var height = camera?.Height ?? Math.Min(1536,(map.Width+map.Height)*16+320);
        var pixels = new byte[width*height*4];
        for (var p = 0; p < pixels.Length; p+=4) { pixels[p]=17; pixels[p+1]=25; pixels[p+2]=32; pixels[p+3]=255; }
        var tiles = map.Layers[0].ToDictionary(t => (t.X,t.Y),t => t.Graphic);
        var centerX = (camera?.X ?? (map.Width-1)/2.0); var centerY = (camera?.Y ?? (map.Height-1)/2.0);
        for (var diagonal = 0; diagonal < map.Width+map.Height-1; diagonal++)
        for (var y = 0; y < map.Height; y++)
        {
            var x = diagonal-y; if (x < 0 || x >= map.Width) continue;
            var graphic = tiles.GetValueOrDefault((x,y),map.FillTile);
            if (graphic == 0) continue;
            var px = (int)(width/2.0 + ((x-centerX)-(y-centerY))*32)-32;
            var py = (int)(height/2.0 + ((x-centerX)+(y-centerY))*16)-16;
            if (px+64 <= 0 || py+32 <= 0 || px >= width || py >= height) continue;
            var tile = terrain.Bitmap(graphic);
            Blit(pixels,width,height,tile,px,py,sourceX:tile.Width == 256 ? (int)((uint)animationFrame%4)*64 : 0);
        }
        return new(width,height,pixels);
    }
    public static Raster Portrait(WorldActor actor, WorldArtwork artwork)
    {
        const int width=100, height=110;
        var pixels = new byte[width*height*4];
        foreach (var layer in CharacterAppearance.Standing(actor,artwork.Items))
        {
            var sprite = artwork.Archive(layer.Archive).OptionalBitmap(layer.Graphic);
            if (sprite == null) continue;
            var w=layer.Width ?? sprite.Width;
            var h=layer.Height ?? sprite.Height;
            var left=layer.Flip ? -layer.Left-w : layer.Left;
            Blit(pixels,width,height,sprite,width/2+left,30+layer.Top,w,h,
                layer.SourceX,layer.SourceY,layer.Flip,layer.Archive is 18 or 19 ? 32*sprite.Width+1 : null,layer.Opacity);
        }
        return new(width,height,pixels);
    }

    private static void Blit(byte[] target,int width,int height,Raster sprite,int left,int top,int drawWidth = 64,int drawHeight = 32,int sourceX = 0,int sourceY = 0,bool flip = false,int? colorKeyPixel = null,int opacity = 255)
    {
        if (left >= width || top >= height || left+drawWidth <= 0 || top+drawHeight <= 0) return;
        // Terrain resources can contain animation frames horizontally; start with frame zero.
        for (var y = 0; y < Math.Min(drawHeight,sprite.Height-sourceY); y++) for (var x = 0; x < Math.Min(drawWidth,sprite.Width-sourceX); x++)
        {
            var dx = left+x; var dy = top+y;
            if (dx < 0 || dy < 0 || dx >= width || dy >= height) continue;
            var src = ((y+sourceY)*sprite.Width+sourceX+(flip ? drawWidth-1-x : x))*4;
            if (sprite.Rgba[src+3] == 0) continue;
            if (colorKeyPixel is {} key && key >= 0 && key < sprite.Width*sprite.Height
                && sprite.Rgba.AsSpan(src,3).SequenceEqual(sprite.Rgba.AsSpan(key*4,3))) continue;
            var destination = (dy*width+dx)*4;
            if (opacity == 255) Buffer.BlockCopy(sprite.Rgba,src,target,destination,4);
            else
                for (var channel=0;channel<3;channel++)
                    target[destination+channel] = (byte)((sprite.Rgba[src+channel]*opacity+target[destination+channel]*(255-opacity)+127)/255);
        }
    }
    public static byte[] Png(Raster raster)
    {
        using var output = new MemoryStream();
        output.Write(new byte[] {137,80,78,71,13,10,26,10});
        void Chunk(string type, byte[] data)
        {
            var kind = System.Text.Encoding.ASCII.GetBytes(type);
            var length = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(length,data.Length); output.Write(length);
            output.Write(kind); output.Write(data);
            uint crc = 0xffffffff;
            foreach (var value in kind.Concat(data))
            {
                crc ^= value;
                for (var i=0;i<8;i++) crc = (crc>>1)^((crc&1)!=0 ? 0xedb88320u : 0);
            }
            BinaryPrimitives.WriteUInt32BigEndian(length,~crc); output.Write(length);
        }
        var header = new byte[13]; BinaryPrimitives.WriteInt32BigEndian(header,raster.Width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4),raster.Height); header[8]=8;header[9]=6;
        Chunk("IHDR",header);
        using var compressed = new MemoryStream();
        using (var z = new ZLibStream(compressed,CompressionLevel.Fastest,true))
            for (var y=0;y<raster.Height;y++) { z.WriteByte(0); z.Write(raster.Rgba,y*raster.Width*4,raster.Width*4); }
        Chunk("IDAT",compressed.ToArray()); Chunk("IEND",[]);return output.ToArray();
    }
}
