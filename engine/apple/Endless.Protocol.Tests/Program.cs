using Endless.Apple.Protocol;
using Endless.Apple.World;
using System.Buffers.Binary;

var passed = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; }
void Reject(Action action, string name)
{
    try { action(); } catch (Exception e) when (e is ArgumentException or InvalidDataException) { passed++; return; }
    throw new Exception(name);
}
Check(EoWire.Number(0, 2).SequenceEqual(new byte[] {1, 254}), "zero golden vector");
Check(EoWire.Number(253, 2).SequenceEqual(new byte[] {1, 2}), "base-253 golden vector");
Check(EoWire.Number(64008, 2).SequenceEqual(new byte[] {253, 253}), "maximum short");
foreach (var n in new[] {0, 1, 252, 253, 254, 64008, 64009, 16194276, int.MaxValue})
    Check(EoWire.Number(EoWire.Number(n, 4)) == n, $"round trip {n}");
Reject(() => EoWire.Number(-1, 2), "negative");
Reject(() => EoWire.Number(253, 1), "overflow");
Reject(() => EoWire.Number(new byte[] {0, 1}), "zero byte");
Reject(() => EoWire.Number(new byte[] {254, 1}), "interior padding");
Reject(() => EoWire.Number(new byte[] {255}), "delimiter");
var profile = ServerProfile.Official;
const int challenge = 12345;
Check(Handshake.ChallengeHash(0) == 114000, "known challenge zero");
Check(Handshake.ChallengeHash(12345) == 266403, "known challenge vector");
Check(Handshake.ChallengeHash(11092110) == 11016, "maximum safe challenge");
Reject(() => Handshake.ChallengeHash(11092111), "oversized challenge");
var request = Handshake.Request(profile, challenge, "123456789", 2345);
Check(EoWire.Number(request.AsSpan(0, 2)) == request.Length - 2, "request frame");
Check(request[10] == 114, "Recharged marker 113");
Check(EoWire.Number(request.AsSpan(4, 3)) == 10000, "0.4.57 randomized prefix");
Check(EoWire.Number(request.AsSpan(11, 3)) == challenge, "0.4.57 actual challenge follows marker");
Check(request[14] == 10 && System.Text.Encoding.ASCII.GetString(request[15..]) == "123456789", "0.4.57 installation length and value");
var wrappedRequest = Handshake.Request(profile, 10, "123", 20);
Check(EoWire.Number(wrappedRequest.AsSpan(4, 3)) == (int)(4294967286u % 16194277u), "unsigned prefix wrap matches official encoder");
var classicRequest = Handshake.Request(profile with { Dialect = ProtocolDialect.Classic }, challenge, "123456789", 2345);
Check(classicRequest.Length == 21 && EoWire.Number(classicRequest.AsSpan(4, 3)) == challenge && classicRequest[10] == 113, "classic layout unchanged");
Reject(() => Handshake.Request(profile, 1_000_000, "123"), "Recharged challenge bound");
Reject(() => Handshake.Request(profile, challenge, "123", -1), "negative initialization noise");

byte[] Reply(ProtocolDialect dialect, int hash) => [255, 255, 2, 8, 9,
    .. EoWire.Number(6, dialect == ProtocolDialect.Recharged ? 2 : 1),
    .. EoWire.Number(7, dialect == ProtocolDialect.Recharged ? 2 : 1),
    .. EoWire.Number(42, 2), .. EoWire.Number(hash, 3),
    .. (dialect == ProtocolDialect.Recharged ? new byte[] {4} : Array.Empty<byte>())];
foreach (var dialect in Enum.GetValues<ProtocolDialect>())
{
    var reply = Reply(dialect, Handshake.ChallengeHash(challenge));
    Check(Handshake.Parse(reply, challenge, dialect).Status == (dialect == ProtocolDialect.Classic ? "init-accepted" : "init-received"), $"{dialect} init");
    Check(Handshake.Parse(Reply(dialect, 1), challenge, dialect).Status == (dialect == ProtocolDialect.Classic ? "challenge-mismatch" : "init-received"), "classic hash versus Recharged session field");
    Check(Handshake.Parse(reply[..^1], challenge, dialect).Status == "layout-mismatch", "truncation");
}
var session = new RechargedSession(Reply(ProtocolDialect.Recharged, 1), challenge);
Check(session.Accept().SequenceEqual(new byte[] {10,254,17,16,24,28,16,26,18,64,20}), "0.4.57 Connection/Accept golden bytes");
Check(RechargedSession.IsSynchronizationRequest([244,3,1]), "recognize current pre-login synchronization");
Check(!RechargedSession.IsSynchronizationRequest([244,3]) && !RechargedSession.IsSynchronizationRequest([244,1,1]), "reject unrelated or truncated synchronization");
Check(session.Synchronize().SequenceEqual(new byte[] {5,254,2,18,25,124}), "synchronization reply and sequence progression golden bytes");
byte[] ping = [21,1, .. EoWire.Number(5,1), .. EoWire.Number(112,4), .. EoWire.Number(3,1)];
Check(session.AnswerKeepalive(ping).SequenceEqual(new byte[] {5,254,255,16,35,124}), "keepalive updates sequence without resetting counter");
Reject(() => session.AnswerKeepalive([21,1,1,1,254,254,254,1]), "zero keepalive divisor");
Reject(() => session.AnswerKeepalive([21,1]), "truncated keepalive");
var selecting = new RechargedSession(Reply(ProtocolDialect.Recharged, 1), challenge);
var oldKey = 7 + challenge % 11;
var selection = selecting.SelectCharacter(123456, 199);
var selectionBody = RechargedSession.Transform(selection.AsSpan(2), oldKey, false);
Check(selectionBody.SequenceEqual((byte[])[35, 30, 8, .. EoWire.Number(123456, 4), 200]), "selection uses original key and current opcode");
var nextBody = RechargedSession.Transform(selecting.Synchronize().AsSpan(2), oldKey + 199, false);
Check(nextBody.SequenceEqual(new byte[] {244, 3, 9, 107}), "selection key transition preserves packet sequence");
Reject(() => selecting.SelectCharacter(0, 2), "reject missing character ID");
Reject(() => selecting.SelectCharacter(1, 200), "reject invalid selection modifier");
var entering = new RechargedSession(Reply(ProtocolDialect.Recharged,1),challenge);
var enterRequest = entering.EnterWorld(new RechargedWelcome(123,155,1,2,557) {SessionId=42});
Check(RechargedSession.Transform(enterRequest.AsSpan(2),oldKey,false).SequenceEqual((byte[])[12,30,8,..EoWire.Number(42,3),..EoWire.Number(123,4)]), "world entry golden layout");
entering.CompleteWorldEntry([5,30,3,4,1,254,254,1,254,254,1,255]);
Check(entering.Decode(RechargedSession.Transform([20,1,4],6+5+oldKey%13,true)).SequenceEqual(new byte[]{20,1,4}), "world entry incoming key transition");
byte[] appearanceData = [..EoWire.Number(42,2),5,6,..Enumerable.Repeat((byte)1,48)];
byte[] appearance = [13,224,..System.Text.Encoding.ASCII.GetBytes("Example"),255,..appearanceData,255];
var worldState = new RechargedWorldState(42);
Check(worldState.Apply(appearance) && worldState.Player is {X:4,Y:5,Name:"Example"}, "world appearance identifies own player and coordinates");
var manaData=(byte[])appearanceData.Clone();
EoWire.Number(25,3).CopyTo(manaData,24);EoWire.Number(100,3).CopyTo(manaData,27);
var manaActor=WorldActor.ParseAppearance([13,224,..System.Text.Encoding.ASCII.GetBytes("Example"),255,..manaData,255]).Single();
Check(manaActor.Tp==25 && manaActor.MaxTp==100,"appearance decodes current and maximum TP independently");
Reject(() => WorldActor.ParseAppearance(appearance[..^2]), "truncated appearance rejected");
Reject(() => WorldActor.ParseAppearance([13,224,255]), "empty actor name rejected");
Check(worldState.Apply([2,206,6,6,4,255,255]) && worldState.Player is {X:5,Y:5,Direction:3}, "own-player movement reply uses authoritative coordinates");
Reject(() => worldState.Apply([2,206,1,1]), "truncated own movement rejected");
Reject(() => worldState.Apply([2,206,1,1,5]), "invalid own movement visibility mask rejected");
Check(!worldState.Apply([20,1,4]) && worldState.Actors.Count==1, "unknown world packets preserve actors");
Check(worldState.Apply([21,102,..EoWire.Number(42,2),2,1]) && worldState.Player is {Direction:1,AttackStartedMilliseconds:not null}, "server basic attack broadcast updates facing and animation");
var beforeAttack=worldState.Player;
Reject(()=>worldState.Apply([21,102,..EoWire.Number(42,2),3,1,2]),"truncated attack batch rejected");
Check(worldState.Player==beforeAttack,"malformed attack batch leaves actor unchanged");
Check(!worldState.Apply([21,102,..EoWire.Number(42,2),3,2]),"unmapped skill attack does not use melee pose");
for(var direction=0;direction<4;direction++)
{
    var previous=worldState.Player!;
    Check(worldState.Apply([21,204,..EoWire.Number(42,2),..EoWire.Number(direction,1)]),"turn update handled");
    Check(worldState.Player is {} turned && turned.Direction==direction && turned.X==previous.X && turned.Y==previous.Y && turned.AttackStartedMilliseconds==null,"turn preserves tile and clears old swing");
}
var beforeFace=worldState.Player;
Reject(()=>worldState.Apply([21,204,1]),"truncated turn rejected");
Reject(()=>worldState.Apply([21,204,..EoWire.Number(42,2),5]),"invalid turn rejected");
Check(worldState.Player==beforeFace,"malformed turn preserves actor");
Check(!worldState.Apply([21,204,..EoWire.Number(43,2),1]) && worldState.Actors.Count==1,"turn never fabricates unknown actors");
var walker = new RechargedSession(Reply(ProtocolDialect.Recharged,1),challenge);
Check(RechargedSession.Transform(walker.Walk(2,4,5).AsSpan(2),oldKey,false).SequenceEqual(new byte[]{21,206,8,3,5,6}), "ordinary walking opcode and destination golden bytes");
Check(RechargedSession.Transform(walker.Attack(3).AsSpan(2),oldKey,false).SequenceEqual(new byte[]{35,102,9,4}), "basic attack opcode, sequence and facing golden bytes");
for(int direction=0;direction<4;direction++)
{
    var facingSession=new RechargedSession(Reply(ProtocolDialect.Recharged,1),challenge);
    Check(RechargedSession.Transform(facingSession.Face(direction).AsSpan(2),oldKey,false)
        .SequenceEqual(new byte[]{21,204,8,(byte)(direction+1)}),"independent turn packet for each isometric direction");
}
Reject(()=>walker.Face(-1),"reject negative facing direction");
Reject(()=>walker.Face(4),"reject invalid facing direction");
Reject(() => walker.Attack(4), "reject invalid attack direction");
Check(!worldState.RequestAttack(), "attack unavailable before world entry");
Reject(() => walker.Walk(4,1,1), "reject invalid walking direction");
Reject(() => walker.Walk(0,-1,1), "reject negative walking coordinate");
Check(worldState.Apply([35,206,..EoWire.Number(42,2),3,5,5]) && worldState.Player is {X:4,Y:4,Direction:2}, "server walking response changes position");
Check(worldState.Apply(appearance) && worldState.Player is {X:4,Y:5}, "server appearance corrects position");
Check(worldState.Apply([21,206,..EoWire.Number(42,2),3,5,6]) && worldState.Player is {X:4,Y:5}, "ordinary actor walking updates coordinates");
Check(worldState.Apply([21,206,..EoWire.Number(42,2),3,7,200,8]) && worldState.Player is {X:6,Y:7}, "walking skips conditional padding for X divisible by three");
Check(worldState.Apply([21,206,..EoWire.Number(42,2),3,5,6,255]) && worldState.Player is {X:4,Y:5}, "walking permits trailing extension data");
Reject(() => worldState.Apply([21,206,..EoWire.Number(42,2),3,7,8]), "walking requires conditional padding");
Reject(() => worldState.Apply([35,206,1,254]), "truncated walking response rejected");
Check(WorldPathfinder.NextDirection(5,5,1,1,3,1,(x,y)=>true)==3,"path heads toward open target");
Check(WorldPathfinder.NextDirection(5,5,1,1,3,1,(x,y)=>x!=2 || y!=1)==0,"path detours around an occupied tile");
Check(WorldPathfinder.NextDirection(5,5,1,1,3,1,(x,y)=>x!=2)==null,"path rejects unreachable target");
Check(WorldPathfinder.NextDirection(5,5,1,1,1,1,(x,y)=>true)==null,"path stops at target");
Check(WorldPathfinder.NextDirection(5,5,1,1,5,1,(x,y)=>true)==null,"path rejects outside map");
var tapCamera=new WorldCamera(13.25,27.75,320,640);
var tapPoint=tapCamera.Project(14,29);
Check(tapCamera.TileAt(tapPoint.X,tapPoint.Y)==(14,29),"map tap uses moving camera inverse projection");
Check(worldState.IsOccupied(4,5)==false,"own player is not a collision obstacle");
var collisionWorld=new RechargedWorldState(99);collisionWorld.Apply(appearance);
Check(collisionWorld.IsOccupied(4,5),"other player occupies destination");
collisionWorld.Apply([13,220,..EoWire.Number(5,2),..EoWire.Number(2,2),11,12,3,255]);
Check(collisionWorld.IsOccupied(10,11),"NPC occupies destination");
var sample = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();
Check(RechargedSession.Transform(RechargedSession.Transform(sample, 64018, true), 64018, false).SequenceEqual(sample), "packet transform all byte values and wrapping");
Reject(() => new RechargedSession([255,255,3,2], challenge), "reject acknowledgement before successful init");
Check(Handshake.Parse([255,255,1,1,5,58], challenge, ProtocolDialect.Recharged).RequiredVersion == "0.4.57", "version reply");
Reject(() => Handshake.Parse([255,255,1], challenge, ProtocolDialect.Recharged), "truncated version");
Reject(() => Handshake.Parse([1,2,3], challenge, ProtocolDialect.Recharged), "wrong packet");
var rejection = Handshake.Parse([255,255,3,2, .. System.Text.Encoding.ASCII.GetBytes("Client update required."), 0, 27], challenge, ProtocolDialect.Recharged);
Check(rejection.Status == "rejected", "rejection stays a failure");
Check(rejection.ServerMessage == "Client update required.", "readable rejection without control bytes");
Check(Handshake.Parse([255,255,3,2], challenge, ProtocolDialect.Recharged).ServerMessage == null, "binary rejection is not a message");
var body1 = new byte[] {255,255,3,2};
var body2 = new byte[] {255,255,1,1,5,58};
using var stream = new FragmentedStream([.. EoWire.Frame(body1), .. EoWire.Frame(body2)]);
Check((await EoWire.ReadFrameAsync(stream, default)).SequenceEqual(body1), "fragmented frame");
Check((await EoWire.ReadFrameAsync(stream, default)).SequenceEqual(body2), "coalesced next frame");
try { await EoWire.ReadFrameAsync(new MemoryStream([5,254,255]), default); throw new Exception("missing EOF"); }
catch (EndOfStreamException) { passed++; }
using var cts = new CancellationTokenSource(); cts.Cancel();
try { await EoWire.ReadFrameAsync(new MemoryStream(EoWire.Frame(body1)), cts.Token); throw new Exception("missing cancellation"); }
catch (OperationCanceledException) { passed++; }
Reject(() => EoWire.Number(new byte[] {253,253,253,253}), "oversized server integer becomes protocol error");
Check(System.Text.Encoding.ASCII.GetString(RechargedCredentials.Encode("ABC", 0)) == "CRCGAFH", "credential base24 vector including every third byte");
Reject(() => RechargedCredentials.Encode("é", 123), "reject unsupported credential encoding");
Reject(() => RechargedCredentials.Encode(new string('a', 17), 123), "credential bound");
passed += await SessionFlowTests.RunAsync();
Check(WorldMap.Expand(new byte[] {7,8,255,2,5}).SequenceEqual(new byte[] {7,8,7,8,7,8,7}), "overlapping map back-reference");
Reject(() => WorldMap.Expand(new byte[] {255,1,2}), "back-reference before map start");
Reject(() => WorldMap.Expand(new byte[] {1,255,1}), "truncated map back-reference");
Check(WorldMap.ExpandDictionary([0,65,0,66,1,0,1,2]).SequenceEqual("ABABABA"u8.ToArray()), "dictionary map handles self-referential code");
Reject(() => WorldMap.ExpandDictionary([0,65,1]), "truncated map dictionary code");
Reject(() => WorldMap.ExpandDictionary([1,0]), "invalid first map dictionary code");
Reject(() => WorldMap.ExpandDictionary([0,65,1,1]), "unknown map dictionary code");
Reject(() => WorldMap.Load(new byte[135]), "bad map header");
var emptyMap = Enumerable.Repeat((byte)1,164).ToArray();
"EMF"u8.CopyTo(emptyMap); emptyMap[8]=5;
emptyMap[14]=255;
var parsedMap = WorldMap.Load(emptyMap);
Check(parsedMap.Width == 1 && parsedMap.Height == 1 && parsedMap.Layers.All(l => l.Count == 0), "empty current map fixture");
Reject(() => WorldMap.Load(emptyMap[..^1]), "truncated graphic layers");
var outsideMap = emptyMap.Concat(new byte[] {1,1,1,1,1}).ToArray();
outsideMap[150]=2; outsideMap[151]=2; outsideMap[152]=1;
Reject(() => WorldMap.Load(outsideMap), "out-of-bounds map row");
var raisedMap = emptyMap[..153].Concat(new byte[] {2,1,2,1,2,254,9}).Concat(emptyMap[154..]).ToArray();
Check(WorldMap.Load(raisedMap).Layers[3].Single() is {Graphic:1,Height:8}, "raised tile consumes extra elevation byte");
var dib = new byte[56];
BinaryPrimitives.WriteInt32LittleEndian(dib,40);
BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(4),2);
BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(8),1);
BinaryPrimitives.WriteUInt16LittleEndian(dib.AsSpan(12),1);
BinaryPrimitives.WriteUInt16LittleEndian(dib.AsSpan(14),16);
BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(16),3);
BinaryPrimitives.WriteUInt32LittleEndian(dib.AsSpan(40),0x7c00);
BinaryPrimitives.WriteUInt32LittleEndian(dib.AsSpan(44),0x03e0);
BinaryPrimitives.WriteUInt32LittleEndian(dib.AsSpan(48),0x001f);
BinaryPrimitives.WriteUInt16LittleEndian(dib.AsSpan(52),0x7c00);
var raster = Raster.FromDib(dib);
Check(raster.Rgba.SequenceEqual(new byte[] {255,0,0,255,0,0,0,0}), "RGB555 red and transparent black");
Reject(() => Raster.FromDib(dib.AsSpan(0,55)), "truncated bitmap pixels");
var dressed = new WorldActor(1,"Fixture",1,1,0,1,0,2,3,1,1,[2,2,0,0,0,0,0,0,0]);
var clothes = CharacterAppearance.Standing(dressed);
Check(clothes.Select(l=>l.Archive).SequenceEqual(new[]{12,11,14,16,12}), "current standing clothing layer order");
Check(clothes[0].Graphic == 61 && clothes[4].Graphic == 62 && clothes[2].Graphic == 41 && clothes[3].Graphic == 51, "current hair stride and equipment resources");
var backClothes = CharacterAppearance.Standing(dressed with { Direction=2,Gender=0 });
Check(backClothes[0].Archive==13 && backClothes[0].Graphic==63 && backClothes[2].Graphic==42 && backClothes.All(l=>l.Flip), "back-facing female archive and mirrored layers");
Check(CharacterAppearance.Standing(dressed with { HairStyle=0,Equipment=[] }).Count==1, "unequipped character preserves body only");
Check(CharacterAppearance.Standing(dressed with { Skin=16 }).Count==0, "unsupported body avoids invalid source reads");
var itemHeader = new byte[13];
"EIF"u8.CopyTo(itemHeader); itemHeader[10]=5; itemHeader[11]=58;
var itemData = Enumerable.Repeat((byte)1,78).ToArray();
itemData[3]=11; EoWire.Number(17,3).CopyTo(itemData,47);
var catalog = new ItemCatalog();
catalog.Append([..itemHeader,2,(byte)'r',1,1,254,1,2,(byte)'A',..itemData]);
Check(catalog.Count==2 && catalog.Find(1)?.Graphic==0 && catalog.Find(2)?.Graphic==17, "reserved item records preserve sequential item IDs");
Check(catalog.Find(0)==null && catalog.Find(3)==null, "unknown equipment IDs have no artwork");
Reject(() => catalog.Append([..itemHeader,2,(byte)'A',..itemData[..77]]), "truncated item table rejected");
Check(catalog.Count==2, "failed item append preserves catalog");
var npcHeader = (byte[])itemHeader.Clone(); "ENF"u8.CopyTo(npcHeader);
var npcRecord = Enumerable.Repeat((byte)1,85).ToArray();
EoWire.Number(7,2).CopyTo(npcRecord,1);
var npcCatalog = new NpcCatalog();
npcCatalog.Append([..npcHeader,2,(byte)'r',1,1,254,2,(byte)'A',..npcRecord]);
Check(npcCatalog.Count==2 && npcCatalog.Graphic(1)==0 && npcCatalog.Graphic(2)==7, "current NPC table records and reserved IDs");
Reject(() => npcCatalog.Append([..npcHeader,2,(byte)'A',..npcRecord[..84]]), "truncated NPC table rejected");
Check(npcCatalog.Count==2, "NPC catalog append is transactional");
var npcState = new RechargedWorldState(99);
byte[] npcAppearance = [13,220,..EoWire.Number(5,2),..EoWire.Number(2,2),11,12,3,255];
Check(npcState.Apply(npcAppearance) && npcState.Npcs.Single() == new WorldNpc(5,2,10,11,2), "NPC appearance fields");
Check(npcState.Apply([21,220,..EoWire.Number(5,2),12,12,14]) && npcState.Npcs.Single().X==11 && npcState.Npcs.Single().Direction==3, "NPC movement strips packed action flags");
Reject(() => npcState.Apply([21,220,..EoWire.Number(5,2),13,12,2,..EoWire.Number(5,2),14,12,5]), "invalid second NPC movement rejected");
Check(npcState.Npcs.Single().X==11, "malformed NPC batch does not partially apply");
Reject(() => WorldNpc.ParseAppearance(npcAppearance[..^1]), "NPC appearance requires delimiter");
Check(npcState.Apply([2,206,1,1,1,255,255]) && npcState.Npcs.Count==1, "zero visibility mask preserves known NPCs");
Reject(() => npcState.Apply([2,206,1,1,3,255,2,255]), "odd NPC visibility list rejected");
Check(npcState.Npcs.Count==1, "invalid visibility leaves scene intact");
Check(npcState.Apply([2,206,1,1,3,255,255]) && npcState.Npcs.Count==0, "authoritative empty NPC list removes departed entities");
var visibilitySession = new RechargedSession(Reply(ProtocolDialect.Recharged,1),challenge);
npcState.Apply(npcAppearance);
npcState.Apply([2,206,1,1,4,..EoWire.Number(99,2),..EoWire.Number(7,2),255,
    ..EoWire.Number(5,2),..EoWire.Number(6,2),..EoWire.Number(6,2),255]);
var visibilityRequest = npcState.TakeAppearanceRequest(visibilitySession)!;
Check(RechargedSession.Transform(visibilityRequest.AsSpan(2),oldKey,false).SequenceEqual(
    (byte[])[1,218,8,2,..EoWire.Number(7,2),255,..EoWire.Number(6,2)]),
    "visibility requests unknown actors and NPCs once, excluding self and known entities");
Check(npcState.TakeAppearanceRequest(visibilitySession)==null,"appearance request consumed once");
npcState.Apply([13,220,..EoWire.Number(6,2),..EoWire.Number(2,2),11,12,3,255]);
npcState.Apply([2,206,1,1,3,255,..EoWire.Number(5,2),..EoWire.Number(6,2),255]);
Check(npcState.Npcs.Count==2 && npcState.TakeAppearanceRequest(visibilitySession)==null,
    "new NPC appears and is not requested again after response");
Reject(()=>visibilitySession.RequestAppearances([],[]),"empty visibility request rejected");
Reject(()=>visibilitySession.RequestAppearances([], [64009]),"oversized entity ID rejected");
npcState.Apply(npcAppearance);
Reject(()=>npcState.Apply([27,220,..EoWire.Number(5,2),1]),"NPC removal batch rejects partial ID");
Check(npcState.Npcs.Any(n=>n.Id==5),"bad disappearance batch leaves NPC collision intact");
Check(npcState.Apply([27,220,..EoWire.Number(5,2)]) && !npcState.Npcs.Any(n=>n.Id==5),"NPC disappearance removes entity");
Check(!npcState.Apply([27,220,..EoWire.Number(5,2)]),"duplicate NPC removal is harmless");
var fullEquipment = CharacterAppearance.Standing(dressed with { Equipment=[1,1,1,1,1] });
Check(fullEquipment.Any(l=>l.Archive==18) && fullEquipment[0].Archive==20 && fullEquipment[^1].Archive==22, "standing hat weapon and shield composition");
Check(fullEquipment.Single(l=>l.Archive==18).Height==null, "hat includes full artwork height");
var motion = new WorldMotion();
motion.Update([(false,1,5,5),(true,1,7,7)],0);
motion.Update([(false,1,6,5),(true,1,7,7)],1);
Check(motion.Position(false,1,1)==new ScenePosition(5,5), "confirmed movement starts at prior visual position");
Check(motion.Position(false,1,1.125)==new ScenePosition(5.5,5), "confirmed movement interpolates halfway");
Check(motion.Position(false,1,2)==new ScenePosition(6,5), "movement clamps at server destination");
Check(motion.Position(true,1,1.125)==new ScenePosition(7,7), "NPC and player IDs are separate");
motion.Update([(false,1,20,20)],3);
Check(motion.Position(false,1,3)==new ScenePosition(20,20), "teleports snap instead of crossing scenery");
var walkingLayers = CharacterAppearance.Walking(dressed,2);
Check(walkingLayers.Single(l=>l.Archive==11) is {Graphic:2,SourceX:260,Width:26,Height:61}, "walking body selects gender and step frame");
Check(walkingLayers.Single(l=>l.Archive==14).Graphic==45, "walking boots use four-frame sequence");
Check(CharacterAppearance.Walking(dressed with {Direction=2},2).Single(l=>l.Archive==14).Graphic==49, "walking boots select rear sequence");
Check(motion.WalkingFrame(false,1,3)==null, "teleports do not animate a walk");
var flowing=new WorldMotion();
flowing.Update([(false,1,0,0)],0);
flowing.Update([(false,1,1,0)],1);
var joint=flowing.Position(false,1,1.3);
flowing.Update([(false,1,2,0)],1.3);
Check(flowing.Position(false,1,1.3)==joint,"successive confirmed steps preserve position continuity");
Check(flowing.WalkingFrame(false,1,1.3)==2,"successive steps preserve walking cycle instead of restarting");
Check(flowing.Position(false,1,1.45).X>1 && flowing.Position(false,1,1.45).X<2,"movement adapts to confirmed cadence without prediction");
for(int facing=0;facing<4;facing++)
{
    var equipment=new int[9];equipment[3]=1;
    var slash=WeaponSlash.Layer(dressed with {Direction=facing,Equipment=equipment},null);
    Check(slash is {Archive:3,Graphic:8,Width:58,Height:58,Opacity:96} && slash.SourceX==facing*58 && slash.SourceY==174,"original directional axe slash frame");
}
Check(WeaponSlash.Layer(dressed with {Equipment=new int[9]},null)==null,"unarmed attacks have no weapon slash");
var camera = new WorldCamera(100,200);
Check(camera.Project(100,200) == (320,240), "camera centers player on large maps");
Check(camera.Project(101,200) == (352,256) && camera.Project(100,201) == (288,256), "camera preserves isometric movement directions");
Check(new WorldCamera(101,200).Project(101,200) == (320,240), "camera follows player without changing viewport");
var png = WorldRenderer.Png(raster);
Check(png.Take(8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}), "PNG signature");
using (var compressedPixels = new MemoryStream(png,41,BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(33)),false))
using (var z = new System.IO.Compression.ZLibStream(compressedPixels,System.IO.Compression.CompressionMode.Decompress))
using (var decoded = new MemoryStream())
{
    z.CopyTo(decoded);
    Check(decoded.ToArray().SequenceEqual(new byte[] {0}.Concat(raster.Rgba)), "PNG pixel round trip");
}
if (args.Length == 2 && args[0] == "--assets")
{
    var root = Path.GetFullPath(args[1]);
    var art = new WorldArtwork(root);
    var archives = new[] {4,25,5,6,7,8,4,7,9,9,7,10,8,7};
    var referenced = new HashSet<(int Archive,int Graphic)>();
    var mapCount = 0;
    foreach (var path in Directory.GetFiles(Path.Combine(root,"data","maps"),"*.emf"))
    {
        var map = WorldMap.Load(File.ReadAllBytes(path));
        for (var layer=0;layer<14;layer++)
            foreach (var tile in map.Layers[layer])
                if (tile.Graphic>0) referenced.Add((archives[layer],tile.Graphic));
        if (map.FillTile>0) referenced.Add((4,map.FillTile));
        var rendered = WorldRenderer.Render(map,root,camera:new WorldCamera((map.Width-1)/2.0,(map.Height-1)/2.0),artwork:art);
        Check(rendered.Rgba.Length==640*480*4,"cached map renders bounded viewport");
        mapCount++;
    }
    foreach (var (archive,graphic) in referenced)
        Check(art.Archive(archive).OptionalBitmap(graphic)!=null,"cached map artwork exists");
    Check(art.Items.Count>0 && art.Npcs.Count>0,"current equipment and NPC catalogs load");
    
Console.WriteLine($"Asset coverage: {mapCount} maps rendered; {referenced.Count} scenery resources checked.");
}
foreach(var gender in new[]{0,1})foreach(var direction in new[]{0,1,2,3})foreach(var frame in new[]{0,1})
{
    var actor=new WorldActor(1,"Example",1,1,direction,gender,15,0,0,10,10,new int[9]);
    var body=CharacterAppearance.Attacking(actor,frame).Single(l=>l.Archive==11);
    Check(body.Graphic==3 && body.SourceX+body.Width<=192 && body.SourceY+body.Height<=928,
        "attack body crop stays inside original sheet for every gender/facing/frame");
}

// Regression: body/equipment must share the official composite-canvas anchors.
foreach(var gender in new[]{0,1})foreach(var direction in new[]{0,1,2,3})
{
    var actor=new WorldActor(1,"Equipped",1,1,direction,gender,0,1,0,10,10,new[]{1,1,1,1,1,0,0,0,0});
    var windup=CharacterAppearance.Attacking(actor,0);
    var strike=CharacterAppearance.Attacking(actor,1);
    var standing=CharacterAppearance.Standing(actor);
    Check(windup.Single(l=>l.Archive==11).Left==-13-gender && strike.Single(l=>l.Archive==11).Left==-11+gender,"official body anchors differ for wind-up and strike");
    var boots=14+(gender==1?0:1);
    Check(windup.Single(l=>l.Archive==boots).Graphic==standing.Single(l=>l.Archive==boots).Graphic && strike.Single(l=>l.Archive==boots).Graphic==standing.Single(l=>l.Archive==boots).Graphic+10,"boots change pose only on strike");
    var hair=12+(gender==1?0:1);var back=direction is 1 or 2;
    Check(strike.First(l=>l.Archive==hair).Left==-17+(back?2*gender:0),"head follows official strike anchor in every facing");
    var weaponArchive=20+(gender==1?0:1);
    Check(strike.Count(l=>l.Archive==weaponArchive)==(back?1:2),"front melee strike includes separate foreground weapon part");
    if(!back)Check(strike.Last().Archive==weaponArchive && strike.Last().Graphic==17,"foreground strike renders above armor and shield");

}

// Missing/blank weapon frames must not erase the equipped weapon; valid split
// poses must remain untouched, even if one of their two parts is blank.
foreach(var direction in new[]{0,1,2,3})
{
    var actor=new WorldActor(1,"Axe",0,0,direction,1,0,0,0,10,10,new[]{0,0,0,11,0});
    var back=direction is 1 or 2;
    var windupGraphic=1013+(back?2:0);
    var fallback=CharacterAppearance.Attacking(actor,1,null,l=>l.Graphic==windupGraphic);
    Check(fallback.Single(l=>l.Archive==20).Graphic==windupGraphic,"blank strike retains available axe wind-up in every direction");
    var standingFallback=CharacterAppearance.Attacking(actor,1,null,l=>l.Graphic==1001+(back?1:0));
    Check(standingFallback.Single(l=>l.Archive==20).Graphic==1001+(back?1:0),"missing whole attack uses held weapon pose");
    var original=CharacterAppearance.Attacking(actor,1);
    var valid=CharacterAppearance.Attacking(actor,1,null,l=>l.Graphic==(back?1016:1017));
    Check(original.SequenceEqual(valid),"valid strike preserves split layer order even with blank rear part");
}

// Current-format synthetic bow + quiver records exercise asset selection without local assets.
var bowRecord=Enumerable.Repeat((byte)1,78).ToArray();bowRecord[3]=11;bowRecord[4]=2;EoWire.Number(120,3).CopyTo(bowRecord,47);
var quiverRecord=(byte[])bowRecord.Clone();quiverRecord[3]=12;quiverRecord[4]=6;EoWire.Number(14,3).CopyTo(quiverRecord,47);
var rangedCatalog=new ItemCatalog();rangedCatalog.Append([..itemHeader,2,(byte)'B',..bowRecord,2,(byte)'Q',..quiverRecord]);
foreach(var gender in new[]{0,1})foreach(var direction in new[]{0,1,2,3})
{
    var actor=new WorldActor(1,"Archer",1,1,direction,gender,0,0,0,10,10,new[]{0,0,0,1,2});
    var shooting=CharacterAppearance.Attacking(actor,0,rangedCatalog);
    var body=shooting.Single(l=>l.Archive==11);
    Check(body.Graphic==7 && body.SourceX+body.Width<=100 && body.SourceY+body.Height<=928,"ranged body uses valid shooting sheet crop");
    Check(shooting.Single(l=>l.Archive is 20 or 21).Graphic==11918+(direction is 1 or 2?1:0),"bows use existing shooting resources rather than melee resources");
    Check(shooting.Single(l=>l.Archive is 22 or 23).Graphic==653+(direction is 1 or 2?1:0),"quiver uses attack pose");
    Check(CharacterAppearance.Attacking(actor,1,rangedCatalog).SequenceEqual(CharacterAppearance.Standing(actor,rangedCatalog)),"ranged recovery restores coordinated standing layers");
    Check(WeaponSlash.Layer(actor,rangedCatalog)==null,"ranged attacks do not display melee slash");
}

byte[] inventoryPacket=[241,226,..EoWire.Number(12,2),..EoWire.Number(70,2),..EoWire.Number(5,2),1,1,1,1,1,1,1,1,..EoWire.Number(9,4)];
var bag=WorldInventory.Parse(inventoryPacket);
Check(bag.Weight==12 && bag.Capacity==70 && bag.Entries.Single()==new InventoryEntry(5,9),"inventory snapshot reads weight and item quantity");
Check(worldState.Apply(inventoryPacket) && worldState.Inventory?.Entries.Single()==new InventoryEntry(5,9),"inventory snapshot applies atomically");
var previousBag=worldState.Inventory;
Reject(()=>WorldInventory.Parse(inventoryPacket[..^1]),"truncated inventory record rejected");
Check(worldState.Apply(inventoryPacket[..^1]) && worldState.InventoryUnsupported,"unsupported bag response does not abort world session");
Check(ReferenceEquals(previousBag,worldState.Inventory),"invalid inventory leaves prior contents intact");
Check(WorldInventory.Parse([241,226,1,254,1,254]).Entries.Count==0,"empty bag snapshot supported");
// Current item-table graphics/footprints differ from legacy EIF offsets.
var iconRecord=(byte[])itemData.Clone();EoWire.Number(362,2).CopyTo(iconRecord,1);EoWire.Number(1,1).CopyTo(iconRecord,74);
var iconsCatalog=new ItemCatalog();iconsCatalog.Append([..itemHeader,2,(byte)'S',..iconRecord]);
Check(iconsCatalog.Find(1) is {InventoryGraphic:362,InventorySize:1} && iconsCatalog.Find(1)!.Footprint==(1,2),"current inventory graphic and footprint fields");
for(int size=0;size<8;size++)Check((new ItemAppearance(0,0,0){InventorySize=size}).Footprint==(size/4+1,size%4+1),"multi-cell footprint mapping");
var footprints=new[]{(2,4),(1,3),(2,2),(1,1),(2,1),(1,4)};
foreach(int columns in new[]{2,5,8})
{
    var placements=InventoryLayout.Pack(footprints,columns);var cells=new HashSet<(int,int)>();
    Check(placements.Count==footprints.Length,"packing preserves each inventory entry");
    foreach(var p in placements){Check(p.X>=0&&p.X+p.Width<=columns,"packed footprint stays inside columns");for(int y=0;y<p.Height;y++)for(int x=0;x<p.Width;x++)Check(cells.Add((p.X+x,p.Y+y)),"multi-cell inventory items do not overlap");}
}
Check(InventoryLayout.Pack([],8).Count==0,"empty inventory layout");
Reject(()=>InventoryLayout.Pack([(3,1)],8),"unsupported footprint rejected");
if (args.Length==2 && args[0]=="--weapon-assets")
{
    var artwork=new WorldArtwork(Path.GetFullPath(args[1]));
    foreach(var id in Enumerable.Range(1,artwork.Items.Count)
        .Where(i=>artwork.Items.Find(i) is {Type:10,Graphic:>0})
        .DistinctBy(i=>artwork.Items.Find(i)!.Graphic))
    foreach(var gender in new[]{0,1})foreach(var direction in new[]{0,1,2,3})foreach(var frame in new[]{0,1})
    {
        var actor=new WorldActor(1,"Fixture",0,0,direction,gender,0,0,0,10,10,new[]{0,0,0,id,0});
        bool Visible(CharacterLayer l)=>artwork.Archive(l.Archive).HasVisibleBitmap(l.Graphic);
        // No fallback can manufacture an absent held sprite (e.g. current Lasergun).
        if (!CharacterAppearance.Standing(actor,artwork.Items).Any(l=>l.Archive is 20 or 21 && Visible(l))) continue;
        var pose=CharacterAppearance.Attacking(actor,frame,artwork.Items,Visible);
        Check(pose.Any(l=>l.Archive is 20 or 21 && Visible(l)),
            $"{artwork.Items.Find(id)!.Name}: visible weapon resource g{gender} d{direction} f{frame}");
    }
}

var panCamera=new CameraPan();
panCamera.Drag(64,0);
Check(panCamera.X==-1 && panCamera.Y==1,"horizontal drag inverts diamond projection");
panCamera.Drag(0,32);
Check(panCamera.X==-2 && panCamera.Y==0,"vertical drag inverts diamond projection");
var draggedCamera=new WorldCamera(10+panCamera.X,10+panCamera.Y,320,480);
Check(draggedCamera.Project(10,10)==(224,272),"scene follows finger by exact rendered pixel translation");
Check(draggedCamera.TileAt(224,272)==(10,10),"panned camera picking returns visible character tile");
panCamera.Drag(double.NaN,0);
Check(panCamera.X==-2 && panCamera.Y==0,"invalid pan input is ignored");
panCamera.Drag(100000,100000);
Check(Math.Abs(panCamera.X)<=32 && Math.Abs(panCamera.Y)<=32,"pan stays bounded");
panCamera.Reset();
Check(panCamera.X==0 && panCamera.Y==0,"attack recenter resets camera displacement");

Console.WriteLine($"PASS: {passed} protocol and graphics assertions (offline; no account or network required).");

sealed class FragmentedStream(byte[] bytes) : MemoryStream(bytes)
{
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => base.ReadAsync(buffer[..Math.Min(1, buffer.Length)], cancellationToken);
}
