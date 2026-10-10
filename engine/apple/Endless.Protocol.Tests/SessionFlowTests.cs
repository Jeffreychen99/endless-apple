using Endless.Apple.Protocol;

internal static class SessionFlowTests
{
    public static async Task<int> RunAsync()
    {
        var assertions = 0;
        void Check(bool condition, string description)
        {
            if (!condition) throw new Exception(description);
            assertions++;
        }

        // Fragment every read. Writes are kept separately from simulated server input.
        byte[] initial = [255,255,2,8,9,7,254,8,254,43,254,2,254,254,4];
        byte[] Server(byte[] body) => EoWire.Frame(RechargedSession.Transform(body, 6, true));
        using (var stream = new DuplexFixture([.. EoWire.Frame(initial), .. Server([244,3,1]), .. Server([21,1,6,113,254,254,254,4])]))
        {
            var result = await RechargedSession.CheckStreamAsync(stream, ServerProfile.Official, "123456789", default, watch: true);
            Check(result.Status == "session-closed", "EOF after keepalive must not claim a sustained connection");
            Check(stream.Writes.Count == 4, "init, acknowledgement, synchronization and keepalive only");
            var challenge = EoWire.Number(stream.Writes[0].AsSpan(11, 3));
            var parameter = 7 + challenge % 11;
            byte[] Body(int index) => RechargedSession.Transform(stream.Writes[index].AsSpan(2), parameter, false);
            Check(Body(1)[..3].SequenceEqual(new byte[] {3,1,8}), "acknowledgement begins sequence at seven");
            Check(Body(2).SequenceEqual(new byte[] {244,3,9,107}), "synchronization uses next sequence");
            Check(Body(3).SequenceEqual(new byte[] {241,1,19,107}), "keepalive sequence base replaces old base and preserves counter");
        }
        using (var stream = new DuplexFixture(EoWire.Frame([255,255,3,2])))
        {
            var result = await RechargedSession.CheckStreamAsync(stream, ServerProfile.Official, "123456789", default);
            Check(result.Status == "rejected" && stream.Writes.Count == 1, "init rejection sends no acknowledgement");
        }
        using (var stream = new DuplexFixture([.. EoWire.Frame(initial), .. Server([99,99,1])]))
        {
            var result = await RechargedSession.CheckStreamAsync(stream, ServerProfile.Official, "123456789", default, watch: true);
            Check(result.Status == "session-response" && stream.Writes.Count == 2, "unknown packet is reported without speculative reply");
        }
        using (var stream = new DuplexFixture([.. EoWire.Frame(initial), .. Server([21,1,1,1,254,254,254,1])]))
        {
            try
            {
                await RechargedSession.CheckStreamAsync(stream, ServerProfile.Official, "123456789", default, watch: true);
                throw new Exception("malformed keepalive was accepted");
            }
            catch (InvalidDataException) { Check(stream.Writes.Count == 2, "invalid keepalive sends no reply"); }
        }
        using (var stream = new DuplexFixture([.. EoWire.Frame(initial), .. Server([244,3,1]), .. Server([244,3,1]), .. Server([35,3,65,255])]))
        {
            var account = "testuser".ToCharArray();
            var secret = "TestOnly123".ToCharArray();
            var result = await RechargedLogin.TestStreamAsync(stream, ServerProfile.Official, "123456789", account, secret, default);
            Check(result.Status == "login-response" && result.Action == 35 && result.Family == 3, "login reports metadata without claiming success");
            Check(stream.Writes.Count == 5, "repeated synchronization does not resend credentials");
            var challenge = EoWire.Number(stream.Writes[0].AsSpan(11, 3));
            var login = RechargedSession.Transform(stream.Writes[3].AsSpan(2), 7 + challenge % 11, false);
            Check(login[..3].SequenceEqual(new byte[] {35,2,10}), "login opcode and sequence follow synchronization");
            byte[] expected = [.. RechargedCredentials.Encode("testuser", 123), 255,
                .. RechargedCredentials.Encode("TestOnly123", unchecked((byte)(challenge % 250 + 136))), 255];
            Check(login[3..].SequenceEqual(expected), "login uses initialization-derived password parameter and delimiters");
            Check(account.All(c => c == 0) && secret.All(c => c == 0), "credentials cleared after submission");
        }
        using (var stream = new DuplexFixture(EoWire.Frame([255,255,3,2])))
        {
            var account = "testuser".ToCharArray();
            var secret = "TestOnly123".ToCharArray();
            var result = await RechargedLogin.TestStreamAsync(stream, ServerProfile.Official, "123456789", account, secret, default);
            Check(result.Status == "rejected" && stream.Writes.Count == 1, "rejected initialization never submits login");
            Check(secret.All(c => c == 0), "credentials cleared on rejection");
        }
        using (var stream = new DuplexFixture(EoWire.Frame([3,2,2,254])))
        {
            var account = "testuser".ToCharArray();
            var secret = "TestOnly123".ToCharArray();
            var result = await RechargedLogin.TestStreamAsync(stream, ServerProfile.Official, "123456789", account, secret, default);
            Check(result.Status == "invalid-initialization", "unexpected first packet is a recoverable result");
            Check(stream.Writes.Count == 1 && !result.AcknowledgementSent, "unexpected initialization never sends credentials or acknowledgement");
            Check(account.All(c => c == 0) && secret.All(c => c == 0), "unexpected initialization clears credentials");
        }
        byte[] accepted = [3,2,2,254,2,254,2,254,13,2,254,2,254,2,254,255];
        Check(RechargedLoginReply.Parse(accepted)?.Status == "login-accepted", "accepted reply requires result one and complete numeric header");
        Check(RechargedLoginReply.Parse([3,2,3,254])?.Status == "login-unrecognized-result", "other result codes do not imply success");
        Check(RechargedLoginReply.Parse([99,99]) == null, "unrelated reply is not interpreted as login");
        foreach (var malformed in new byte[][] { [3,2], [3,2,2,254], [3,2,2,254,255] })
        {
            try { RechargedLoginReply.Parse(malformed); throw new Exception("truncated login accepted"); }
            catch (InvalidDataException) { Check(true, "truncated accepted reply rejected"); }
        }
        byte[] stats = [.. EoWire.Number(42,4), .. Enumerable.Repeat((byte)1,22)];
        byte[] roster = [.. accepted, 2,255, .. System.Text.Encoding.ASCII.GetBytes("Testhero"),255,255,255, .. stats,255];
        var rosterResult = RechargedCharacters.Parse(roster);
        Check(rosterResult.Count == 1 && rosterResult[0].Id == 42 && rosterResult[0].Name == "Testhero", "character record extracts name and id");
        Check(RechargedCharacters.Parse([.. accepted,1,255]).Count == 0, "zero characters supported");
        foreach (var malformed in new byte[][] { roster[..^15], [.. roster,1] })
        {
            try { RechargedCharacters.Parse(malformed); throw new Exception("invalid roster accepted"); }
            catch (InvalidDataException) { Check(true, "invalid roster rejected"); }
        }
        using (var stream = new DuplexFixture([.. EoWire.Frame(initial), .. Server([244,3,1]), .. Server(roster), .. Server([21,1,6,113,254,254,254,4])]))
        {
            SessionCheckResult? update = null;
            var result = await RechargedLogin.TestStreamAsync(stream, ServerProfile.Official, "123456789",
                "testuser".ToCharArray(), "TestOnly123".ToCharArray(), default,
                new InlineProgress(value => update = value));
            Check(update?.Status == "login-accepted" && update.Characters?.Count == 1, "authenticated roster published while connection remains open");
            Check(stream.Writes.Count == 5, "authenticated session continues answering keepalive without resending login");
            Check(result.Status == "login-closed", "authenticated EOF closes cleanly");
        }
        Check(RechargedCharacters.Parse(roster[..^1]).Count == 1, "official reader permits final field without delimiter");
        byte[] extendedCount = [.. accepted,2,1,1,255, .. roster[(accepted.Length + 2)..]];
        Check(RechargedCharacters.Parse(extendedCount).Count == 1, "official reader uses first count byte and skips remaining header");
        byte[] liveLayout = [.. accepted,101,1,255,
            .. System.Text.Encoding.ASCII.GetBytes("Examplehero"),255,
            .. System.Text.Encoding.ASCII.GetBytes("ExampleX"),255,
            .. System.Text.Encoding.ASCII.GetBytes("ExampleXX"),255,
            .. stats,1,1,1,1,1,255];
        Check(liveLayout.Length == 82, "regression matches observed 82-byte response shape");
        Check(RechargedCharacters.Parse(liveLayout).Single().Name == "Examplehero", "large count does not reject populated character slots");
        byte[] portraitStats=[..EoWire.Number(42,4),..EoWire.Number(12,2),2,6,4,3,1,1,1,
            ..Enumerable.Range(1,9).SelectMany(n=>EoWire.Number(n,2))];
        var portraitRoster=RechargedCharacters.Parse([..accepted,2,255,(byte)'A',255,255,255,..portraitStats,255]);
        var portrait=portraitRoster.Single().Appearance!;
        Check(portrait.Gender==1 && portrait.HairStyle==5 && portrait.HairColor==3 && portrait.Skin==2,
            "selection appearance uses official record offsets");
        Check(portrait.Equipment.SequenceEqual(Enumerable.Range(1,9)), "selection equipment shorts decoded");
        Check(rosterResult[0].Appearance==null,"short legacy character record does not invent appearance");
        byte[] welcome = [2,30,2,1,1, .. EoWire.Number(12,2), .. EoWire.Number(42,4),
            .. EoWire.Number(7,2),1, .. EoWire.Number(10,2), .. EoWire.Number(11,2), .. EoWire.Number(1200,3)];
        Check(RechargedWelcome.Parse(welcome, 42).MapId == 7, "welcome prefix identifies map");
        foreach (var invalid in new[] { welcome[..^1], welcome })
        {
            try { RechargedWelcome.Parse(invalid, 999); throw new Exception("invalid welcome accepted"); }
            catch (InvalidDataException) { Check(true, "truncated or mismatched welcome rejected"); }
        }
        using (var stream = new DuplexFixture([.. EoWire.Frame(initial), .. Server([244,3,1]), .. Server(roster),
            .. Server([21,1,6,113,254,254,254,4]), .. Server(welcome)]))
        {
            var selection = new TaskCompletionSource<int>();
            var result = await RechargedLogin.TestStreamAsync(stream, ServerProfile.Official, "123456789",
                "testuser".ToCharArray(), "TestOnly123".ToCharArray(), default,
                new InlineProgress(_ => selection.SetResult(42)), characterSelection: selection.Task);
            Check(result.Status == "selection-accepted" && result.Welcome?.MapFileSize == 1200,
                "UI selection receives validated welcome");
            Check(stream.Writes.Count == 6, "one selection and continued keepalive after login");
            var challenge = EoWire.Number(stream.Writes[0].AsSpan(11,3));
            var key = 7 + challenge % 11;
            var request = RechargedSession.Transform(stream.Writes[4].AsSpan(2), key, false);
            Check(request[0] == 35 && request[1] == 30 && EoWire.Number(request.AsSpan(3,4)) == 42,
                "selection sends only roster-owned character");
            var modifier = EoWire.Number(request.AsSpan(7,1));
            var keepalive = RechargedSession.Transform(stream.Writes[5].AsSpan(2), key + modifier, false);
            Check(keepalive[0] == 241 && keepalive[1] == 1, "post-selection keepalive uses advanced key");
        }
        using (var stream = new DuplexFixture([.. EoWire.Frame(initial), .. Server([244,3,1]), .. Server(roster)]))
        {
            try
            {
                await RechargedLogin.TestStreamAsync(stream, ServerProfile.Official, "123456789",
                    "testuser".ToCharArray(), "TestOnly123".ToCharArray(), default,
                    characterSelection: Task.FromResult(999));
                throw new Exception("unowned selection accepted");
            }
            catch (InvalidDataException) { Check(stream.Writes.Count == 4, "unowned character never sent to server"); }
        }
        return assertions;
    }

    private sealed class InlineProgress(Action<SessionCheckResult> report) : IProgress<SessionCheckResult>
    {
        public void Report(SessionCheckResult value) => report(value);
    }

    private sealed class DuplexFixture(byte[] input) : Stream
    {
        private int position;
        public List<byte[]> Writes { get; } = [];
        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => position; set => throw new NotSupportedException(); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (position == input.Length || buffer.IsEmpty) return ValueTask.FromResult(0);
            buffer.Span[0] = input[position++];
            return ValueTask.FromResult(1);
        }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Writes.Add(buffer.ToArray());
            return ValueTask.CompletedTask;
        }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
