using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// Copies complete connected FIGURES to padded cells. No limb cutting, scaling, or deformation.
public static class WholeCelLayout
{
    public static string Prepare(string input,string output,int cell=768)
    {
        using var src=new Bitmap(input);int w=src.Width,h=src.Height;
        if(w!=h||w%2!=0)throw new Exception("Expected square2x2 master");
        int old=w/2,n=w*h;var raw=new byte[n*4];var seen=new bool[n];var q=new int[n];
        using var readable=new Bitmap(w,h,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(readable))g.DrawImageUnscaled(src,0,0);
        var data=readable.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        Marshal.Copy(data.Scan0,raw,0,raw.Length);readable.UnlockBits(data);
        var figures=new List<(int Count,Rectangle Box)>();
        for(int start=0;start<n;start++)
        {
            if(seen[start]||!Ink(start))continue;
            int first=0,last=1;q[0]=start;seen[start]=true;
            int minx=start%w,maxx=minx,miny=start/w,maxy=miny;
            while(first<last)
            {
                int p=q[first++],x=p%w,y=p/w;
                minx=Math.Min(minx,x);maxx=Math.Max(maxx,x);miny=Math.Min(miny,y);maxy=Math.Max(maxy,y);
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    int nx=x+dx,ny=y+dy;if(nx<0||nx>=w||ny<0||ny>=h)continue;
                    int a=ny*w+nx;if(seen[a]||!Ink(a))continue;seen[a]=true;q[last++]=a;
                }
            }
            if(last>5000)figures.Add((last,Rectangle.FromLTRB(Math.Max(0,minx-3),Math.Max(0,miny-3),Math.Min(w,maxx+4),Math.Min(h,maxy+4))));
        }
        if(figures.Count!=4)throw new Exception("Expected exactly four separate complete figures");
        using var dst=new Bitmap(cell*2,cell*2,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(dst))g.Clear(Color.White);
        var used=new HashSet<int>();var report=new System.Text.StringBuilder();
        foreach(var fig in figures)
        {
            var r=fig.Box;int col=(r.Left+r.Right)/2>=old?1:0,row=(r.Top+r.Bottom)/2>=old?1:0,id=row*2+col;
            if(!used.Add(id))throw new Exception("Ambiguous cell assignment");
            if(r.Width>cell-24||r.Height>cell-24)throw new Exception("Figure exceeds padded canvas; never crop it");
            int pad=(cell-old)/2;
            int x=Math.Clamp(r.X-col*old+pad,12,cell-r.Width-12);
            int y=Math.Clamp(r.Y-row*old+pad,12,cell-r.Height-12);
            using var figure=readable.Clone(r,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(dst))g.DrawImageUnscaled(figure,col*cell+x,row*cell+y);
            report.AppendLine($"pose={id+1}; source={r}; wholeCelOffset={col*cell+x-r.X},{row*cell+y-r.Y}; noScale=True");
        }
        dst.Save(output,ImageFormat.Png);return report.ToString().TrimEnd();
        bool Ink(int p){int o=p*4;return raw[o]<225||raw[o+1]<225||raw[o+2]<225;}
    }
}
