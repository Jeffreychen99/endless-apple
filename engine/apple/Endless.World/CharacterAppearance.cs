using Endless.Apple.Protocol;
using EndlessClient.Rendering.Sprites;

namespace Endless.Apple.World;

public sealed record CharacterLayer(int Archive, int Graphic, int Left, int Top,
    bool Flip, int SourceX = 0, int SourceY = 0, int? Width = null, int? Height = null, int Opacity = 255);

public static class CharacterAppearance
{
    // Body frame extraction: 0x6ea28c; equipment sequences: 0x5aab2f/0x5aab67.
    public static IReadOnlyList<CharacterLayer> Walking(WorldActor actor,int frame,ItemCatalog? catalog = null)
    {
        if (frame is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(frame));
        var back = actor.Direction is 1 or 2 ? 1 : 0;
        return Standing(actor,catalog).Select(layer => layer.Archive switch
        {
            11 => layer with { Graphic=2,Left=-13-actor.Gender,SourceX=actor.Gender*208+back*104+frame*26,
                SourceY=actor.Skin*61,Width=26,Height=61 },
            12 or 13 or 18 or 19 => layer with { Top=layer.Top+1 },
            14 or 15 or 16 or 17 or 20 or 21 => layer with { Graphic=layer.Graphic-back+2+back*4+frame },
            22 or 23 when catalog?.Find(actor.Equipment.ElementAtOrDefault(4))?.Subtype is not (5 or 6 or 7 or 13)
                => layer with { Graphic=layer.Graphic-back+2+back*4+frame },
            _ => layer
        }).ToArray();
    }

    // Original gfx011/3 is 192x928: two 24x58 frames per facing/gender,
    // sixteen skin rows. Crop: 0x6ea348; composed placements: 0x5ac3ea..0x5ac963.
    // Wind-up boots are standing; only the strike uses the +10 boot resource.
    public static IReadOnlyList<CharacterLayer> Attacking(WorldActor actor,int frame,ItemCatalog? catalog=null, Func<CharacterLayer,bool>? hasArtwork=null)
    {
        if(frame is <0 or >1)throw new ArgumentOutOfRangeException(nameof(frame));
        var back=actor.Direction is 1 or 2 ? 1 : 0;
        if(catalog?.Find(actor.Equipment.ElementAtOrDefault(3))?.Subtype is 1 or 2)
            return PreserveWeapon(RangedAttack(actor,frame,catalog),actor,catalog,hasArtwork);
        var layers=Standing(actor,catalog).Select(layer=>layer.Archive switch
        {
            11 => layer with { Graphic=3,Left=frame==0?-13-actor.Gender:-11+actor.Gender,SourceX=actor.Gender*96+back*48+frame*24,
                SourceY=actor.Skin*58,Width=24,Height=58 },
            14 or 15 => frame==0?layer:layer with { Graphic=layer.Graphic+10 },
            16 or 17 => layer with { Graphic=layer.Graphic-back+12+back*2+frame },
            20 or 21 => layer with { Graphic=layer.Graphic-back-1+(int)WeaponSpriteType.SwingFrame1+back*2+frame },
            22 or 23 when catalog?.Find(actor.Equipment.ElementAtOrDefault(4))?.Subtype is 5 or 6 or 7 or 13
                => layer with {Graphic=layer.Graphic+2,Left=layer.Left-(frame==1?2:0)},
            22 or 23 => layer with { Graphic=layer.Graphic-back+12+back*2+frame },
            12 or 13 or 18 or 19 when frame==1 => layer with {Left=layer.Left-2+(back==1?actor.Gender*2:0),Top=layer.Top+(back==1?1:5)- (back==0?actor.Gender:0)},
            _ => layer
        }).ToList();
        // A front-facing strike has TWO weapon parts: frame 14 behind the body
        // and frame 17 in front. Omitting the latter hides the grip/blade under armor.
        if(frame==1 && back==0)
        {
            var weapon=layers.FirstOrDefault(l=>l.Archive is 20 or 21);
            if(weapon is not null)layers.Add(weapon with {Graphic=weapon.Graphic-(int)WeaponSpriteType.SwingFrame2+(int)WeaponSpriteType.SwingFrame2Spec});
        }
        return PreserveWeapon(layers,actor,catalog,hasArtwork);
    }

    // Some current weapons (including Old Choppy) have completely blank strike
    // resources. Keep the last available held pose, without duplicating a valid
    // split foreground/background strike. This is a visual fallback, not a projectile.
    private static IReadOnlyList<CharacterLayer> PreserveWeapon(
        IReadOnlyList<CharacterLayer> layers, WorldActor actor, ItemCatalog? catalog,
        Func<CharacterLayer,bool>? hasArtwork)
    {
        if (hasArtwork is null) return layers;
        var weapon = layers.FirstOrDefault(l => l.Archive is 20 or 21);
        if (weapon is null || layers.Any(l => l.Archive is 20 or 21 && hasArtwork(l))) return layers;
        var standing = Standing(actor,catalog).First(l => l.Archive is 20 or 21);
        var back = actor.Direction is 1 or 2 ? 1 : 0;
        var windup = standing with { Graphic=standing.Graphic-back-1+(int)WeaponSpriteType.SwingFrame1+back*2 };
        var fallback = hasArtwork(windup) ? windup : standing;
        if (!hasArtwork(fallback)) return layers;
        var result = layers.Where(l => l.Archive is not (20 or 21)).ToList();
        result.Insert(0,fallback);
        return result;
    }

    private static IReadOnlyList<CharacterLayer> RangedAttack(WorldActor actor,int frame,ItemCatalog catalog)
    {
        // Desktop CharacterSpriteCalculator: shooting uses body 7, weapon 18/19,
        // armor 21/22 and back-item 3/4, followed by the standing recovery pose.
        if(frame==1)return Standing(actor,catalog);
        var back=actor.Direction is 1 or 2?1:0;
        return Standing(actor,catalog).Select(layer=>layer.Archive switch
        {
            11=>layer with {Graphic=7,SourceX=actor.Gender*50+back*25,SourceY=actor.Skin*58,Width=25,Height=58,Left=-18+back*2-actor.Gender*2,Top=1-back},
            14 or 15=>layer with {Graphic=layer.Graphic+10,Left=layer.Left-(back==0?5+actor.Gender:3+4*actor.Gender)},
            16 or 17=>layer with {Graphic=layer.Graphic+20,Left=layer.Left-(back==0?6:4),Top=layer.Top+1-back},
            20 or 21=>layer with {Graphic=layer.Graphic-1+(int)WeaponSpriteType.Shooting,Left=layer.Left-(back==0?6:4),Top=layer.Top+1-back},
            12 or 13 or 18 or 19=>layer with {Left=layer.Left-1-2*actor.Gender,Top=layer.Top+(back==0?actor.Gender:0)},
            22 or 23 when catalog.Find(actor.Equipment.ElementAtOrDefault(4))?.Subtype is 5 or 6 or 7 or 13
                =>layer with {Graphic=layer.Graphic+2,Left=layer.Left-1-2*actor.Gender},
            22 or 23=>layer with {Graphic=layer.Graphic-back+12+back*2},
            _=>layer
        }).ToArray();
    }

    // Current executable: resource arithmetic 0x5a88be..0x5a891c;
    // archive selection 0x5a8588; standing placement 0x5a9078..0x5a92c4.
    public static IReadOnlyList<CharacterLayer> Standing(WorldActor actor, ItemCatalog? catalog = null)
    {
        if (actor.Gender is < 0 or > 1 || actor.Skin is < 0 or > 15 || actor.Direction is < 0 or > 3) return [];
        var layers = new List<CharacterLayer>();
        var back = actor.Direction is 1 or 2 ? 1 : 0;
        var flip = actor.Direction is 2 or 3;
        var genderArchive = actor.Gender == 1 ? 0 : 1;
        ItemAppearance? Item(int slot) => catalog?.Find(actor.Equipment.ElementAtOrDefault(slot));
        int Graphic(int slot) => catalog == null ? actor.Equipment.ElementAtOrDefault(slot) : Item(slot)?.Graphic ?? 0;
        var hat = Graphic(2);
        var weapon = Graphic(3);
        var shield = Graphic(4);
        var hatType = Item(2)?.Subtype ?? 0;
        var shieldBack = Item(4)?.Subtype is 5 or 6 or 7 or 13;
        var hideHair = hatType == 9 || hat is >= 16 and <= 21 or 25 or 26 or 28 or 30 or 31 or 34 or 35 or 36 or 37 or 38 or 40 or 41 or 44 or 46 or 47;
        if (weapon is > 0 and <= 145) layers.Add(new(20+genderArchive,(weapon-1)*100+1+back,-43,-19-actor.Gender,flip));
        if (shieldBack && back==0 && shield is > 0 and <= 290)
            layers.Add(new(22+genderArchive,(shield-1)*50+1,-25,-19-actor.Gender,flip));
        var hair = actor.HairStyle > 0 && actor.HairStyle <= 300 && actor.HairColor is >= 0 and < 12
            ? (actor.HairStyle-1)*48+actor.HairColor*4+1+back*2 : 0;
        if (hair > 0 && !hideHair) layers.Add(new(12+genderArchive,hair,-15,-12-actor.Gender,flip));
        layers.Add(new(11,1,-9,0,flip,(actor.Gender*2+back)*18,actor.Skin*58,18,58));
        var boots = Graphic(0);
        var armor = Graphic(1);
        if (boots is > 0 and <= 300) layers.Add(new(14+genderArchive,(boots-1)*40+1+back,-17,37-actor.Gender,flip));
        if (armor is > 0 and <= 280) layers.Add(new(16+genderArchive,(armor-1)*50+1+back,-17,-12-actor.Gender,flip));
        if (hair > 0 && !hideHair) layers.Add(new(12+genderArchive,hair+1,-15,-12-actor.Gender,flip));
        if (hat is > 0 and <= 1450)
        {
            var hatLayer = new CharacterLayer(18+genderArchive,(hat-1)*10+1+back*2,-15,-15-actor.Gender,flip);
            // Face masks sit underneath the front hair layer.
            var frontHair = layers.FindLastIndex(l => l.Archive == 12+genderArchive);
            if (hatType == 8 && frontHair >= 0) layers.Insert(frontHair,hatLayer);
            else layers.Add(hatLayer);
        }
        if (shield is > 0 and <= 290 && (!shieldBack || back==1))
            layers.Add(new(22+genderArchive,(shield-1)*50+1+back,shieldBack ? -25 : -24,(shieldBack ? -19 : 15)-actor.Gender,flip));
        return layers;
    }
}
