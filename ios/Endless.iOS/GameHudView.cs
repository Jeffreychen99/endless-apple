using CoreGraphics;
using Foundation;
using UIKit;
using Endless.Apple.World;
using Endless.Apple.Protocol;

namespace Endless.Apple.iOS;

/// <summary>Portrait-first game chrome. Panels never move the movement controls.</summary>
public sealed class GameHudView : UIView
{
    public UIImageView World { get; } = new() { ContentMode = UIViewContentMode.ScaleAspectFill, ClipsToBounds = true };
    private readonly UIView bars = new(), controls = new(), tabs = new(), drawer = new();
    public event Action? ViewportChanged;
    public event Action? AttackRequested;
    public event Action<double,double>? WorldTapped;
    private readonly UIImageView worldBorder = new() { UserInteractionEnabled=false };
    private CoreGraphics.CGSize lastViewport;
    private NSTimer? attackTimer;
    private readonly DestinationMarker destinationMarker = new();
    private WorldCamera? markerCamera;
    private (int X,int Y)? destination;
    public void SetDestination((int X,int Y)? tile, WorldCamera? camera)
    {
        destination=tile;markerCamera=camera;LayoutDestination();
    }
    private void LayoutDestination()
    {
        destinationMarker.Hidden=destination is null || markerCamera is null;
        if(destination is not {} tile || markerCamera is not {} camera)return;
        var point=camera.Project(tile.X,tile.Y);
        var sx=(double)World.Bounds.Width/camera.Width;
        var sy=(double)World.Bounds.Height/camera.Height;
        destinationMarker.Frame=new CGRect((point.X-32)*sx,(point.Y-16)*sy,64*sx,32*sy);
    }
    private void StopAttack()
    {
        attackTimer?.Invalidate();attackTimer?.Dispose();attackTimer=null;
    }
    private void BeginAttack(UIButton button)
    {
        StopAttack();
        if(!button.Enabled)return;
        RecenterCamera();
        AttackRequested?.Invoke();
        attackTimer=NSTimer.CreateRepeatingTimer(TimeSpan.FromMilliseconds(620),_=>
        {
            if(Window is null || !button.Enabled){StopAttack();return;}
            RecenterCamera();
            AttackRequested?.Invoke();
        });
        NSRunLoop.Main.AddTimer(attackTimer,NSRunLoopMode.Common);
    }
    public void StopControls(){StopAttack();joystick.Stop();}
    public override void WillMoveToWindow(UIWindow? window)
    {
        if(window is null)StopControls();
        base.WillMoveToWindow(window);
    }

    private double healthFraction, tpFraction;
    private readonly UIView tpClip = new() { ClipsToBounds=true };
    private readonly UIImageView tpFill = new();
    private readonly UIImageView sword = new() { ContentMode=UIViewContentMode.ScaleAspectFit, UserInteractionEnabled=false };
    private readonly UIImageView[] statusFrames = Enumerable.Range(0,4).Select(_=>new UIImageView()).ToArray();
    private readonly UIView healthClip = new() { ClipsToBounds=true };
    private readonly UIImageView healthHearts = new();
    private readonly UIButton paperdoll,quests;
    private double zoom=1;
    private readonly UIImageView headerWood=new(){UserInteractionEnabled=false};
    private readonly UIImageView footerWood=new(){UserInteractionEnabled=false};
    private readonly CameraPan cameraPan=new();
    public (double X,double Y) CameraOffset => (cameraPan.X,cameraPan.Y);
    private void UpdateCameraDescription() => World.AccessibilityValue=$"Zoom {zoom:F1}× · Camera {(cameraPan.X==0 && cameraPan.Y==0 ? "centered" : "panned")}";
    private void RecenterCamera() { cameraPan.Reset();UpdateCameraDescription();ViewportChanged?.Invoke(); }
    private readonly CharacterDetailsView characterDetails;
    private readonly UITextView players=new(){Editable=false,Selectable=false,BackgroundColor=UIColor.Clear,TextColor=UIColor.FromRGB(240,225,180),Font=EoTypography.Body(16.5f),AccessibilityIdentifier="nearby-players"};
    private readonly UILabel panelTitle = Text("Inventory",18);
    private readonly UILabel panelMessage = Text("Inventory is not available yet.",14);
    private readonly MovementJoystick joystick;
    private readonly List<UIImage> images = [];
    private readonly InventoryGridView inventory;
    private readonly CharacterStatsView stats=new();
    private WorldActor? currentPlayer;
    private string? selectedItem;
    private readonly UILabel weightLabel=Text("",13.75f);
    private readonly UITextField composer = new() { Placeholder="Chat is not connected", Font=EoTypography.Body(16.5f), Enabled=false, TextColor=UIColor.FromRGB(240,225,180), BackgroundColor=UIColor.FromRGB(40,30,20), BorderStyle=UITextBorderStyle.RoundedRect };
    private readonly UILabel channel = Text("Local",14);
    private WorldInventory? currentInventory;
    private bool inventoryUnsupported;
    private readonly ItemCatalog itemCatalog;
    private string selected = "Bag";
    // Menus always begin minimized; expansion is an explicit tap or preview option.
    private bool expanded = false;
    private readonly DesktopWoodFrame screenWood,controlWood;
    private readonly Action<int> move;
    public GameHudView(string assetRoot, Action<int> move, Action close)
    {
        this.move = move;
        AccessibilityViewIsModal=true;
        itemCatalog=ItemCatalog.Load(assetRoot);
        characterDetails=new CharacterDetailsView(assetRoot,itemCatalog);
        inventory=new InventoryGridView(assetRoot,itemCatalog);
        inventory.SelectionChanged+=value=>{selectedItem=value;RefreshPanel();};
        World.UserInteractionEnabled=true;
        World.IsAccessibilityElement=true;World.AccessibilityIdentifier="game-world";World.AccessibilityLabel="Game world";
        World.AddSubview(destinationMarker);
        var pinch=new UIPinchGestureRecognizer(g=>
        {
            if(g.State is UIGestureRecognizerState.Began or UIGestureRecognizerState.Changed)
            {
                zoom=Math.Clamp(zoom*(double)g.Scale,1,2.5);g.Scale=1;
                UpdateCameraDescription();ViewportChanged?.Invoke();
            }
        });
        World.AddGestureRecognizer(pinch);
        var pan=new UIPanGestureRecognizer(g=>
        {
            if(g.State is UIGestureRecognizerState.Began or UIGestureRecognizerState.Changed)
            {
                var delta=g.TranslationInView(World);var size=RenderSize;
                cameraPan.Drag((double)delta.X*size.Width/Math.Max(1,(double)World.Bounds.Width),
                    (double)delta.Y*size.Height/Math.Max(1,(double)World.Bounds.Height));
                g.SetTranslation(CGPoint.Empty,World);
                UpdateCameraDescription();ViewportChanged?.Invoke();
            }
        }){MaximumNumberOfTouches=1};
        World.AddGestureRecognizer(pan);
        UpdateCameraDescription();
        var tap=new UITapGestureRecognizer(t=>
        {var p=t.LocationInView(World);WorldTapped?.Invoke((double)(p.X/World.Bounds.Width),(double)(p.Y/World.Bounds.Height));});
        tap.RequireGestureRecognizerToFail(pinch);tap.RequireGestureRecognizerToFail(pan);World.AddGestureRecognizer(tap);
        BackgroundColor = UIColor.FromRGB(112,80,39);
        var art = new EgfArchive(Path.Combine(assetRoot,"gfx","gfx002.egf"));
        var classicArt = new EgfArchive(Path.Combine(assetRoot,"classic","gfx002.egf"));
        UIImage Crop(int id,int x,int y,int width,int height)
        {
            var raster=(id is 1 or 28 or 50 or 51 ? classicArt : art).Bitmap(id); var pixels=new byte[width*height*4];
            for(int row=0;row<height;row++) Buffer.BlockCopy(raster.Rgba,((y+row)*raster.Width+x)*4,pixels,row*width*4,width*4);
            if(id==1)
                for(int i=0;i<pixels.Length;i+=4)if(pixels[i]==0&&pixels[i+1]==0&&pixels[i+2]==0)pixels[i+3]=0;
            if(id==17){var r=pixels[0];var g=pixels[1];var b=pixels[2];for(int i=0;i<pixels.Length;i+=4)if(Math.Abs(pixels[i]-r)<20&&Math.Abs(pixels[i+1]-g)<20&&Math.Abs(pixels[i+2]-b)<20)pixels[i+3]=0;}
            // EO UI sheets use cyan as their transparent key.
            for(int i=0;i<pixels.Length;i+=4)
                if(pixels[i]==0 && pixels[i+1]>=240 && pixels[i+2]>=240) pixels[i+3]=0;
            using var data=NSData.FromArray(WorldRenderer.Png(new Raster(width,height,pixels)));
            var image=UIImage.LoadFromData(data)!; images.Add(image); return image;
        }
        // Original EO wood interior (gfx002, resource 128) repeated without stretching its grain.
        var wood=Crop(28,12,28,64,64);
        var hudArt=new DesktopHudArt(assetRoot);
        controls.BackgroundColor=UIColor.FromPatternImage(wood);
        BackgroundColor=UIColor.FromPatternImage(wood);
        foreach(var panel in new[]{tabs,drawer})
        { panel.BackgroundColor=UIColor.FromPatternImage(wood); panel.Layer.BorderWidth=2; panel.Layer.BorderColor=UIColor.FromRGB(137,113,56).CGColor; }
        // Preserve the desktop frame's corners and bevels while stretching its interior.
        var frame=new DesktopLoginArt(assetRoot).CleanPanel();
        images.Add(frame);
        foreach(var panel in new[]{drawer})
        {
            var backdrop=new UIImageView(frame) { Frame=panel.Bounds, AutoresizingMask=UIViewAutoresizing.FlexibleWidth|UIViewAutoresizing.FlexibleHeight, UserInteractionEnabled=false };
            backdrop.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;
            panel.AddSubview(backdrop);panel.Layer.BorderWidth=0;
        }
        // Broad original panel grain: never tile an eight-pixel perimeter across the status area.
        headerWood.Image=hudArt.Crop(28,12,8,440,84);images.Add(headerWood.Image!);
        headerWood.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;
        footerWood.Image=headerWood.Image;footerWood.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;
        AddSubview(headerWood);AddSubview(footerWood);
        foreach(var v in new UIView[]{World,bars,drawer,controls,tabs}) AddSubview(v);
        // The original gameplay viewport border (gfx002/1) is transparent inside.
        var viewportFrame=Crop(1,0,54,640,256).CreateResizableImage(new UIEdgeInsets(0,10,1,10));
        images.Add(viewportFrame);worldBorder.Image=viewportFrame;
        worldBorder.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;
        InsertSubviewAbove(worldBorder,World);
        World.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;
        // Updated official client: gfx002 resource 158 has empty/full status rows.
        for(int i=0;i<4;i++)
        {
            statusFrames[i].Image=Crop(58,i*110,0,107,13);
            statusFrames[i].Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;
            statusFrames[i].IsAccessibilityElement=true;
            statusFrames[i].AccessibilityLabel=new[]{"Health unavailable","TP unavailable","Stamina unavailable","Experience unavailable"}[i];
            bars.AddSubview(statusFrames[i]);
        }
        healthHearts.Image=Crop(58,0,14,107,13);
        healthHearts.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;
        healthClip.AddSubview(healthHearts);bars.AddSubview(healthClip);
        tpFill.Image=Crop(58,110,14,107,13);
        tpFill.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;
        tpClip.AddSubview(tpFill);bars.AddSubview(tpClip);
        var exit=Button("×",close); exit.AccessibilityLabel="Leave world";bars.AddSubview(exit);exit.Tag=99;
        var keyPanel=hudArt.Button();
        images.Add(keyPanel);
        joystick=new MovementJoystick(UIColor.FromPatternImage(wood),keyPanel,move);controls.AddSubview(joystick);
        var attack=Button("Attack",()=>{});attack.Enabled=false;attack.AccessibilityLabel="Attack";controls.AddSubview(attack);attack.Tag=10;
#if DEBUG
        if(NSProcessInfo.ProcessInfo.Arguments.Contains("--hud-preview"))
        {
            var requests=0;attack.AccessibilityValue="0";
            AttackRequested+=()=>attack.AccessibilityValue=(++requests).ToString();
        }
#endif
        attack.TouchDown+=(_,_)=>BeginAttack(attack);
        attack.TouchUpInside+=(_,_)=>StopAttack();
        attack.TouchUpOutside+=(_,_)=>StopAttack();
        attack.TouchCancel+=(_,_)=>StopAttack();
        attack.TouchDragExit+=(_,_)=>StopAttack();
        foreach(var title in new[]{"Bag","Chat","Skills","Stats","Players"})
        { var b=Button(title,()=>Select(title));b.AccessibilityIdentifier="hud-"+title.ToLowerInvariant();tabs.AddSubview(b); }
        // Original desktop action glyphs, gfx002 graphic 32 (14-pixel cells).
        UIImage Icon(int column,int row)=>Crop(32,column*14,row*14,14,14).ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
        var bagIcon=hudArt.InventoryIcon();images.Add(bagIcon);
        var icons=new[]{bagIcon,Icon(0,0),Icon(1,0),Icon(3,3),Icon(2,1)};
        for(int i=0;i<5;i++)
        {
            var button=(UIButton)tabs.Subviews[i];
            var glyph=new UIImageView(icons[i]) { ContentMode=UIViewContentMode.ScaleAspectFit,UserInteractionEnabled=false,Tag=501 };
            glyph.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;
            button.AddSubview(glyph);

        }
        // Compact original combat glyph in a normal amber thumb button.
        sword.Image=Crop(17,5,31,16,16);sword.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;
        attack.AddSubview(sword);
        paperdoll=Button("Paperdoll",()=>Select("Paperdoll"));paperdoll.AccessibilityIdentifier="hud-paperdoll";paperdoll.AccessibilityLabel="Paperdoll";controls.AddSubview(paperdoll);paperdoll.Tag=12;
        var dollImage=UIImage.FromFile(Path.Combine(NSBundle.MainBundle.BundlePath,"Assets","paperdoll-lettering-refined.png"))!;images.Add(dollImage);
        var dollIcon=new UIImageView(dollImage){ContentMode=UIViewContentMode.ScaleAspectFit,UserInteractionEnabled=false,Tag=501};
        dollIcon.Layer.MinificationFilter=CoreAnimation.CALayer.FilterLinear;paperdoll.AddSubview(dollIcon);
        quests=Button("Quests",()=>Select("Quests"));quests.AccessibilityIdentifier="hud-quests";quests.AccessibilityLabel="Quests";controls.AddSubview(quests);
        var questImage=hudArt.QuestIcon();images.Add(questImage);
        var questIcon=new UIImageView(questImage){ContentMode=UIViewContentMode.ScaleAspectFit,UserInteractionEnabled=false,Tag=501};quests.AddSubview(questIcon);
        questIcon.Layer.MagnificationFilter=CoreAnimation.CALayer.FilterNearest;
        drawer.AddSubview(characterDetails);drawer.AddSubview(players);
        panelMessage.AccessibilityIdentifier="inventory-selection";
        drawer.AddSubview(panelTitle);drawer.AddSubview(panelMessage);drawer.AddSubview(weightLabel);
        weightLabel.TextAlignment=UITextAlignment.Right;
        drawer.AddSubview(stats);drawer.AddSubview(inventory);drawer.AddSubview(composer);drawer.AddSubview(channel);
        var buttonArt=hudArt.Button();
        images.Add(buttonArt);
        foreach(var button in tabs.Subviews.Concat(controls.Subviews).OfType<UIButton>())
        { button.SetBackgroundImage(buttonArt,UIControlState.Normal);button.BackgroundColor=UIColor.Clear;button.Layer.BorderWidth=0; }
        foreach(var button in tabs.Subviews.OfType<UIButton>().Concat(new[]{attack,paperdoll,quests}))
        {
            button.AccessibilityLabel ??= button.CurrentTitle;
            button.SetTitle("",UIControlState.Normal);
            if(button.ImageView is {} icon)icon.Transform=CGAffineTransform.MakeScale(2,2);
        }

        screenWood=new DesktopWoodFrame(assetRoot){RoundedTop=true,TopInset=8,CornerWidth=64};controlWood=new DesktopWoodFrame(assetRoot);
        AddSubview(screenWood);controls.InsertSubview(controlWood,0);
        RefreshPanel();
    }
    private static UILabel Text(string value,nfloat size)=>new(){Text=value,TextColor=UIColor.FromRGB(240,225,180),Font=EoTypography.Body(size),Lines=0};
    private static UIButton Button(string title,Action action)
    {
        var b=new HudButton();b.SetTitle(title,UIControlState.Normal);b.SetTitleColor(UIColor.FromRGB(245,226,177),UIControlState.Normal);
        b.SetTitleColor(UIColor.FromRGB(139,127,100),UIControlState.Disabled);
        b.BackgroundColor=UIColor.FromRGBA(38,29,16,190);b.Layer.BorderWidth=1;b.Layer.BorderColor=UIColor.FromRGB(142,116,57).CGColor;
        b.TitleLabel!.Font=EoTypography.Body(16.5f);b.TouchUpInside+=(_,_)=>action();return b;
    }
    public (int Width,int Height) RenderSize
    {
        get { var width=(int)Math.Round(320/zoom);return(width,Math.Clamp((int)(width*World.Bounds.Height/Math.Max(1,(double)World.Bounds.Width)),64,960)); }
    }
    public void ShowInventory() { selected="Bag";expanded=true;RefreshPanel(); }
    private void Select(string title){expanded=selected==title?!expanded:true;selected=title;RefreshPanel();}
    private void RefreshPanel()
    {
        drawer.Hidden=!expanded;
        panelTitle.AttributedText=EoTypography.Title(selected=="Bag"?"Inventory":selected,18);
        panelMessage.Text=selected switch{"Bag"=>inventoryUnsupported ? "Inventory unavailable · response not supported" : currentInventory is {} bag ? selectedItem??(bag.Entries.Count==0?"Your bag is empty.":"Select an item") : "Waiting for inventory…","Chat"=>"Chat is not connected yet.","Skills"=>"Skills are not available yet.","Players"=>"Nearby players","Quests"=>"Quest journal is not connected yet.",_=>currentPlayer?.Name??"Character unavailable"};
        foreach(var button in tabs.Subviews.OfType<UIButton>().Concat(new[]{paperdoll,quests})){button.Selected=expanded&&button.AccessibilityIdentifier=="hud-"+selected.ToLowerInvariant();button.AccessibilityValue=button.Selected?"Open":"Closed";}
        characterDetails.Hidden=selected!="Paperdoll";players.Hidden=selected!="Players";
        if(selected=="Paperdoll")characterDetails.Update(currentPlayer);
        inventory.Hidden=weightLabel.Hidden=selected!="Bag";
        weightLabel.Text=currentInventory is {} contents?$"{contents.Weight} / {contents.Capacity}":"";
        composer.Hidden=channel.Hidden=selected!="Chat";
        stats.Hidden=selected!="Stats";stats.Update(currentPlayer,currentInventory);
        SetNeedsLayout();
    }
    public void UpdateInventory(WorldInventory? bag,bool unsupported=false)
    {
        if(inventoryUnsupported!=unsupported){inventoryUnsupported=unsupported;RefreshPanel();}
        if(ReferenceEquals(bag,currentInventory))return;
        currentInventory=bag;
        selectedItem=null;inventory.Update(bag?.Entries??[]);
        RefreshPanel();
    }
    public void UpdatePlayers(IEnumerable<WorldActor> actors)
    {
        var text=string.Join("\n\n",actors.OrderBy(a=>a.Name,StringComparer.OrdinalIgnoreCase).Select(a=>a.Name));
        if(players.Text!=text)players.Text=text.Length>0?text:"No players in view.";
    }
    public void Update(WorldActor? player,bool canMove,bool canAttack=true)
    {
        currentPlayer=player;stats.Update(player,currentInventory);
        if(selected=="Paperdoll"&&expanded)characterDetails.Update(player);
        if(selected=="Stats")panelMessage.Text=player?.Name??"Character unavailable";

        statusFrames[0].AccessibilityLabel=player is null?"Health unavailable":$"Health {player.Hp} of {player.MaxHp}";
        tpFraction=player is {Tp: {} tp, MaxTp: >0} ? Math.Clamp((double)tp/player.MaxTp.Value,0,1):0;
        statusFrames[1].AccessibilityLabel=player?.Tp is {} value ? $"TP {value} of {player.MaxTp}" : "TP unavailable";
        statusFrames[2].Alpha=statusFrames[3].Alpha=0.45f;
        healthFraction=player is { MaxHp: >0 } ? Math.Clamp((double)player.Hp/player.MaxHp,0,1):0;
        SetNeedsLayout();
        joystick.CanMove=canMove;
        if(controls.ViewWithTag(10) is UIButton attack)attack.Enabled=canAttack && player is {Hp: >0};
    }
    public override void LayoutSubviews()
    {
        base.LayoutSubviews();var safe=SafeAreaInsets;var w=Bounds.Width;var top=safe.Top+14;var bottom=Bounds.Height-safe.Bottom;
        bars.Frame=new CGRect(0,top,w,52);headerWood.Frame=new CGRect(0,0,w,top+52);
        for(int i=0;i<4;i++)statusFrames[i].Frame=new CGRect(6+(i%2)*(w-42)/2,(i/2)*25,(w-54)/2,22);
        var hp=statusFrames[0].Frame;
        healthClip.Frame=new CGRect(hp.X,hp.Y,healthFraction>0?hp.Width*(0.18+0.82*healthFraction):0,hp.Height);
        healthHearts.Frame=new CGRect(0,0,hp.Width,hp.Height);
        var tpFrame=statusFrames[1].Frame;
        tpClip.Frame=new CGRect(tpFrame.X,tpFrame.Y,tpFraction>0?tpFrame.Width*(0.18+0.82*tpFraction):0,tpFrame.Height);
        tpFill.Frame=new CGRect(0,0,tpFrame.Width,tpFrame.Height);
        bars.ViewWithTag(99)!.Frame=new CGRect(w-38,0,36,48);
        var tabY=bottom-60;var controlY=tabY-132;
        tabs.Frame=new CGRect(0,tabY,w,60);controls.Frame=new CGRect(0,controlY,w,132);
        screenWood.Frame=Bounds;controlWood.Frame=controls.Bounds;
        footerWood.Frame=new CGRect(0,bottom,w,Bounds.Height-bottom);
        for(int i=0;i<tabs.Subviews.Length;i++)
        {
            tabs.Subviews[i].Frame=new CGRect(i*w/5+3,4,w/5-6,52);
            if(tabs.Subviews[i].ViewWithTag(501) is {} glyph)glyph.Frame=new CGRect((w/5-6-32)/2,10,32,32);
        }
        joystick.Frame=new CGRect(12,12,108,108);
        var middleX=132.0;var attackWidth=Math.Clamp((double)w*.25,64,104);
        var attackX=(double)w-12-attackWidth;var middleWidth=attackX-middleX-10;
        paperdoll.Frame=new CGRect(middleX,12,middleWidth,50);
        var plaqueWidth=Math.Min(middleWidth-12,130);var plaqueHeight=32.0;
        paperdoll.ViewWithTag(501)!.Frame=new CGRect((middleWidth-plaqueWidth)/2,(50-plaqueHeight)/2,plaqueWidth,plaqueHeight);
        quests.Frame=new CGRect(middleX,70,middleWidth,50);
        quests.ViewWithTag(501)!.Frame=new CGRect((middleWidth-42)/2,12,42,26);
        controls.ViewWithTag(10)!.Frame=new CGRect(attackX,12,attackWidth,108);
        sword.Frame=new CGRect((attackWidth-44)/2,32,44,44);
        var height=(nfloat)Math.Min(260,Math.Max(180,(double)(controlY-top-52)*0.55));
        drawer.Frame=new CGRect(0,controlY-height,w,height);panelTitle.Frame=new CGRect(14,12,w-152,28);panelMessage.Frame=new CGRect(14,40,w-28,28);
        inventory.Frame=new CGRect(12,76,w-24,Math.Max(0,(double)height-84));
        stats.Frame=new CGRect(12,72,w-24,Math.Max(0,(double)height-82));
        characterDetails.Frame=players.Frame=stats.Frame;
        weightLabel.Frame=new CGRect(w-128,15,112,22);
        channel.Frame=new CGRect(14,height-48,48,36);
        composer.Frame=new CGRect(68,height-48,w-82,36);
        worldBorder.Frame=new CGRect(0,top+52,w,(nfloat)Math.Max(0,(double)(controlY-top-52)));
        World.Frame=new CGRect(7,worldBorder.Frame.Y+6,w-14,Math.Max(1,(double)worldBorder.Frame.Height-7));
        LayoutDestination();
        if(lastViewport!=World.Bounds.Size){lastViewport=World.Bounds.Size;ViewportChanged?.Invoke();}
    }
#if DEBUG
    public async Task VerifyInteractionsAsync()
    {
        var originalFrame=Frame;
        var originalExpanded=expanded;
        var originalSelected=selected;
        try
        {
            foreach(var size in new[]{new CGSize(320,568),new CGSize(393,852),originalFrame.Size})
            {
                Frame=new CGRect(originalFrame.Location,size);
                expanded=false;SetNeedsLayout();LayoutIfNeeded();
                var viewport=World.Frame;var rendering=RenderSize;
                var controlFrame=controls.Frame;
                foreach(var panel in new[]{"Bag","Chat","Skills","Stats","Players","Paperdoll","Quests"})
                {
                    selected=panel;
                    foreach(var open in new[]{true,false})
                    {
                        expanded=open;RefreshPanel();LayoutIfNeeded();
                        if(World.Frame!=viewport || RenderSize!=rendering || controls.Frame!=controlFrame)
                            throw new InvalidOperationException("Menu changed the world projection or controls.");
                    }
                }
            }
        }
        finally
        {
            Frame=originalFrame;expanded=originalExpanded;selected=originalSelected;
            RefreshPanel();LayoutIfNeeded();
        }
        var button=(UIButton)controls.ViewWithTag(10)!;
        var enabled=button.Enabled;
        Update(currentPlayer,false,true);
        if(!button.Enabled || button.Highlighted)throw new InvalidOperationException("Movement pending changed Attack visual state.");
        Update(currentPlayer,true,true);
        var count=0;
        void Count()=>count++;
        AttackRequested+=Count;
        try
        {
            button.Enabled=true;
            button.SendActionForControlEvents(UIControlEvent.TouchDown);
            // Match the real-touch test window; one 620 ms interval plus 130 ms
            // was too narrow while the multi-size preview was still drawing.
            await Task.Delay(1600);
            if(count<2 || count>4)throw new InvalidOperationException($"Held attack did not repeat: requests={count}, attached={Window is not null}, enabled={button.Enabled}, timerValid={attackTimer?.IsValid}, mainThread={NSThread.IsMain}.");
            button.SendActionForControlEvents(UIControlEvent.TouchUpInside);
            var released=count;
            await Task.Delay(750);
            if(count!=released)throw new InvalidOperationException("Attack continued after release.");
            button.SendActionForControlEvents(UIControlEvent.TouchDown);
            button.SendActionForControlEvents(UIControlEvent.TouchCancel);
            var cancelled=count;
            await Task.Delay(750);
            if(count!=cancelled)throw new InvalidOperationException("Attack continued after cancellation.");
            button.SendActionForControlEvents(UIControlEvent.TouchDown);
            StopControls();
            var stopped=count;
            await Task.Delay(750);
            if(count!=stopped)throw new InvalidOperationException("Attack continued after lifecycle stop.");
        }
        finally {StopControls();button.Enabled=enabled;AttackRequested-=Count;}
        await joystick.VerifyHoldAsync();
        Console.WriteLine("HUD checks passed: stable projection/controls at three sizes across all drawers; attack hold/release/cancel/lifecycle stop.");
    }
#endif
    protected override void Dispose(bool disposing){if(disposing){StopControls();foreach(var image in images)image.Dispose();}base.Dispose(disposing);}
}
