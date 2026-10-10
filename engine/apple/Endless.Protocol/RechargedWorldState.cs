using System.Text;
using System.Threading.Channels;
using System.Security.Cryptography;

namespace Endless.Apple.Protocol;

public sealed record WorldActor(int Id, string Name, int X, int Y, int Direction,
    int Gender, int Skin, int HairStyle, int HairColor, int Hp, int MaxHp,
    IReadOnlyList<int> Equipment)
{
    public long? AttackStartedMilliseconds { get; init; }
    public int? Tp { get; init; }
    public int? MaxTp { get; init; }
    public static IReadOnlyList<WorldActor> ParseAppearance(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 2 || packet[0] != 13 || packet[1] != 224)
            throw new InvalidDataException("Unexpected character appearance packet.");
        var result = new List<WorldActor>();
        var rest = packet[2..];
        while (!rest.IsEmpty)
        {
            var end = rest.IndexOf((byte)255);
            if (end is < 1 or > 32) throw new InvalidDataException("Invalid world character name field.");
            var name = Encoding.Latin1.GetString(rest[..end]);
            if (name.Any(char.IsControl)) throw new InvalidDataException("Invalid world character name.");
            rest = rest[(end+1)..];
            end = rest.IndexOf((byte)255);
            if (end != 52) throw new InvalidDataException("Unsupported world character appearance layout.");
            var data = rest[..end].ToArray();
            int N(int offset, int count = 1) => EoWire.Number(data.AsSpan(offset,count));
            var equipment = Enumerable.Range(0,9).Select(i => N(34+i*2,2)).ToArray();
            result.Add(new(N(0,2),name,N(2),N(3),N(10)&7,N(15)/124,N(15)%124,
                N(16),N(17),N(18,3),N(21,3),equipment) { Tp=N(24,3), MaxTp=N(27,3) });
            rest = rest[(end+1)..];
            if (result.Count > 1024) throw new InvalidDataException("Too many world characters.");
        }
        return result;
    }
}

public sealed class RechargedWorldState(int playerId)
{
    private readonly Dictionary<int,WorldActor> actors = new();
    private readonly Dictionary<int,WorldNpc> npcs = new();
    public IReadOnlyCollection<WorldNpc> Npcs => npcs.Values;
    private readonly Channel<int> moves = Channel.CreateBounded<int>(1);
    public Dictionary<(byte Action,byte Family),int> PacketCounts { get; } = new();
    public WorldInventory? Inventory { get; private set; }
    public bool InventoryUnsupported {get;private set;}
    public bool MovePending { get; private set; }
    private int? requestedDirection;
    private long lastAttack;
    public bool RequestAttack() => Entered && !MovePending && Player is {Hp: >0} && moves.Writer.TryWrite(4);
    public bool RequestMove(int direction) => Entered && !MovePending && direction is >= 0 and <= 3 && moves.Writer.TryWrite(direction);
    public bool Entered { get; private set; }
    public IReadOnlyCollection<WorldActor> Actors => actors.Values;
    public WorldActor? Player => actors.GetValueOrDefault(playerId);
    public bool IsOccupied(int x,int y) => actors.Values.Any(a=>a.Id!=playerId && a.X==x && a.Y==y)
        || npcs.Values.Any(n=>n.X==x && n.Y==y);
    private int[] missingPlayers = [];
    private int[] missingNpcs = [];

    public byte[]? TakeAppearanceRequest(RechargedSession session)
    {
        if (missingPlayers.Length + missingNpcs.Length == 0) return null;
        var packet = session.RequestAppearances(missingPlayers,missingNpcs);
        missingPlayers = []; missingNpcs = [];
        return packet;
    }

    public bool Apply(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 2) throw new InvalidDataException("Truncated world packet.");
        if(packet[0]==241 && packet[1]==226)
        {
            try { Inventory=WorldInventory.Parse(packet);InventoryUnsupported=false; }
            catch(InvalidDataException) { InventoryUnsupported=true; }
            return true;
        }
        if(packet[0]==21 && packet[1]==204)
        {
            // Official 0x50fbe1..0x50fd02 reads player short + direction char,
            // then 0x5aff84 changes direction without changing position.
            if(packet.Length<5)throw new InvalidDataException("Truncated player facing update.");
            var id=EoWire.Number(packet.Slice(2,2));
            var direction=EoWire.Number(packet.Slice(4,1));
            if(direction>3)throw new InvalidDataException("Invalid player facing direction.");
            if(!actors.TryGetValue(id,out var actor))return false;
            actors[id]=actor with {Direction=direction,AttackStartedMilliseconds=null};
            return true;
        }
        if(packet[0]==21 && packet[1]==102)
        {
            // Official 0x510060..0x5103cc consumes four-byte records:
            // player short, direction char, attack-kind char; 0x5b1c54 animates.
            if((packet.Length-2)%4!=0)throw new InvalidDataException("Truncated attack batch.");
            var updates=new List<WorldActor>();
            var now=Environment.TickCount64;
            for(var offset=2;offset<packet.Length;offset+=4)
            {
                var id=EoWire.Number(packet.Slice(offset,2));
                var direction=EoWire.Number(packet.Slice(offset+2,1));
                var kind=EoWire.Number(packet.Slice(offset+3,1));
                if(direction>3)throw new InvalidDataException("Invalid attack direction.");
                // Special/skill attacks require their own animation mapping.
                if(kind==0 && actors.TryGetValue(id,out var actor))
                    updates.Add(actor with {Direction=direction,AttackStartedMilliseconds=now});
            }
            foreach(var actor in updates)actors[actor.Id]=actor;
            return updates.Count>0;
        }
        if (packet[0] == 13 && packet[1] == 220)
        {
            var updates = WorldNpc.ParseAppearance(packet);
            if (npcs.Count + updates.Select(n => n.Id).Distinct().Count(id => !npcs.ContainsKey(id)) > 1024)
                throw new InvalidDataException("Too many world NPCs.");
            foreach (var npc in updates) npcs[npc.Id] = npc;
            return true;
        }
        if (packet[0] == 27 && packet[1] == 220)
        {
            // Official 0x515df0..0x515ee1: repeated short IDs, then remove each NPC.
            if ((packet.Length-2)%2 != 0) throw new InvalidDataException("Truncated NPC disappearance batch.");
            var ids = new List<int>();
            for (var offset=2; offset<packet.Length; offset+=2)
                ids.Add(EoWire.Number(packet.Slice(offset,2)));
            var changed = false;
            foreach (var id in ids) changed |= npcs.Remove(id);
            return changed;
        }
        if (packet[0] == 21 && packet[1] == 220)
        {
            // Official 0x514127: batches of ID short, X, Y, packed direction.
            if ((packet.Length-2)%5 != 0) throw new InvalidDataException("Truncated NPC movement batch.");
            var updates = new List<WorldNpc>();
            for (var offset=2; offset<packet.Length; offset+=5)
            {
                var id = EoWire.Number(packet.Slice(offset,2));
                var x = EoWire.Number(packet.Slice(offset+2,1));
                var y = EoWire.Number(packet.Slice(offset+3,1));
                var direction = EoWire.Number(packet.Slice(offset+4,1))%10;
                if (x > 249 || y > 249 || direction > 3) throw new InvalidDataException("Invalid NPC movement.");
                if (npcs.TryGetValue(id,out var npc)) updates.Add(npc with { X=x,Y=y,Direction=direction });
            }
            foreach (var npc in updates) npcs[npc.Id] = npc;
            return updates.Count > 0;
        }
        if (packet[0] == 13 && packet[1] == 224)
        {
            var updates = WorldActor.ParseAppearance(packet);
            if (actors.Count + updates.Count(a => !actors.ContainsKey(a.Id)) > 1024)
                throw new InvalidDataException("Too many world characters.");
            foreach (var actor in updates)
            {
                actors[actor.Id] = actor;
                if (actor.Id == playerId) MovePending = false;
            }
            return true;
        }
        // Official 0x50c8e5: the own-player reply starts with X, Y and a visibility mask,
        // followed by visibility-update lists. Other-player walking has a separate opcode.
        if (packet[0] == 2 && packet[1] == 206)
        {
            if (packet.Length < 5) throw new InvalidDataException("Truncated own-player movement reply.");
            var x = EoWire.Number(packet.Slice(2,1));
            var y = EoWire.Number(packet.Slice(3,1));
            var visibility = EoWire.Number(packet.Slice(4,1));
            if (x > 249 || y > 249 || visibility > 3)
                throw new InvalidDataException("Invalid own-player movement reply.");
            var lists = packet[5..];
            var visiblePlayers = new HashSet<int>();
            var visibleNpcs = new HashSet<int>();
            if (!lists.IsEmpty || visibility != 0)
            {
                for (var list=0;list<2;list++)
                {
                    var end = lists.IndexOf((byte)255);
                    if (end < 0 || end%2 != 0 || end > 2048) throw new InvalidDataException("Invalid world visibility list.");
                    var ids = list == 0 ? visiblePlayers : visibleNpcs;
                    for (var i=0;i<end;i+=2) ids.Add(EoWire.Number(lists.Slice(i,2)));
                    lists = lists[(end+1)..];
                }
            }
            missingPlayers = visiblePlayers.Where(id => id != playerId && !actors.ContainsKey(id)).ToArray();
            missingNpcs = visibleNpcs.Where(id => !npcs.ContainsKey(id)).ToArray();
            // 0x50cb7a / 0x50ccc6: each mask bit makes its list authoritative.
            if ((visibility&1) != 0)
                foreach (var id in actors.Keys.Where(id => id != playerId && !visiblePlayers.Contains(id)).ToArray()) actors.Remove(id);
            if ((visibility&2) != 0)
                foreach (var id in npcs.Keys.Where(id => !visibleNpcs.Contains(id)).ToArray()) npcs.Remove(id);
            if (Player is {} own)
            {
                var direction = requestedDirection ?? (x == own.X && y == own.Y+1 ? 0 :
                    x == own.X-1 && y == own.Y ? 1 : x == own.X && y == own.Y-1 ? 2 :
                    x == own.X+1 && y == own.Y ? 3 : own.Direction);
                actors[playerId] = own with { X=x,Y=y,Direction=direction };
            }
            requestedDirection = null;
            MovePending = false;
            return true;
        }
        if (packet[0] is 21 or 35 && packet[1] == 206)
        {
            if (packet.Length < 7) throw new InvalidDataException("Truncated walking response.");
            var id = EoWire.Number(packet.Slice(2,2));
            var direction = EoWire.Number(packet.Slice(4,1));
            var x = EoWire.Number(packet.Slice(5,1));
            // 0x50c082..0x50c0d8 skips one padding byte when X is divisible by three.
            var yOffset = packet[0] == 21 && x % 3 == 0 ? 7 : 6;
            // The desktop handler reads this prefix and tolerates trailing extension data.
            if (packet.Length < yOffset+1) throw new InvalidDataException("Truncated walking response.");
            var y = EoWire.Number(packet.Slice(yOffset,1));
            if (x > 249 || y > 249 || direction > 3) throw new InvalidDataException("Invalid walking direction.");
            if (actors.TryGetValue(id,out var actor)) actors[id] = actor with {X=x,Y=y,Direction=direction};
            if (id == playerId) MovePending = false;
            return true;
        }
        return false;
    }

    // One reader owns encoding transitions and all writes, including keepalive replies.
    public async Task RunAsync(Stream stream, RechargedSession session, RechargedWelcome welcome,
        Action<RechargedWorldState>? changed, CancellationToken token, Func<int,int,bool>? canWalk = null)
    {
        await stream.WriteAsync(session.EnterWorld(welcome),token);
        var reading = EoWire.ReadFrameAsync(stream,token);
        var command = moves.Reader.ReadAsync(token).AsTask();
        var noDeadline = new TaskCompletionSource().Task;
        Task moveDeadline = noDeadline;
        while (true)
        {
            var completed = await Task.WhenAny(reading,command,moveDeadline);
            if(completed==moveDeadline)
            {
                token.ThrowIfCancellationRequested();
                throw new IOException("The server did not confirm movement. Reconnect before trying again.");
            }
            if (completed == command)
            {
                var direction = await command;
                command = moves.Reader.ReadAsync(token).AsTask();
                if (!Entered || MovePending || Player is not { } player) continue;
                if (direction == 4)
                {
                    var now = Environment.TickCount64;
                    if (player.Hp <= 0 || now-lastAttack < 600) continue;
                    await stream.WriteAsync(session.Attack(player.Direction),token);
                    lastAttack = now;
                    actors[playerId]=player with {AttackStartedMilliseconds=now};
                    changed?.Invoke(this);
                    continue;
                }
                if (canWalk == null || !canWalk(player.X,player.Y)) continue;
                var x = player.X + (direction == 1 ? -1 : direction == 3 ? 1 : 0);
                var y = player.Y + (direction == 2 ? -1 : direction == 0 ? 1 : 0);
                if (!canWalk(x,y) || IsOccupied(x,y))
                {
                    if(player.Direction!=direction)
                    {
                        await stream.WriteAsync(session.Face(direction),token);
                        // Like the desktop turn path, update facing after the write.
                        // No displacement and no movement-confirmation deadline.
                        actors[playerId]=player with {Direction=direction};
                        changed?.Invoke(this);
                    }
                    continue;
                }
                await stream.WriteAsync(session.Walk(direction,x,y),token);
                MovePending = true;
                moveDeadline=Task.Delay(TimeSpan.FromSeconds(5),token);
                requestedDirection = direction;
                changed?.Invoke(this);
                continue;
            }
            var frame = await reading;
            reading = EoWire.ReadFrameAsync(stream,token);
            var body = session.Decode(frame);
            try
            {
                if (body.Length < 2) throw new InvalidDataException("Truncated world packet.");
                var opcode = (body[0],body[1]);
                PacketCounts[opcode] = PacketCounts.GetValueOrDefault(opcode)+1;
                if (body[0] == 21 && body[1] == 1)
                    await stream.WriteAsync(session.AnswerKeepalive(body),token);
                else if (RechargedSession.IsSynchronizationRequest(body))
                    await stream.WriteAsync(session.Synchronize(),token);
                else if (body[0] == 5 && body[1] == 30)
                {
                    if (Entered) throw new InvalidDataException("Duplicate world entry completion.");
                    session.CompleteWorldEntry(body);
                    await stream.WriteAsync(session.ConfirmWorld(),token);
                    Entered = true;
                    changed?.Invoke(this);
                }
                else if (Apply(body))
                {
                    if (TakeAppearanceRequest(session) is {} request)
                    {
                        try { await stream.WriteAsync(request,token); }
                        finally { CryptographicOperations.ZeroMemory(request); }
                    }
                    changed?.Invoke(this);
                }
            }
            finally { if(!MovePending)moveDeadline=noDeadline; CryptographicOperations.ZeroMemory(frame); CryptographicOperations.ZeroMemory(body); }
        }
    }
}
