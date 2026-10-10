namespace Endless.Apple.World;

/// <summary>A bounded viewport centered on a world tile.</summary>
public sealed record WorldCamera
{
    public double X { get; }
    public double Y { get; }
    public int Width { get; }
    public int Height { get; }
    public (int X, int Y) Project(double x, double y)
        => ((int)(Width/2.0+((x-X)-(y-Y))*32), (int)(Height/2.0+((x-X)+(y-Y))*16));
    public (int X,int Y) TileAt(double screenX,double screenY)
    {
        var dx=(screenX-Width/2.0)/32;var dy=(screenY-Height/2.0)/16;
        return ((int)Math.Floor(X+(dx+dy)/2+.5),(int)Math.Floor(Y+(dy-dx)/2+.5));
    }

    public WorldCamera(double x, double y, int width = 640, int height = 480)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || width is < 1 or > 2048 || height is < 1 or > 1536)
            throw new ArgumentOutOfRangeException(nameof(width), "Invalid world viewport.");
        X = x; Y = y; Width = width; Height = height;
    }
}

/// <summary>Session-owned artwork cache. Use from one rendering worker at a time.</summary>
public sealed class WorldArtwork(string root)
{
    private readonly Dictionary<int,EgfArchive> archives = new();
    private ItemCatalog? items;
    private NpcCatalog? npcs;
    public NpcCatalog Npcs => npcs ??= NpcCatalog.Load(root);
    public ItemCatalog Items => items ??= ItemCatalog.Load(root);
    public EgfArchive Archive(int id)
    {
        if (!archives.TryGetValue(id,out var archive))
            archives[id] = archive = new EgfArchive(Path.Combine(root,"gfx",$"gfx{id:D3}.egf"));
        return archive;
    }
}
