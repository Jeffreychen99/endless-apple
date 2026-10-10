using Endless.Apple.Protocol;
namespace Endless.Apple.World;

/// <summary>Original slash sheet moved from legacy gfx002/40 to current gfx003/8.</summary>
public static class WeaponSlash
{
    // Legacy client WeaponMetadataProvider defaults for shared weapon graphics.
    // Unknown/current-only graphics remain unsupported rather than labeling ranged weapons melee.
    private static readonly Dictionary<int,int> rows = new()
    {
        [1]=3,
        [2]=0,
        [3]=2,
        [4]=0,
        [5]=2,
        [6]=0,
        [7]=0,
        [8]=0,
        [9]=3,
        [10]=0,
        [11]=0,
        [12]=2,
        [13]=0,
        [14]=2,
        [15]=0,
        [16]=0,
        [17]=0,
        [18]=2,
        [19]=2,
        [20]=0,
        [21]=2,
        [22]=0,
        [23]=0,
        [24]=1,
        [25]=0,
        [26]=0,
        [27]=2,
        [28]=2,
        [29]=0,
        [30]=2,
        [31]=2,
        [32]=2,
        [33]=5,
        [34]=5,
        [35]=1,
        [36]=4,
        [37]=4,
        [38]=5,
        [39]=7,
        [40]=7,
        [41]=1,
        [44]=8,
        [45]=0,
        [46]=5,
        [47]=1,
        [48]=0,
        [51]=5,
        [52]=1,
        [53]=1,
        [54]=1,
        [55]=7,
        [56]=1,
        [57]=2,
        [59]=0,
        [60]=0,
        [61]=0,
        [62]=0,
        [63]=0,
        [64]=0,
        [65]=3,
        [66]=1,
        [67]=2,
        [68]=3,
        [69]=0,
        [70]=0,
        [71]=0,
        [72]=0,
        [74]=0,
    };
    public static CharacterLayer? Layer(WorldActor actor,ItemCatalog? catalog)
    {
        if(catalog?.Find(actor.Equipment.ElementAtOrDefault(3))?.Subtype is 1 or 2)return null;
        var graphic=catalog?.Find(actor.Equipment.ElementAtOrDefault(3))?.Graphic
            ?? actor.Equipment.ElementAtOrDefault(3);
        if(actor.Direction is <0 or >3 || !rows.TryGetValue(graphic,out var row))return null;
        var (x,y)=actor.Direction switch {0=>(-30,4),1=>(-34,-9),2=>(-6,-9),_=>(-10,4)};
        return new CharacterLayer(3,8,x-9,y-actor.Gender,false,actor.Direction*58,row*58,58,58,96);
    }
}
