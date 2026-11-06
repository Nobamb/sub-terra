using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// Whole-cel measurements/transforms only. Never edits anatomy or assembles body parts.
public static class SpriteMotionAlignment
{
    public sealed class Region
    {
        public int Count; public int X; public int Y; public int Width; public int Height;
        public double CenterX=>X+Width/2.0;public double CenterY=>Y+Height/2.0;
    }
    public sealed class Measurements
    {
        public Region Bounds;public Region Helmet;public Region Belt;
    }
    public static Measurements Measure(string path)
    {
        using var src=new Bitmap(path);int w=src.Width,h=src.Height,n=w*h;
        using var rgba=new Bitmap(w,h,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(rgba))g.DrawImageUnscaled(src,0,0);
        var data=rgba.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        var b=new byte[n*4];Marshal.Copy(data.Scan0,b,0,b.Length);rgba.UnlockBits(data);
        int l=w,t=h,r=-1,bottom=-1,count=0;
        for(int p=0;p<n;p++)if(b[p*4+3]>32){int x=p%w,y=p/w;l=Math.Min(l,x);r=Math.Max(r,x);t=Math.Min(t,y);bottom=Math.Max(bottom,y);count++;}
        if(count==0)throw new Exception("Empty image");
        var bounds=new Region{X=l,Y=t,Width=r-l+1,Height=bottom-t+1,Count=count};
        var oranges=FindRegions(b,w,h,0);Region helmet=null;
        foreach(var a in oranges)if(a.Y<t+bounds.Height*0.35&&(helmet==null||a.Count>helmet.Count))helmet=a;
        var cyans=FindRegions(b,w,h,1);Region belt=null;double best=double.PositiveInfinity;
        double tx=bounds.CenterX+bounds.Width*.05,ty=t+bounds.Height*.55;
        foreach(var a in cyans)
        {
            if(a.Count<8)continue;
            double score=Math.Abs(a.CenterY-ty)*2+Math.Abs(a.CenterX-tx)*.5;
            if(score<best){belt=a;best=score;}
        }
        if(helmet==null)throw new Exception("Helmet orange component missing");
        return new Measurements{Bounds=bounds,Helmet=helmet,Belt=belt};
    }
    private static List<Region> FindRegions(byte[] b,int w,int h,int mode)
    {
        int n=w*h;var seen=new bool[n];var q=new int[n];var result=new List<Region>();
        for(int start=0;start<n;start++)
        {
            if(seen[start]||!Match(start))continue;
            int first=0,last=1;q[0]=start;seen[start]=true;
            int l=start%w,r=l,t=start/w,bottom=t;
            while(first<last)
            {
                int p=q[first++],x=p%w,y=p/w;l=Math.Min(l,x);r=Math.Max(r,x);t=Math.Min(t,y);bottom=Math.Max(bottom,y);
                Add(x-1,y);Add(x+1,y);Add(x,y-1);Add(x,y+1);
            }
            if(last>=8)result.Add(new Region{X=l,Y=t,Width=r-l+1,Height=bottom-t+1,Count=last});
            void Add(int x,int y){if(x<0||x>=w||y<0||y>=h)return;int p=y*w+x;if(seen[p]||!Match(p))return;seen[p]=true;q[last++]=p;}
        }
        return result;
        bool Match(int p)
        {
            int o=p*4,bb=b[o],g=b[o+1],r=b[o+2];if(b[o+3]<180)return false;
            return mode==0?r>140&&g>55&&g<225&&bb<125&&r>g*1.07&&g>bb*1.25:g>100&&bb>100&&r<140&&g>r*1.2&&bb>r*1.2;
        }
    }
    public static string Render(string source,string output,double scale,double dx,double dy,int size=1024)
    {
        using var src=new Bitmap(source);using var dst=new Bitmap(size,size,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(dst))
        {
            g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.DrawImage(src,new RectangleF((float)dx,(float)dy,(float)(src.Width*scale),(float)(src.Height*scale)),new RectangleF(0,0,src.Width,src.Height),GraphicsUnit.Pixel);
        }
        dst.Save(output,ImageFormat.Png);
        var m=Measure(output);var a=m.Bounds;
        if(a.X<3||a.Y<3||a.X+a.Width>size-3||a.Y+a.Height>size-3)throw new Exception("Aligned cel clipped: "+output);
        return $"bounds={a.X},{a.Y},{a.Width},{a.Height}";
    }
}
