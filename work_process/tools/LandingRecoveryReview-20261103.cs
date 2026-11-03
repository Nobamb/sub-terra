using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// Background-only connected-component cleanup. No limb manipulation.
public static class LandingRecoveryReview
{
    public static string Extract(string source,string output,int column)
    {
        using var sheet=new Bitmap(source);
        if(sheet.Width!=sheet.Height*2)throw new Exception("Expected two square cells");
        int s=sheet.Height,n=s*s;
        using var cel=sheet.Clone(new Rectangle(column*s,0,s,s),PixelFormat.Format32bppArgb);
        var rect=new Rectangle(0,0,s,s);
        var data=cel.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        var b=new byte[n*4];Marshal.Copy(data.Scan0,b,0,b.Length);cel.UnlockBits(data);
        var labels=new int[n];var queue=new int[n];int id=0,best=0,bestSize=0;
        for(int start=0;start<n;start++)
        {
            if(labels[start]!=0||b[start*4+3]==0)continue;
            int first=0,last=1;queue[0]=start;labels[start]=++id;
            while(first<last)
            {
                int p=queue[first++],x=p%s,y=p/s;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=s||ny>=s)continue;
                    int q=ny*s+nx;if(labels[q]!=0||b[q*4+3]==0)continue;
                    labels[q]=id;queue[last++]=q;
                }
            }
            if(last>bestSize){bestSize=last;best=id;}
        }
        if(bestSize<10000)throw new Exception("Full figure missing");
        int removed=0,margin=0;
        for(int p=0;p<n;p++)
        {
            if(labels[p]!=best){if(b[p*4+3]>0)removed++;Array.Clear(b,p*4,4);}
            else if(p%s<4||p%s>s-5||p/s<4||p/s>s-5)margin++;
        }
        if(margin>0)throw new Exception("Figure reaches cell edge");
        data=cel.LockBits(rect,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
        Marshal.Copy(b,0,data.Scan0,b.Length);cel.UnlockBits(data);cel.Save(output,ImageFormat.Png);
        return $"column={column}; isolatedAlphaPixelsRemoved={removed}; foregroundRgbChanged=0; nonzeroMarginPixels={margin}";
    }
}
