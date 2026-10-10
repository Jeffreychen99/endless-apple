using CoreGraphics;
using UIKit;
using Endless.Apple.Protocol;
namespace Endless.Apple.iOS;

/// <summary>Only server-derived values; a dash means the field has not been decoded.</summary>
internal sealed class CharacterStatsView:UIView
{
    private readonly UILabel[] labels,values;
    private static readonly string[] Names=["HP","TP","Stamina","Experience","Weight"];
    public CharacterStatsView()
    {
        BackgroundColor=UIColor.FromRGB(126,91,40);
        Layer.BorderWidth=2;Layer.BorderColor=UIColor.FromRGB(57,36,16).CGColor;
        labels=Names.Select(name=>Label(name,UITextAlignment.Left)).ToArray();
        values=Names.Select(_=>Label("—",UITextAlignment.Right)).ToArray();
        for(var i=0;i<Names.Length;i++){AddSubview(labels[i]);AddSubview(values[i]);values[i].AccessibilityIdentifier="stat-"+Names[i].ToLowerInvariant();}
    }
    private static UILabel Label(string text,UITextAlignment alignment)=>new()
    {Text=text,Font=EoTypography.Body(13.75f),TextColor=UIColor.FromRGB(245,225,178),TextAlignment=alignment};
    public void Update(WorldActor? player,WorldInventory? inventory)
    {
        var next=new[]{player==null?"—":$"{player.Hp} / {player.MaxHp}",
            player?.Tp is {} tp && player.MaxTp is {} max?$"{tp} / {max}":"—","—","—",
            inventory==null?"—":$"{inventory.Weight} / {inventory.Capacity}"};
        for(var i=0;i<next.Length;i++){values[i].Text=next[i];values[i].AccessibilityLabel=Names[i]+(next[i]=="—"?" unavailable":": "+next[i]);}
    }
    public override void LayoutSubviews()
    {
        base.LayoutSubviews();var row=Bounds.Height/Names.Length;
        for(var i=0;i<Names.Length;i++)
        {
            labels[i].Frame=new CGRect(8,i*row,Bounds.Width*.47-8,row);
            values[i].Frame=new CGRect(Bounds.Width*.47,i*row,Bounds.Width*.53-8,row);
        }
        SetNeedsDisplay();
    }
    public override void Draw(CGRect rect)
    {
        using var context=UIGraphics.GetCurrentContext();if(context==null)return;
        context.SetStrokeColor(UIColor.FromRGB(57,36,16).CGColor);context.SetLineWidth(1);
        for(var i=1;i<Names.Length;i++){var y=i*Bounds.Height/Names.Length;context.MoveTo(0,y);context.AddLineToPoint(Bounds.Width,y);}
        context.StrokePath();
    }
}
