namespace Endless.Apple.World;

public readonly record struct ScenePosition(double X, double Y);

/// <summary>Interpolates confirmed one-tile moves; never predicts server positions.</summary>
public sealed class WorldMotion
{
    private sealed record Move(ScenePosition From, ScenePosition To, double Started, double Duration, double CycleStarted);
    private readonly Dictionary<(bool Npc,int Id),Move> moves = new();
    public const double StepSeconds = 0.25;

    public void Update(IEnumerable<(bool Npc,int Id,int X,int Y)> entities, double seconds)
    {
        if (!double.IsFinite(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        var present = new HashSet<(bool,int)>();
        foreach (var entity in entities)
        {
            var key = (entity.Npc,entity.Id);
            present.Add(key);
            var target = new ScenePosition(entity.X,entity.Y);
            if (!moves.TryGetValue(key,out var previous)) moves[key] = new(target,target,seconds,StepSeconds,seconds);
            else if (previous.To != target)
            {
                var adjacent = Math.Abs(previous.To.X-target.X)+Math.Abs(previous.To.Y-target.Y)==1;
                var interval=seconds-previous.Started;
                var continuous=adjacent && previous.From!=previous.To && interval is >=.1 and <=.6;
                // Match observed confirmed-step cadence, without predicting a destination.
                var duration=continuous ? Math.Clamp(interval,.2,.4) : StepSeconds;
                moves[key] = new(adjacent ? Position(key.Npc,key.Id,seconds) : target,target,seconds,
                    duration,continuous ? previous.CycleStarted : seconds);
            }
        }
        foreach (var key in moves.Keys.Where(key => !present.Contains(key)).ToArray()) moves.Remove(key);
    }

    public int? WalkingFrame(bool npc,int id,double seconds)
    {
        if (!moves.TryGetValue((npc,id),out var move) || move.From==move.To) return null;
        var progress=(seconds-move.Started)/move.Duration;
        return progress is >= 0 and < 1 ? (int)((seconds-move.CycleStarted)/(.5/4))%4 : null;
    }

    public ScenePosition Position(bool npc,int id,double seconds)
    {
        if (!double.IsFinite(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (!moves.TryGetValue((npc,id),out var move)) throw new KeyNotFoundException("Entity has no scene position.");
        var progress = Math.Clamp((seconds-move.Started)/move.Duration,0,1);
        return new(move.From.X+(move.To.X-move.From.X)*progress,move.From.Y+(move.To.Y-move.From.Y)*progress);
    }
}
