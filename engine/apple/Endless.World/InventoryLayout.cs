namespace Endless.Apple.World;

public sealed record InventoryPlacement(int Index,int X,int Y,int Width,int Height);

/// <summary>Row-major packing for original multi-cell item footprints; quantity does not occupy extra cells.</summary>
public static class InventoryLayout
{
    public static IReadOnlyList<InventoryPlacement> Pack(IReadOnlyList<(int Width,int Height)> footprints,int columns)
    {
        if(columns<2||columns>16||footprints.Count>1024)throw new ArgumentOutOfRangeException(nameof(columns));
        var occupied=new HashSet<(int,int)>();var placements=new List<InventoryPlacement>();
        for(int i=0;i<footprints.Count;i++)
        {
            var (width,height)=footprints[i];
            if(width<1||width>2||height<1||height>4)throw new InvalidDataException("Unsupported inventory footprint.");
            bool done=false;
            for(int y=0;y<=i*4&&!done;y++)for(int x=0;x<=columns-width;x++)
            {
                bool free=true;
                for(int dy=0;dy<height&&free;dy++)for(int dx=0;dx<width;dx++)if(occupied.Contains((x+dx,y+dy))){free=false;break;}
                if(!free)continue;
                placements.Add(new(i,x,y,width,height));
                for(int dy=0;dy<height;dy++)for(int dx=0;dx<width;dx++)occupied.Add((x+dx,y+dy));
                done=true;break;
            }
        }
        return placements;
    }
}
