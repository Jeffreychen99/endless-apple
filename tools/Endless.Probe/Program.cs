using System.Text.Json;
using Endless.Apple.Protocol;
using Endless.Apple.World;

if (args.Length == 0 || args.Contains("--help"))
{
    Console.WriteLine("Endless Apple compatibility probe\nUsage: probe --official | --host HOST --port PORT --version X.Y.Z --dialect classic|recharged\nBy default sends one initialization packet, then disconnects. Credentials are sent only with --login-file PATH (official server only).\n--session-check sends a connection acknowledgement and observes one packet for up to 20 seconds and answers a recognized synchronization request (Recharged only).\n--session-watch observes for 45 seconds and answers known synchronization/keepalive packets.\n--select-character with --login-file selects the sole returned character, reads one response, then disconnects.\n--walk-test additionally attempts one ordinary-floor step and return, requiring server confirmation.\n--enter-world enters the world and observes it for eight seconds, using --assets.\n--assets DIR --world-preview PNG renders verified cached scenery after selection.\n--render-map ID --assets DIR --world-preview PNG renders a cached map offline, without credentials or a server connection.\n--json emits a sanitized report. Success means initialization only, not playable compatibility.");
    return 0;
}
try
{
    var options = new Dictionary<string, string>();
    var official = false;
    var json = false;
    var sessionCheck = false;
    var watch = false;
    string? loginFile = null;
    string? assets = null, previewPath = null;
    int? renderMap = null;
    var selectCharacter = false;
    var enterWorld = false;
    var walkTest = false;
    var attackTest = false;
    var faceTest = false;
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--render-map" && i+1 < args.Length) { renderMap = int.Parse(args[++i]); continue; }
        if (args[i] == "--assets" && i+1 < args.Length) { assets = args[++i]; continue; }
        if (args[i] == "--world-preview" && i+1 < args.Length) { previewPath = args[++i]; continue; }
        if (args[i] == "--login-file" && i + 1 < args.Length) { loginFile = args[++i]; continue; }
        if (args[i] == "--face-test") {faceTest=true; enterWorld=true; selectCharacter=true; continue;}
        if (args[i] == "--attack-test") {attackTest=true; enterWorld=true; selectCharacter=true; continue;}
        if (args[i] == "--walk-test") {walkTest=true; enterWorld=true; selectCharacter=true; continue;}
        if (args[i] == "--enter-world") { enterWorld = true; selectCharacter = true; continue; }
        if (args[i] == "--select-character") { selectCharacter = true; continue; }
        if (args[i] == "--official") { official = true; continue; }
        if (args[i] == "--session-watch") { sessionCheck = true; watch = true; continue; }
        if (args[i] == "--session-check") { sessionCheck = true; continue; }
        if (args[i] == "--json") { json = true; continue; }
        if (args[i] is not ("--host" or "--port" or "--version" or "--dialect") || i + 1 >= args.Length)
            throw new ArgumentException($"Unknown or incomplete option: {args[i]}");
        options.Add(args[i], args[++i]);
    }
    if (renderMap != null)
    {
        if (assets == null || previewPath == null || renderMap <= 0 || renderMap > 64008 || loginFile != null || official || options.Count > 0)
            throw new ArgumentException("Offline rendering requires --render-map ID --assets DIR --world-preview PNG, without connection options.");
        var map = WorldMap.Load(await File.ReadAllBytesAsync(Path.Combine(assets,"data","maps",$"{renderMap:D5}.emf")));
        var raster = WorldRenderer.Render(map,assets);
        await File.WriteAllBytesAsync(previewPath,WorldRenderer.Png(raster));
        Console.WriteLine(JsonSerializer.Serialize(new { status = "offline-map-rendered", map.Width, map.Height, terrainTiles = map.Layers[0].Count }));
        return 0;
    }
    if (official && options.Count > 0) throw new ArgumentException("Use --official or an explicit profile, not both.");
    var profile = ServerProfile.Official;
    if (!official)
    {
        var v = options["--version"].Split('.').Select(int.Parse).ToArray();
        if (v.Length != 3) throw new ArgumentException("Version must be X.Y.Z.");
        var dialect = options["--dialect"] switch { "classic" => ProtocolDialect.Classic, "recharged" => ProtocolDialect.Recharged, _ => throw new ArgumentException("Unknown dialect.") };
        profile = new(options["--host"], int.Parse(options["--port"]), v[0], v[1], v[2], dialect);
    }
    if (loginFile != null && (!official || sessionCheck))
        throw new ArgumentException("--login-file requires --official and cannot be combined with session checks.");
    if (selectCharacter && loginFile == null) throw new ArgumentException("--select-character requires --login-file.");
    if (previewPath != null && (assets == null || !selectCharacter)) throw new ArgumentException("--world-preview requires --assets and --select-character.");
    if (enterWorld && assets == null) throw new ArgumentException("--enter-world requires --assets.");
    // A stable per-installation ID; never fingerprint or expose the user's hardware serial.
    var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EndlessApple");
    Directory.CreateDirectory(folder);
    var identityPath = Path.Combine(folder, "installation-id");
    if (!File.Exists(identityPath)) await File.WriteAllTextAsync(identityPath, System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000000, int.MaxValue).ToString());
    var identity = (await File.ReadAllTextAsync(identityPath)).Trim();
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(loginFile != null ? 35 : watch ? 60 : sessionCheck ? 35 : 12));
    if (loginFile != null)
    {
        char[] account = [], password = [];
        try
        {
            using (var reader = new StreamReader(loginFile))
            {
                account = (await reader.ReadLineAsync() ?? "").ToCharArray();
                password = (await reader.ReadLineAsync() ?? "").ToCharArray();
                if (account.Length == 0 || password.Length == 0)
                    throw new ArgumentException("Credential file must contain username and password on separate lines.");
            }
            WorldActor? worldAppearance=null;
            var login = await RechargedLogin.TestAsync(profile, identity, account, password, timeout.Token, selectSingleCharacter: selectCharacter,
                worldSession: enterWorld ? async (stream,session,welcome,token) =>
                {
                    var loaded = WorldRenderer.LoadPreview(welcome,assets!); // Validate the local map before entering.
                    var state = new RechargedWorldState(session.PlayerId);
                    using var observation = CancellationTokenSource.CreateLinkedTokenSource(token);
                    observation.CancelAfter(TimeSpan.FromSeconds(8));
                    WorldActor? start = null;
                    var stepDirection = -1;
                    var moved = false;
                    var returned = false;
                    var attackQueued = false;
                    var attackSent = false;
                    (int X,int Y,int Direction)? blockedTurn=null;
                    var turnChecked=false;var blockedTurnSent=false;
                    void Changed(RechargedWorldState world)
                    {
                        if(faceTest && world.Entered && !world.MovePending && world.Player is {} turning)
                        {
                            if(!turnChecked && loaded.Map.CanWalk(turning.X,turning.Y))
                            {
                                turnChecked=true;
                                for(var d=0;d<4;d++)
                                {
                                    var x=turning.X+(d==1?-1:d==3?1:0);var y=turning.Y+(d==2?-1:d==0?1:0);
                                    if(d==turning.Direction || loaded.Map.CanWalk(x,y)&&!world.IsOccupied(x,y))continue;
                                    if(world.RequestMove(d))blockedTurn=(turning.X,turning.Y,d);
                                    break;
                                }
                            }
                            else if(blockedTurn is {} target)
                                blockedTurnSent=turning.Direction==target.Direction&&turning.X==target.X&&turning.Y==target.Y;
                        }
                        if(attackTest && world.Entered && world.Player is {} attacker)
                        {
                            if(!attackQueued)attackQueued=world.RequestAttack();
                            attackSent |= attacker.AttackStartedMilliseconds != null;
                        }
                        if (!walkTest || !world.Entered || world.Player is not {} player || world.MovePending) return;
                        if (start == null)
                        {
                            if (!loaded.Map.CanWalk(player.X,player.Y)) return;
                            start=player;
                            for(var d=0;d<4;d++)
                            {
                                var x=player.X+(d==1?-1:d==3?1:0);
                                var y=player.Y+(d==2?-1:d==0?1:0);
                                if (!loaded.Map.CanWalk(x,y) || world.IsOccupied(x,y)) continue;
                                stepDirection=d; world.RequestMove(d); break;
                            }
                        }
                        else if (!moved && (player.X!=start.X || player.Y!=start.Y))
                        { moved=true; world.RequestMove((stepDirection+2)%4); }
                        else if (moved && player.X==start.X && player.Y==start.Y) returned=true;
                    }
                    try { await state.RunAsync(stream,session,welcome,Changed,observation.Token,loaded.Map.CanWalk); }
                    catch (OperationCanceledException) when (!token.IsCancellationRequested) { }
                    if (previewPath != null)
                        await File.WriteAllBytesAsync(previewPath,WorldRenderer.Png(WorldRenderer.Render(loaded.Map,assets!,state.Actors,state.Player is {} focus ? new WorldCamera(focus.X,focus.Y) : null,npcs:state.Npcs)));
                    worldAppearance=state.Player;
                    Console.WriteLine(JsonSerializer.Serialize(new { status="world-state", entered=state.Entered,
                        actorCount=state.Actors.Count, npcCount=state.Npcs.Count, inventoryReceived=state.Inventory!=null, inventoryEntryCount=state.Inventory?.Entries.Count, ownPlayerPresent=state.Player != null, stepConfirmed=moved, returnConfirmed=returned, attackSent, blockedTurnAvailable=blockedTurn!=null, blockedTurnSent, movePending=state.MovePending,
                        actorInBounds=state.Player is {} a && a.X < loaded.Map.Width && a.Y < loaded.Map.Height,
                        supportedSkin=state.Player is {} b && b.Gender<2 && b.Skin<16,
                        packetCounts=state.PacketCounts.Select(p=>new {action=p.Key.Action,family=p.Key.Family,count=p.Value}) }));
                    return new SessionCheckResult(state.Entered && state.Player != null && (!walkTest || returned) ? "world-entered" : "world-unconfirmed",
                        "Bounded world observation ended.",true,null,null,0);
                } : null);
            if (!enterWorld && previewPath != null && login.Welcome != null)
            {
                var preview = WorldRenderer.LoadPreview(login.Welcome, assets!);
                await File.WriteAllBytesAsync(previewPath, WorldRenderer.Png(preview.Image));
            }
            if (worldAppearance is {} entered && login.Characters?.SingleOrDefault()?.Appearance is {} selected)
                Console.WriteLine(JsonSerializer.Serialize(new { status="appearance-check",
                    gender=selected.Gender==entered.Gender,skin=selected.Skin==entered.Skin,
                    hair=selected.HairStyle==entered.HairStyle,color=selected.HairColor==entered.HairColor,
                    equipment=selected.Equipment.SequenceEqual(entered.Equipment) }));
            // Deliberately omit character names, identifiers, raw packets and exception data.
            Console.WriteLine(JsonSerializer.Serialize(new { status = login.Status,
                characterCount = login.Characters?.Count, action = login.Action,
                family = login.Family, bytes = login.ResponseBytes, mapId = login.Welcome?.MapId, mapBytes = login.Welcome?.MapFileSize }));
            return login.Status == (enterWorld ? "world-entered" : selectCharacter ? "selection-accepted" : "login-accepted") && login.Characters != null ? 0 : 2;
        }
        finally { Array.Clear(account); Array.Clear(password); }
    }
    if (sessionCheck)
    {
        var session = await RechargedSession.CheckAsync(profile, identity, timeout.Token, watch);
        Console.WriteLine(JsonSerializer.Serialize(new { timestamp = DateTimeOffset.UtcNow, profile, session }, new JsonSerializerOptions { WriteIndented = true }));
        return session.Status is "session-response" or "synchronization-replied" or "session-observed" ? 0 : 2;
    }
    var result = await Handshake.ProbeAsync(profile, identity, timeout.Token);
    if (json) Console.WriteLine(JsonSerializer.Serialize(new { timestamp = DateTimeOffset.UtcNow, profile, result }, new JsonSerializerOptions { WriteIndented = true }));
    else Console.WriteLine($"{profile.Host}:{profile.Port} / {profile.Version}\n{result.Status}: {result.Message}\nReply: {result.ReplyCode}; bytes: {result.ResponseBytes}");
    return result.Status == "init-accepted" ? 0 : 2;
}
catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or FormatException or OverflowException or InvalidDataException or IOException or System.Net.Sockets.SocketException or OperationCanceledException)
{
    Console.Error.WriteLine(ex is OperationCanceledException ? "Connection timed out or was cancelled." : ex.Message);
    return 1;
}
