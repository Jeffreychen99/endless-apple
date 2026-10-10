using CoreGraphics;
using UIKit;
using Endless.Apple.Protocol;
using Endless.Apple.World;
namespace Endless.Apple.iOS;

internal sealed class CharacterCarousel : UIView
{
    private readonly RechargedCharacter[] characters;
    private readonly DesktopLoginArt art;
    private readonly WorldArtwork artwork;
    private readonly Action<int> select;
    private readonly HudButton previous=new(),next=new();
    private readonly UILabel position=new(){TextAlignment=UITextAlignment.Center,Font=EoTypography.Body(13.75f),TextColor=UIColor.FromRGB(65,43,23)};
    private readonly UIImage buttonImage;
    private CharacterCard? card;
    private int index;
    private bool pending;
    public bool Pending {set{pending=value;RefreshEnabled();}}
    public CharacterCarousel(IEnumerable<RechargedCharacter> characters,DesktopLoginArt art,WorldArtwork artwork,Action<int> select)
    {
        this.characters=characters.ToArray();this.art=art;this.artwork=artwork;this.select=select;
        buttonImage=art.Crop(19,0,0,260,12).CreateResizableImage(new UIEdgeInsets(3,4,3,4));
        foreach(var button in new[]{previous,next}){button.SetBackgroundImage(buttonImage,UIControlState.Normal);AddSubview(button);}
        previous.AddSubview(new Arrow(false));next.AddSubview(new Arrow(true));
        previous.AccessibilityLabel="Previous character";next.AccessibilityLabel="Next character";
        previous.TouchUpInside+=(_,_)=>Change(-1);next.TouchUpInside+=(_,_)=>Change(1);
        AddSubview(position);HeightAnchor.ConstraintEqualTo(230).Active=true;
        ShowCharacter();
    }
    private void Change(int delta){if(pending||characters.Length<2)return;index=(index+delta+characters.Length)%characters.Length;ShowCharacter();}
    private void ShowCharacter()
    {
        card?.RemoveFromSuperview();card?.Dispose();card=null;
        if(characters.Length>0)
        {
            var selected=characters[index];
            card=new CharacterCard(selected,art,artwork,()=>{if(!pending)select(selected.Id);});AddSubview(card);
        }
        position.Text=characters.Length==0?"No characters":$"{index+1} / {characters.Length}";
        RefreshEnabled();SetNeedsLayout();
    }
    private void RefreshEnabled(){previous.Enabled=next.Enabled=!pending&&characters.Length>1;if(card!=null)card.Pending=pending;}
    public override void LayoutSubviews()
    {
        base.LayoutSubviews();var width=Math.Min(276,Bounds.Width-8);var height=width*123/276;
        if(card!=null)card.Frame=new CGRect((Bounds.Width-width)/2,38,width,height);
        previous.Frame=new CGRect(Bounds.Width/2-88,46+height,44,44);next.Frame=new CGRect(Bounds.Width/2+44,previous.Frame.Y,44,44);
        foreach(var button in new[]{previous,next})button.Subviews.OfType<Arrow>().Single().Frame=new CGRect(12,12,20,20);
        position.Frame=new CGRect(Bounds.Width/2-40,57+height,80,22);
    }
#if DEBUG
    public void VerifyNavigation()
    {
        var start=index;Change(1);if(characters.Length>1&&index==start)throw new Exception("Character navigation did not advance.");
        Change(-1);if(index!=start)throw new Exception("Character navigation did not return.");
        Pending=true;Change(1);if(index!=start)throw new Exception("Pending selection allowed navigation.");Pending=false;
        var old=Frame;
        foreach(var width in new[]{272,327,354}){Frame=new CGRect(0,0,width,230);LayoutSubviews();if(card!=null&&(previous.Frame.IntersectsWith(card.Frame)||next.Frame.IntersectsWith(card.Frame)||card.Frame.Bottom>230))throw new Exception("Character controls overlap.");}
        Frame=old;SetNeedsLayout();Console.WriteLine("Carousel checks passed: selection navigation, pending gate, separated controls at three widths.");
    }
#endif
    protected override void Dispose(bool disposing){if(disposing){card?.Dispose();buttonImage.Dispose();}base.Dispose(disposing);}
    private sealed class Arrow: UIView
    {
        private readonly bool right;
        public Arrow(bool right){this.right=right;Opaque=false;BackgroundColor=UIColor.Clear;UserInteractionEnabled=false;}
        public override void Draw(CGRect rect)
        {
            using var context=UIGraphics.GetCurrentContext();if(context==null)return;context.SetShouldAntialias(false);
            context.SetFillColor(UIColor.FromRGB(241,213,144).CGColor);
            context.MoveTo(right?rect.Left:rect.Right,rect.Top);context.AddLineToPoint(right?rect.Right:rect.Left,rect.GetMidY());context.AddLineToPoint(right?rect.Left:rect.Right,rect.Bottom);context.ClosePath();context.FillPath();
        }
    }
}
