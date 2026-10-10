namespace Endless.Apple.World;

/// <summary>Bounded four-way route search. Re-plan each confirmed step for moving obstacles.</summary>
public static class WorldPathfinder
{
    public static int? NextDirection(int width, int height, int x, int y, int targetX, int targetY,
        Func<int,int,bool> canEnter)
    {
        bool InBounds(int a,int b)=>a>=0 && b>=0 && a<width && b<height;
        if(width is <1 or >250 || height is <1 or >250 || !InBounds(x,y) ||
            !InBounds(targetX,targetY) || (x==targetX && y==targetY) || !canEnter(targetX,targetY)) return null;
        var seen=new bool[width*height];
        var queue=new Queue<(int X,int Y,int First)>();
        queue.Enqueue((x,y,-1));seen[y*width+x]=true;
        while(queue.TryDequeue(out var p))
            for(int direction=0;direction<4;direction++)
            {
                var nx=p.X+(direction==1?-1:direction==3?1:0);
                var ny=p.Y+(direction==2?-1:direction==0?1:0);
                if(!InBounds(nx,ny) || seen[ny*width+nx] || !canEnter(nx,ny))continue;
                seen[ny*width+nx]=true;
                var first=p.First<0?direction:p.First;
                if(nx==targetX && ny==targetY)return first;
                queue.Enqueue((nx,ny,first));
            }
        return null;
    }
}
