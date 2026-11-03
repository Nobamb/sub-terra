using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// User-authorized background/edge cleanup only. Never transforms or assembles limbs.
public static class MiningWhiteMatte
{
    public static string Process(string input, string output, string holeIds)
    {
        using var src = new Bitmap(input);
        int w=src.Width,h=src.Height,n=w*h;
        int columns,cellSize;
        if(w==1536&&h==1024){columns=3;cellSize=512;}
        else if(w==h&&w%2==0){columns=2;cellSize=w/2;}
        else throw new Exception("Expected 3x2 mining or 2x2 walk sheet");
        int cellCount=columns*(h/cellSize);
        using var rgba = new Bitmap(w,h,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(rgba)) g.DrawImageUnscaled(src,0,0);
        var rect=new Rectangle(0,0,w,h);
        var data=rgba.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        var bytes=new byte[n*4]; Marshal.Copy(data.Scan0,bytes,0,bytes.Length);rgba.UnlockBits(data);
        var remove=new bool[n]; var labels=new int[n]; var queue=new int[cellSize*cellSize];
        var accepted=new HashSet<string>((holeIds??"").Split(','));
        var report=new System.Text.StringBuilder();
        for(int cell=0;cell<cellCount;cell++)
        {
            int ox=cell%columns*cellSize,oy=cell/columns*cellSize,id=0;
            for(int y=oy;y<oy+cellSize;y++) for(int x=ox;x<ox+cellSize;x++)
            {
                int start=y*w+x;
                if(labels[start]!=0||!White(bytes,start))continue;
                id++; int first=0,last=1; queue[0]=start;labels[start]=id;
                int minx=x,maxx=x,miny=y,maxy=y;bool border=false;
                while(first<last)
                {
                    int p=queue[first++],px=p%w,py=p/w;
                    minx=Math.Min(minx,px);maxx=Math.Max(maxx,px);miny=Math.Min(miny,py);maxy=Math.Max(maxy,py);
                    border|=px==ox||px==ox+cellSize-1||py==oy||py==oy+cellSize-1;
                    Add(px-1,py);Add(px+1,py);Add(px,py-1);Add(px,py+1);
                    void Add(int nx,int ny)
                    {
                        if(nx<ox||nx>=ox+cellSize||ny<oy||ny>=oy+cellSize)return;
                        int q=ny*w+nx;if(labels[q]!=0||!White(bytes,q))return;
                        labels[q]=id;queue[last++]=q;
                    }
                }
                string tag=(cell+1)+":"+id;
                bool erase=border||accepted.Contains(tag);
                if(!border&&last>=10)report.AppendLine($"{tag} pixels={last} bbox={minx-ox},{miny-oy},{maxx-ox},{maxy-oy} remove={erase}");
                if(erase)for(int j=0;j<last;j++)remove[queue[j]]=true;
            }
        }
        var original=(byte[])bytes.Clone();int removed=0,edge=0,unchanged=0;
        for(int p=0;p<n;p++)
        {
            int o=p*4;
            if(remove[p]){bytes[o]=bytes[o+1]=bytes[o+2]=bytes[o+3]=0;removed++;continue;}
            bytes[o+3]=255;
            int x=p%w,y=p/w;bool boundary=false;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
            {int nx=x+dx,ny=y+dy;if(nx>=0&&nx<w&&ny>=0&&ny<h&&remove[ny*w+nx])boundary=true;}
            int lo=Math.Min(bytes[o],Math.Min(bytes[o+1],bytes[o+2]));
            int hi=Math.Max(bytes[o],Math.Max(bytes[o+1],bytes[o+2]));
            // Only neutral exterior matte pixels around an outlined sprite are un-matted.
            if(boundary&&hi-lo<28&&lo>22)
            {
                double coverage=Math.Clamp((255.0-(bytes[o]+bytes[o+1]+bytes[o+2])/3.0)/240.0,0,1);
                bytes[o]=bytes[o+1]=bytes[o+2]=15;bytes[o+3]=(byte)Math.Round(coverage*255);edge++;
            }
            else if(bytes[o]==original[o]&&bytes[o+1]==original[o+1]&&bytes[o+2]==original[o+2])unchanged++;
        }
        // Isolated leftovers are background; keep the one connected full-body cel per grid cell.
        Array.Clear(labels,0,labels.Length);int stray=0;
        for(int cell=0;cell<cellCount;cell++)
        {
            int ox=cell%columns*cellSize,oy=cell/columns*cellSize,id=0,best=0,bestSize=0;
            for(int y=oy;y<oy+cellSize;y++)for(int x=ox;x<ox+cellSize;x++)
            {
                int start=y*w+x;if(labels[start]!=0||bytes[start*4+3]==0)continue;
                id++;int first=0,last=1;queue[0]=start;labels[start]=id;
                while(first<last)
                {
                    int p=queue[first++],px=p%w,py=p/w;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int nx=px+dx,ny=py+dy;if(nx<ox||nx>=ox+cellSize||ny<oy||ny>=oy+cellSize)continue;
                        int q=ny*w+nx;if(labels[q]!=0||bytes[q*4+3]==0)continue;
                        labels[q]=id;queue[last++]=q;
                    }
                }
                if(last>bestSize){best=id;bestSize=last;}
            }
            if(bestSize<10000)throw new Exception("Foreground segmentation failed");
            for(int y=oy;y<oy+cellSize;y++)for(int x=ox;x<ox+cellSize;x++)
            {
                int p=y*w+x;if(bytes[p*4+3]>0&&labels[p]!=best)
                {bytes[p*4]=bytes[p*4+1]=bytes[p*4+2]=bytes[p*4+3]=0;stray++;}
            }
        }
        data=rgba.LockBits(rect,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
        Marshal.Copy(bytes,0,data.Scan0,bytes.Length);rgba.UnlockBits(data);rgba.Save(output,ImageFormat.Png);
        report.AppendLine($"removedBackground={removed}; unmatteBoundary={edge}; preservedInteriorRGB={unchanged}; isolatedPixelsRemoved={stray}");
        return report.ToString().TrimEnd();
    }
    private static bool White(byte[] b,int p){int o=p*4;return b[o]>=225&&b[o+1]>=225&&b[o+2]>=225;}
    public static string Audit(string input,string output)
    {
        using var src=new Bitmap(input);using var dst=new Bitmap(output);
        int changed=0,border=0,opaque=0,clear=0;
        int cell=dst.Width==1536&&dst.Height==1024?512:dst.Width/2;
        for(int y=0;y<dst.Height;y++)for(int x=0;x<dst.Width;x++)
        {
            var c=dst.GetPixel(x,y);var s=src.GetPixel(x,y);
            if(c.A==255){opaque++;if(c.R!=s.R||c.G!=s.G||c.B!=s.B)changed++;}
            if(c.A==0)clear++;
            if((x%cell<4||x%cell>cell-5||y%cell<4||y%cell>cell-5)&&c.A!=0)border++;
        }
        if(changed!=0||border!=0)throw new Exception("Opaque RGB or cel margin regression");
        return $"opaqueRgbChanged={changed}; nonzeroMarginPixels={border}; opaquePixels={opaque}; transparentPixels={clear}";
    }
}
