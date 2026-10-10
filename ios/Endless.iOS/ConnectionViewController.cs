using Endless.Apple.Protocol;
using Endless.Apple.World;
using Foundation;
using UIKit;

namespace Endless.Apple.iOS;

/// <summary>Login shell with a credential-free official-server connection check.</summary>
public sealed class ConnectionViewController : UIViewController
{
    private readonly UILabel status = Label("Server not checked", UIFontTextStyle.Title2);
    private readonly UILabel detail = Label("Check the server connection. No account is needed.", UIFontTextStyle.Body);
    private readonly UITextField username = AccountField("Username", false);
    private readonly UITextField password = AccountField("Password", true);
    private readonly UILabel characters = Label("", UIFontTextStyle.Body);
    private readonly UIStackView characterChoices = new() { Axis = UILayoutConstraintAxis.Vertical, Spacing = 8 };
    private readonly UIButton disconnect = new HudButton();
    private readonly UIImageView worldPreview = new() { ContentMode = UIViewContentMode.ScaleAspectFit, Hidden = true };
    private readonly UIButton signIn = new HudButton();
    private readonly UIButton remember = new HudButton();
    private bool rememberEnabled;
    private DesktopLoginArt? desktopArt;
    private DesktopLoginBackdrop? loginBackdrop;
    private WorldArtwork? selectionArtwork;
    private readonly UIButton credits = new HudButton();
    private GameHudView? hud;
    private UIStackView? loginFields;
    private UIView? rosterRegion;
    private UIStackView? screenStack;
    private UIImage? loginPanelArt;
    private UIImage? loginButtonArt;
    private CancellationTokenSource? cancellation;
    private DateTimeOffset nextCheck;
    private bool launchCheckStarted;
    private readonly UILabel response = Label("No response yet", UIFontTextStyle.Caption1);

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        OverrideUserInterfaceStyle = UIUserInterfaceStyle.Dark;
        View!.BackgroundColor = UIColor.FromRGB(112,80,39);
        var assetRoot=Path.Combine((NSBundle.MainBundle.ResourcePath ?? NSBundle.MainBundle.BundlePath),"WorldAssets");
        loginBackdrop=new DesktopLoginBackdrop(assetRoot){Frame=View.Bounds,AutoresizingMask=UIViewAutoresizing.FlexibleWidth|UIViewAutoresizing.FlexibleHeight};
        View.AddSubview(loginBackdrop);
        desktopArt=new DesktopLoginArt(assetRoot);
        loginPanelArt=desktopArt.CleanPanel();
        loginButtonArt=desktopArt.BlankPlaque();
        var scroll = new UIScrollView { TranslatesAutoresizingMaskIntoConstraints = false };
        var stack = new LayoutStack { Axis = UILayoutConstraintAxis.Vertical, Spacing = 8, TranslatesAutoresizingMaskIntoConstraints = false };
        screenStack=stack;stack.LaidOut=UpdateArtworkRegion;
        View.AddSubview(scroll);
        scroll.AddSubview(stack);
        var title = new UIView();
        title.HeightAnchor.ConstraintEqualTo(95).Active=true;
        var subtitle = Label("UNOFFICIAL • iPHONE CLIENT", UIFontTextStyle.Caption1);
        subtitle.TextColor = UIColor.SecondaryLabel;
        var profile = ServerProfile.Official;
        var server = Label($"Official world · {profile.Host}", UIFontTextStyle.Subheadline);
        server.TextColor = UIColor.SecondaryLabel;
        signIn.SetTitle("Sign in", UIControlState.Normal);
        signIn.TouchUpInside += TestSignIn;
        username.ReturnKeyType=UIReturnKeyType.Next;
        username.ShouldReturn=_=>{password.BecomeFirstResponder();return false;};
        password.ReturnKeyType=UIReturnKeyType.Go;
        password.ShouldReturn=_=>{TestSignIn(this,EventArgs.Empty);return false;};
        signIn.BackgroundColor = UIColor.SecondarySystemBackground;
        signIn.Layer.CornerRadius = 12;
        signIn.HeightAnchor.ConstraintGreaterThanOrEqualTo(50).Active = true;
        worldPreview.HeightAnchor.ConstraintEqualTo(300).Active = true;
        worldPreview.AccessibilityLabel = "Current map terrain preview";
        var loginNote = Label("Auto-login opens character selection on this iPhone.", UIFontTextStyle.Footnote);
        remember.SetTitle("☐ Remember me", UIControlState.Normal);
        remember.AccessibilityIdentifier = "remember-account";
        remember.TouchUpInside += (_, _) =>
        {
            if (rememberEnabled && !RememberedAccount.Forget())
            { status.Hidden=detail.Hidden=false;status.Text="Remember me";detail.Text = "Could not remove the saved account. Please try again."; return; }
            SetRemember(!rememberEnabled);
        };
        loginNote.TextColor = UIColor.SecondaryLabel;
        disconnect.AccessibilityIdentifier="leave-selection";
        disconnect.SetTitle("Disconnect", UIControlState.Normal);
        disconnect.Hidden = true;
        disconnect.TouchUpInside += (_, _) => CancelConnection();
        var rule = new UIView { BackgroundColor = UIColor.Separator };
        rule.HeightAnchor.ConstraintEqualTo(1).Active = true;
        response.TextColor = UIColor.SecondaryLabel;
        status.AccessibilityIdentifier = "server-status";
        response.AccessibilityIdentifier = "server-response";
        loginFields=new UIStackView { Axis=UILayoutConstraintAxis.Vertical,Spacing=10,LayoutMarginsRelativeArrangement=true,LayoutMargins=new UIEdgeInsets(20,22,16,22) };
        var panelBackground=new UIImageView(loginPanelArt){TranslatesAutoresizingMaskIntoConstraints=false};
        loginFields.InsertSubview(panelBackground,0);
        NSLayoutConstraint.ActivateConstraints([panelBackground.TopAnchor.ConstraintEqualTo(loginFields.TopAnchor),panelBackground.BottomAnchor.ConstraintEqualTo(loginFields.BottomAnchor),panelBackground.LeadingAnchor.ConstraintEqualTo(loginFields.LeadingAnchor),panelBackground.TrailingAnchor.ConstraintEqualTo(loginFields.TrailingAnchor)]);
        var loginActions=new UIStackView{Axis=UILayoutConstraintAxis.Horizontal,Alignment=UIStackViewAlignment.Center,Spacing=8};
        remember.HorizontalAlignment=UIControlContentHorizontalAlignment.Left;
        remember.HeightAnchor.ConstraintGreaterThanOrEqualTo(44).Active=true;
        remember.SetContentCompressionResistancePriority(750,UILayoutConstraintAxis.Horizontal);
        signIn.WidthAnchor.ConstraintEqualTo(100).Active=true;
        loginActions.AddArrangedSubview(remember);loginActions.AddArrangedSubview(new UIView());loginActions.AddArrangedSubview(signIn);
        foreach(var item in new UIView[]{username,password,loginActions})loginFields.AddArrangedSubview(item);
        foreach(var field in new[]{username,password}){field.LeftView=new UIView(new CGRect(0,0,8,44));field.LeftViewMode=UITextFieldViewMode.Always;field.Font=EoTypography.Body(18.333f);field.BorderStyle=UITextBorderStyle.None;field.Layer.BorderWidth=1;field.Layer.BorderColor=UIColor.FromRGB(135,107,57).CGColor;field.BackgroundColor=UIColor.FromRGB(65,46,29);field.TextColor=UIColor.FromRGB(245,226,177);field.HeightAnchor.ConstraintEqualTo(44).Active=true;}
        // UIKit's secure renderer requires its native masking font. The placeholder
        // remains EO pixel text; entered secrets are always native secure dots.
        password.Font=UIFont.SystemFontOfSize(18);
        password.AttributedPlaceholder=new NSAttributedString("Password",new UIStringAttributes{Font=EoTypography.Body(18.333f),ForegroundColor=UIColor.FromRGB(165,143,116)});
        username.AttributedPlaceholder=new NSAttributedString("Username",new UIStringAttributes{Font=EoTypography.Body(18.333f),ForegroundColor=UIColor.FromRGB(165,143,116)});
        foreach(var b in new[]{signIn,disconnect})StyleLoginButton(b);
        desktopArt.Button(signIn,14,2,120,40);
        signIn.AccessibilityLabel="Sign in";
        remember.SetTitleColor(UIColor.FromRGB(255,235,185),UIControlState.Normal);remember.TitleLabel!.Font=EoTypography.Body(13.75f);
        credits.AccessibilityLabel="View credits";
        desktopArt.Button(credits,13,2,180,40);
        credits.SetImage(null,UIControlState.Normal);credits.SetImage(null,UIControlState.Highlighted);
        credits.SetBackgroundImage(desktopArt.Crop(13,0,80,180,40),UIControlState.Normal);
        credits.SetBackgroundImage(desktopArt.Crop(13,180,80,180,40),UIControlState.Highlighted);
        credits.HeightAnchor.ConstraintEqualTo(44).Active=true;
        credits.TouchUpInside+=(_,_)=>PresentViewController(new SafariServices.SFSafariViewController(
            new NSUrl("https://www.endless-online.com/credential.html")),true,null);
        rosterRegion=new UIView{Hidden=true};
        characterChoices.TranslatesAutoresizingMaskIntoConstraints=false;
        rosterRegion.AddSubview(characterChoices);
        NSLayoutConstraint.ActivateConstraints([
            characterChoices.TopAnchor.ConstraintEqualTo(rosterRegion.TopAnchor),
            characterChoices.BottomAnchor.ConstraintEqualTo(rosterRegion.BottomAnchor),
            characterChoices.TrailingAnchor.ConstraintEqualTo(rosterRegion.TrailingAnchor),
            characterChoices.LeadingAnchor.ConstraintEqualTo(rosterRegion.LeadingAnchor)]);
        var supportingActions=new UIStackView{Axis=UILayoutConstraintAxis.Vertical,Alignment=UIStackViewAlignment.Leading,Spacing=6};
        supportingActions.AddArrangedSubview(credits);
        credits.WidthAnchor.ConstraintEqualTo(193).Active=true;
        foreach (var item in new UIView[] { title, status, detail, loginFields, rosterRegion, supportingActions, disconnect, characters, worldPreview, response }) stack.AddArrangedSubview(item);
        characters.Hidden=true;response.Hidden=true;status.Hidden=detail.Hidden=true;
        foreach(var label in new[]{status,detail}){label.Font=EoTypography.Body(16.5f);label.TextColor=UIColor.FromRGB(255,240,203);label.BackgroundColor=UIColor.FromRGBA(44,31,20,205);}
        scroll.KeyboardDismissMode=UIScrollViewKeyboardDismissMode.OnDrag;
        NSLayoutConstraint.ActivateConstraints([
            scroll.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
            scroll.BottomAnchor.ConstraintEqualTo(View.KeyboardLayoutGuide.TopAnchor),
            scroll.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
            scroll.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
            stack.TopAnchor.ConstraintEqualTo(scroll.ContentLayoutGuide.TopAnchor, 8),
            stack.BottomAnchor.ConstraintEqualTo(scroll.ContentLayoutGuide.BottomAnchor, -16),
            stack.LeadingAnchor.ConstraintEqualTo(scroll.ContentLayoutGuide.LeadingAnchor, 24),
            stack.TrailingAnchor.ConstraintEqualTo(scroll.ContentLayoutGuide.TrailingAnchor, -24),
            stack.WidthAnchor.ConstraintEqualTo(scroll.FrameLayoutGuide.WidthAnchor, 1, -48)
        ]);
    }

    private void UpdateArtworkRegion()
    {
        if(loginBackdrop==null||screenStack==null)return;
        // UIStackView lays out hidden-state transitions independently of the
        // controller, so reserve space after the stack's own final layout pass.
        var visibleBottom=screenStack.ArrangedSubviews.Where(v=>!v.Hidden)
            .Select(v=>v.ConvertRectToView(v.Bounds,View).Bottom).DefaultIfEmpty(0).Max();
        loginBackdrop.ArtworkTop=visibleBottom+10;
        loginBackdrop.SetNeedsLayout();
    }

    private sealed class LayoutStack:UIStackView
    {
        public Action? LaidOut {get;set;}
        public override void LayoutSubviews(){base.LayoutSubviews();LaidOut?.Invoke();}
    }

    public override void ViewDidAppear(bool animated)
    {
        base.ViewDidAppear(animated);
#if DEBUG
        if (!launchCheckStarted && NSProcessInfo.ProcessInfo.Arguments.Contains("--keychain-self-test"))
        {
            launchCheckStarted = true;
            status.Text = RememberedAccount.SelfTest() ? "Keychain checks passed" : "Keychain checks failed";
            Console.WriteLine(status.Text);
            detail.Text = "Synthetic account: empty, save, read, replace, remove. No server connection.";
            return;
        }
#endif
        if (!launchCheckStarted && NSProcessInfo.ProcessInfo.Arguments.Contains("--hud-preview"))
        {
            launchCheckStarted = true;
            var root = Path.Combine((NSBundle.MainBundle.ResourcePath ?? NSBundle.MainBundle.BundlePath),"WorldAssets");
            var map=WorldMap.Load(File.ReadAllBytes(Path.Combine(root,"data","maps","00001.emf")));
            var actor=new WorldActor(1,"Preview",map.Width/2,map.Height/2,0,0,0,1,0,100,100,new int[9]);
            var artwork=new WorldArtwork(root);
            if(NSProcessInfo.ProcessInfo.Arguments.Contains("--attack-preview"))
            {
                var weaponName=NSProcessInfo.ProcessInfo.Arguments.Contains("--choppy-preview")?"Old Choppy":"Small Sword";
                var weapon=Enumerable.Range(1,artwork.Items.Count).FirstOrDefault(id=>artwork.Items.Find(id)?.Name==weaponName);
                var equipment=new int[9];equipment[3]=weapon;
                actor=actor with {Equipment=equipment,AttackStartedMilliseconds=Environment.TickCount64-220};
            }
            void DrawPreview()
            {
                if(hud==null || hud.World.Bounds.Width<1)return;
                if(NSProcessInfo.ProcessInfo.Arguments.Contains("--attack-preview"))actor=actor with {AttackStartedMilliseconds=Environment.TickCount64-220};
                var size=hud.RenderSize;
                var raster=WorldRenderer.Render(map,root,[actor],new WorldCamera(actor.X+hud.CameraOffset.X,actor.Y+hud.CameraOffset.Y,size.Width,size.Height),artwork);
                var old=hud.World.Image;hud.World.Image=NativeWorldImage.Create(raster);old?.Dispose();
                hud.Update(actor,true);hud.UpdatePlayers([actor]);
                hud.SetDestination((actor.X+1,actor.Y),new WorldCamera(actor.X+hud.CameraOffset.X,actor.Y+hud.CameraOffset.Y,size.Width,size.Height));
            }
            hud = new GameHudView(root,direction=>
            {
                var (dx,dy)=direction switch {0=>(0,1),1=>(-1,0),2=>(0,-1),_=>(1,0)};
                if(map.CanWalk(actor.X+dx,actor.Y+dy)) actor=actor with {X=actor.X+dx,Y=actor.Y+dy,Direction=direction};
                DrawPreview();
            },()=>{hud?.RemoveFromSuperview();hud?.Dispose();hud=null;})
            { Frame=View!.Bounds,AutoresizingMask=UIViewAutoresizing.FlexibleWidth|UIViewAutoresizing.FlexibleHeight };
            hud.ViewportChanged+=DrawPreview;
            View.AddSubview(hud);
            if (NSProcessInfo.ProcessInfo.Arguments.Contains("--expanded"))
            {
                var ids=Enumerable.Range(1,artwork.Items.Count).Where(id=>artwork.Items.Find(id) is {InventoryGraphic:>0}).GroupBy(id=>artwork.Items.Find(id)!.InventorySize).SelectMany(group=>group.Take(3)).Take(24).ToArray();
                hud.UpdateInventory(new WorldInventory(24,70,ids.Select((id,index)=>new InventoryEntry(id,index%4==0?12:1)).ToArray()));
                hud.ShowInventory();
            }
            View.LayoutIfNeeded();hud.LayoutIfNeeded();
            DrawPreview();
#if DEBUG
            if(NSProcessInfo.ProcessInfo.Arguments.Contains("--hud-self-test"))
                _=VerifyHudPreviewAsync(hud);
#endif
            return;
        }
#if DEBUG
        if(!launchCheckStarted && (NSProcessInfo.ProcessInfo.Arguments.Contains("--login-preview") || NSProcessInfo.ProcessInfo.Arguments.Contains("--keyboard-preview") || NSProcessInfo.ProcessInfo.Arguments.Contains("--selection-preview")))
        {
            launchCheckStarted=true;
            status.Hidden=detail.Hidden=true;
            if(NSProcessInfo.ProcessInfo.Arguments.Contains("--status-preview")){status.Hidden=detail.Hidden=false;status.Text="Connecting…";detail.Text="Checking the connection and signing in…";}
            if(NSProcessInfo.ProcessInfo.Arguments.Contains("--keyboard-preview"))username.BecomeFirstResponder();
            if(NSProcessInfo.ProcessInfo.Arguments.Contains("--selection-preview"))
            {
                loginFields!.Hidden=credits.Hidden=true;
                rosterRegion!.Hidden=false;loginBackdrop!.SelectionMode=true;loginBackdrop.SetNeedsLayout();
                var root=Path.Combine(NSBundle.MainBundle.ResourcePath!,"WorldAssets");
                selectionArtwork=new WorldArtwork(root);
                var sample=Enumerable.Range(0,3).Select(i=>new RechargedCharacter(i+1,new[]{"Adventurer","LongCharacterName","Explorer"}[i]){Appearance=new WorldActor(i+1,"Preview",0,0,0,i%2,0,i+1,i,0,0,new int[9]),Level=i*25});
                var carousel=new CharacterCarousel(sample,desktopArt!,selectionArtwork,_=>{});characterChoices.AddArrangedSubview(carousel);
                carousel.VerifyNavigation();
                disconnect.Hidden=false;desktopArt!.Button(disconnect,15,5,91,29);
            }
            return;
        }
#endif
        // Diagnostic launches bypass remembered sign-in.
        if (!launchCheckStarted && NSProcessInfo.ProcessInfo.Arguments.Contains("--check-server-once"))
        {
            launchCheckStarted = true;
            CheckConnection(this, EventArgs.Empty);
            return;
        }
        if (!launchCheckStarted)
        {
            launchCheckStarted = true;
            var saved = RememberedAccount.Read();
            if (saved != null)
            {
                SetRemember(true);
                username.Text = saved.Username;
                password.Text = saved.Password;
                TestSignIn(this, EventArgs.Empty);
            }
        }
    }

    private static UITextField AccountField(string placeholder, bool secure) => new()
    {
        Placeholder = placeholder,
        SecureTextEntry = secure,
        Enabled = true,
        AutocorrectionType = UITextAutocorrectionType.No,
        AutocapitalizationType = UITextAutocapitalizationType.None,
        BorderStyle = UITextBorderStyle.RoundedRect,
        Font = EoTypography.Body(22),
        AccessibilityLabel = placeholder
    };

#if DEBUG
    private static async Task VerifyHudPreviewAsync(GameHudView preview)
    {
        try {NativeWorldImage.Verify();await preview.VerifyInteractionsAsync();}
        catch(Exception error){Console.WriteLine("HUD checks failed: "+error.Message);}
    }
#endif
    public void CancelConnection() { hud?.StopControls(); password.Text = string.Empty; cancellation?.Cancel(); }

    private async void CheckConnection(object? sender, EventArgs args)
    {
        if (cancellation != null) return;
        status.Hidden=detail.Hidden=false;
        if (DateTimeOffset.UtcNow < nextCheck)
        {
            detail.Text = "Please wait a minute between connection checks.";
            return;
        }
        nextCheck = DateTimeOffset.UtcNow.AddMinutes(1);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        cancellation = timeout;
        signIn.Enabled = false;
        status.Text = "Connecting…";
        response.Text = "Waiting for server reply…";
        detail.Text = "Checking initialization and pre-login synchronization. No credentials are sent.";
        try
        {
            const string key = "EndlessAppleInstallationID";
            var defaults = NSUserDefaults.StandardUserDefaults;
            var identity = defaults.StringForKey(key);
            if (identity == null)
            {
                identity = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000000, int.MaxValue).ToString();
                defaults.SetString(identity, key);
            }
            var started = System.Diagnostics.Stopwatch.StartNew();
            var progress = new Progress<SessionCheckResult>(update =>
            {
                if (timeout.IsCancellationRequested) return;
                status.Text = "Checking sustained connection…";
                detail.Text = update.Message;
                response.Text = $"Packet {update.Family}/{update.Action} · {update.ResponseBytes} bytes";
            });
            var result = await RechargedSession.CheckAsync(ServerProfile.Official, identity, timeout.Token, watch: true, progress: progress);
            status.Text = result.Status switch
            {
                "session-observed" => "Connection check passed",
                "synchronization-replied" => "Server synchronization received",
                "session-response" => "Server session response received",
                "session-unconfirmed" => "Initialization received · session unconfirmed",
                "session-closed" => "Server closed the session",
                "init-received" => "Server responded · initialization received",
                "rejected" => "Server responded · connection rejected",
                "out-of-date" => "Server responded · version mismatch",
                _ => "Server responded · protocol mismatch"
            };
            response.Text = $"{started.Elapsed.TotalSeconds:F1} seconds · checked {DateTimeOffset.Now:t}\nClient {ServerProfile.Official.Version}";
            detail.Text = result.Message;
        }
        catch (OperationCanceledException) { status.Text = "Connection stopped"; response.Text = "Connection check interrupted."; detail.Text = "The check timed out or the app became inactive."; }
        catch (Exception ex) when (ex is InvalidDataException or IOException or System.Net.Sockets.SocketException or ArgumentException)
        { status.Text = "Connection check failed"; response.Text = "Connection check did not complete."; detail.Text = ex.Message; }
        finally { cancellation = null; signIn.Enabled = true; }
    }

    private async void TestSignIn(object? sender, EventArgs args)
    {
        if (cancellation != null) return;
        status.Hidden=detail.Hidden=false;
        if (DateTimeOffset.UtcNow < nextCheck)
        {
            detail.Text = "Please wait a minute between connection attempts.";
            return;
        }
        var account = (username.Text ?? string.Empty).ToCharArray();
        var secret = (password.Text ?? string.Empty).ToCharArray();
        var saveAccount = rememberEnabled ? (char[])account.Clone() : [];
        var saveSecret = rememberEnabled ? (char[])secret.Clone() : [];
        password.Text = string.Empty;
        View?.EndEditing(true);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        cancellation = timeout;
        signIn.Enabled = remember.Enabled = username.Enabled = password.Enabled = false;
        desktopArt!.Button(disconnect,15,1,91,29);
        disconnect.Hidden = false;
        var savedThisAttempt = false;
        ClearCharacterChoices();
        var selection = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        characters.Text = string.Empty;
        worldPreview.Hidden = true;
        var previousImage = worldPreview.Image; worldPreview.Image = null; previousImage?.Dispose();
        status.Text = "Connecting…";
        detail.Text = "Checking the connection and signing in…";
        response.Text = "Waiting for server reply…";
        try
        {
            const string key = "EndlessAppleInstallationID";
            var defaults = NSUserDefaults.StandardUserDefaults;
            var identity = defaults.StringForKey(key);
            if (identity == null)
            {
                identity = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000000, int.MaxValue).ToString();
                defaults.SetString(identity, key);
            }
            nextCheck = DateTimeOffset.UtcNow.AddMinutes(1);
            var progress = new Progress<SessionCheckResult>(update =>
            {
                if (cancellation != timeout || timeout.IsCancellationRequested) return;
                var savedSuccessfully = true;
                if (!savedThisAttempt && rememberEnabled)
                {
                    savedThisAttempt = true;
                    savedSuccessfully = RememberedAccount.Save(new string(saveAccount), new string(saveSecret));
                }
                Array.Clear(saveAccount); Array.Clear(saveSecret);
                desktopArt!.Button(disconnect,15,5,91,29);
                timeout.CancelAfter(TimeSpan.FromMinutes(5));
                status.Text = "Sign-in accepted";
                detail.Text = savedSuccessfully ? update.Message : "Signed in, but this device could not save the account.";
                characters.Text = update.Characters?.Count > 0
                    ? string.Join("\n", update.Characters.Select(c => c.Name))
                    : "No characters on this account.";
                ClearCharacterChoices();
                loginFields!.Hidden=true;credits.Hidden=true;
                rosterRegion!.Hidden=false;loginBackdrop!.SelectionMode=true;loginBackdrop.SetNeedsLayout();
                selectionArtwork ??= new WorldArtwork(Path.Combine(NSBundle.MainBundle.ResourcePath!,"WorldAssets"));
                var carousel=new CharacterCarousel(update.Characters ?? [],desktopArt!,selectionArtwork,id=>
                {
                    if(!selection.TrySetResult(id))return;
                    foreach(var view in characterChoices.ArrangedSubviews.OfType<CharacterCarousel>())view.Pending=true;
                    status.Text="Selecting character…";detail.Text="Waiting for world information from the server.";
                });
                characterChoices.AddArrangedSubview(carousel);
                response.Text = "Connected · session limited to five minutes during development";
                disconnect.Hidden = false;
            });
            var result = await RechargedLogin.TestAsync(ServerProfile.Official, identity, account, secret, timeout.Token, progress,
                characterSelection: selection.Task, worldSession: async (stream,session,welcome,token) =>
                {
                    var root = Path.Combine((NSBundle.MainBundle.ResourcePath ?? NSBundle.MainBundle.BundlePath),"WorldAssets");
                    status.Text = "Loading world…";
                    var loaded = await Task.Run(() => WorldRenderer.LoadPreview(welcome,root),token);
                    worldPreview.Image = NativeWorldImage.Create(loaded.Image);
                    worldPreview.Hidden = true;
                    ClearCharacterChoices();
                    var state = new RechargedWorldState(session.PlayerId);
                    (int X,int Y)? routeTarget=null;
                    WorldCamera? displayedCamera=null;
                    var nextRouteStep=DateTimeOffset.MinValue;
                    var routeDeadline=DateTimeOffset.MinValue;
                    (int X,int Y)? lastRoutePosition=null;
                    hud = new GameHudView(root,direction => {routeTarget=null;hud?.SetDestination(null,displayedCamera);state.RequestMove(direction);},CancelConnection)
                    { Frame = View!.Bounds, AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight };
                    hud.AttackRequested+=()=>{routeTarget=null;hud?.SetDestination(null,displayedCamera);state.RequestAttack();};
                    hud.WorldTapped+=(x,y)=>
                    {
                        if(displayedCamera is not {} camera || !state.Entered)return;
                        var target=camera.TileAt(x*camera.Width,y*camera.Height);
                        routeTarget=loaded.Map.CanWalk(target.X,target.Y) && !state.IsOccupied(target.X,target.Y)?target:null;
                        hud?.SetDestination(routeTarget,displayedCamera);
                        lastRoutePosition=state.Player is {} origin ? (origin.X,origin.Y) : null;
                        routeDeadline=DateTimeOffset.UtcNow.AddSeconds(3);
                    };
                    View.AddSubview(hud);
                    var artwork = new WorldArtwork(root);
                    WorldActor[]? pendingActors = null;
                    WorldNpc[] pendingNpcs = [];
                    WorldCamera? pendingCamera = null;
                    Task renderTask = Task.CompletedTask;
                    bool rendering = false;
                    bool activeWorld = true;
                    int animationFrame = 0;
                    var animationClock = System.Diagnostics.Stopwatch.StartNew();
                    var motion = new WorldMotion();
                    int? pendingPlayer = null;
                    using var animationCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                    async Task RenderPending()
                    {
                        rendering = true;
                        try
                        {
                            while (activeWorld && pendingActors is {} snapshot)
                            {
                                var camera = pendingCamera;
                                var viewport = hud?.RenderSize ?? (Width:320,Height:480);
                                var pan = hud?.CameraOffset ?? (X:0.0,Y:0.0);
                                var npcs = pendingNpcs;
                                var frame = animationFrame;
                                var playerId = pendingPlayer;
                                pendingActors = null;
                                var raster = await Task.Run(() =>
                                {
                                    var seconds = animationClock.Elapsed.TotalSeconds;
                                    motion.Update(snapshot.Select(a => (false,a.Id,a.X,a.Y))
                                        .Concat(npcs.Select(n => (true,n.Id,n.X,n.Y))),seconds);
                                    if (playerId is {} ownId)
                                    {
                                        var position = motion.Position(false,ownId,seconds);
                                        camera = new WorldCamera(position.X+pan.X,position.Y+pan.Y,viewport.Width,viewport.Height);
                                    }
                                    return WorldRenderer.Render(loaded.Map,root,snapshot,camera,artwork,npcs,frame,motion,seconds);
                                },token);
                                if (!activeWorld || token.IsCancellationRequested) return;
                                var previousImage = hud?.World.Image;
                                if (hud != null) hud.World.Image = NativeWorldImage.Create(raster);
                                displayedCamera=camera;
                                hud?.SetDestination(routeTarget,camera);
                                previousImage?.Dispose();
                            }
                        }
                        finally { rendering = false; }
                    }
                    void QueueScene()
                    {
                        pendingActors = state.Actors.ToArray();
                        pendingPlayer = state.Player?.Id;
                        pendingNpcs = state.Npcs.ToArray();
                        pendingCamera = state.Player is {} focus ? new WorldCamera(focus.X,focus.Y) : null;
                        if (renderTask.IsFaulted) renderTask.GetAwaiter().GetResult();
                        if (!rendering) renderTask = RenderPending();
                    }
                    async Task Animate()
                    {
                        try
                        {
                            while (activeWorld)
                            {
                                await Task.Delay(50,animationCancellation.Token);
                                if (!activeWorld || !state.Entered) continue;
                                if(routeTarget is {} target && !state.MovePending && state.Player is {} walker && DateTimeOffset.UtcNow>=nextRouteStep)
                                {
                                    if(lastRoutePosition != (walker.X,walker.Y))
                                    {lastRoutePosition=(walker.X,walker.Y);routeDeadline=DateTimeOffset.UtcNow.AddSeconds(3);}
                                    if(DateTimeOffset.UtcNow>=routeDeadline){routeTarget=null;continue;}
                                    var direction=WorldPathfinder.NextDirection(loaded.Map.Width,loaded.Map.Height,
                                        walker.X,walker.Y,target.X,target.Y,(x,y)=>loaded.Map.CanWalk(x,y)&&!state.IsOccupied(x,y));
                                    if(direction is {} d)state.RequestMove(d);else routeTarget=null;
                                    nextRouteStep=DateTimeOffset.UtcNow.AddMilliseconds(250);
                                }
                                animationFrame = (int)(animationClock.Elapsed.TotalSeconds*4)%4;
                                QueueScene();
                            }
                        }
                        catch (OperationCanceledException) when (animationCancellation.IsCancellationRequested) { }
                    }
                    var animationTask = Animate();
                    try
                    {
                    await state.RunAsync(stream,session,welcome,world =>
                    {
                        status.Text = loaded.Map.Name;
                        detail.Text = world.Entered ? "World connected" : "Entering world…";
                        hud?.UpdatePlayers(world.Actors);
                        hud?.UpdateInventory(world.Inventory,world.InventoryUnsupported);
                        hud?.Update(world.Player,world.Entered && !world.MovePending && world.Player is {} mover && loaded.Map.CanWalk(mover.X,mover.Y),world.Entered);
                        QueueScene();
                        characters.Text = string.Join("\n",world.Actors.Select(a => a.Name));
                        foreach (var view in characterChoices.ArrangedSubviews)
                            if (view is UIButton button) button.Enabled = world.Entered && !world.MovePending && world.Player is {} player && loaded.Map.CanWalk(player.X,player.Y);
                        response.Text = world.Player is {} own
                            ? loaded.Map.CanWalk(own.X,own.Y) ? "Connected" : "Movement unavailable: the character position does not match a walkable map tile."
                            : "Connected · waiting for character";
                    },token,loaded.Map.CanWalk);
                    }
                    finally
                    {
                        activeWorld = false;
                        pendingActors = null;
                        animationCancellation.Cancel();
                        await animationTask;
                        await renderTask;
                    }
                    return new SessionCheckResult("disconnected","World session ended.",true,null,null,0);
                });
            status.Text = "Connection ended";
            detail.Text = result.Message;
        }
        catch (OperationCanceledException)
        { status.Text = "Disconnected"; detail.Text = "Disconnected. The session ended, was canceled, or the app became inactive."; response.Text = "No automatic retry."; }
        catch (Exception ex) when (ex is InvalidDataException or IOException or System.Net.Sockets.SocketException or ArgumentException)
        { status.Text = "Sign-in failed"; detail.Text = ex is ArgumentException ? "Enter 1–16 printable ASCII characters in each field." : "The connection or response could not be processed. Login is not confirmed."; response.Text = "No automatic retry."; }
        finally
        {
            hud?.RemoveFromSuperview(); hud?.Dispose(); hud = null;
            ClearCharacterChoices();
            disconnect.Hidden = true;
            worldPreview.Hidden=true;
            rosterRegion!.Hidden=true;
            loginBackdrop!.SelectionMode=false;loginBackdrop.SetNeedsLayout();
            loginBackdrop.RefreshSplash();
            loginFields!.Hidden=false;credits.Hidden=false;
            Array.Clear(account); Array.Clear(secret);
            Array.Clear(saveAccount); Array.Clear(saveSecret);
            cancellation = null;
            signIn.Enabled = remember.Enabled = username.Enabled = password.Enabled = true;
        }
    }

    private void SetRemember(bool enabled)
    {
        rememberEnabled = enabled;
        remember.SetTitle(enabled ? "☑ Remember me" : "☐ Remember me", UIControlState.Normal);
        remember.AccessibilityValue = enabled ? "On" : "Off";
    }

    private void StyleLoginButton(UIButton button)
    {
        button.SetBackgroundImage(loginButtonArt,UIControlState.Normal);
        button.BackgroundColor=UIColor.Clear;button.Layer.CornerRadius=0;
        button.SetTitleColor(UIColor.FromRGB(255,235,185),UIControlState.Normal);
        button.SetAttributedTitle(EoTypography.Title(button.CurrentTitle??"",16),UIControlState.Normal);
    }

    private void ClearCharacterChoices()
    {
        foreach (var view in characterChoices.ArrangedSubviews)
        {
            characterChoices.RemoveArrangedSubview(view);
            view.RemoveFromSuperview();
            view.Dispose();
        }
    }

    private static UIFont PreferredFont(UIFontTextStyle style) =>
        EoTypography.Body(16.5f);

    private static UILabel Label(string text, UIFontTextStyle style) => new()
    {
        Text = text, Font = PreferredFont(style),
        Lines = 0, AdjustsFontForContentSizeCategory = true
    };
}
