"""Export whole newly drawn poses with Idle-compatible height/ground and 1s timing."""
from pathlib import Path
import sys,json,hashlib
sys.path.insert(0,str(Path(__file__).resolve().parent/'video-deps'))
import cv2,numpy as np
from PIL import Image,ImageOps,ImageDraw,ImageSequence
import imageio_ffmpeg

ROOT=Path(__file__).resolve().parents[2]
FOLDER=ROOT/'work_process/images/player-walk-whole-redraw-20261027'
OUT=FOLDER/'approved-one-second'

def bg(f,color):
    canvas=Image.new('RGBA',f.size,color);canvas.alpha_composite(f);return canvas.convert('RGB')

def save_gif(frames,path,durations):
    atlas=Image.new('RGB',(frames[0].width*4,frames[0].height*4))
    for i,f in enumerate(frames):atlas.paste(f,((i%4)*f.width,(i//4)*f.height))
    palette=atlas.quantize(colors=256)
    q=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in frames]
    q[0].save(path,save_all=True,append_images=q[1:],duration=durations,loop=0,disposal=2)
    decoded=Image.open(path);ms=0;errors=[]
    for i,f in enumerate(ImageSequence.Iterator(decoded)):
        ms+=f.info['duration'];errors.append(float(np.abs(np.array(f.convert('RGB')).astype(float)-np.array(frames[i])).mean()))
    assert len(errors)==16 and ms==sum(durations) and max(errors)<4
    return {'decoded_frames':16,'total_ms':ms,'max_palette_error':max(errors)}

def main():
    OUT.mkdir(exist_ok=True)
    source=Image.open(FOLDER/'whole-sixteen-v1.png').convert('RGBA');pixels=np.array(source)
    n,labels,stats,centroids=cv2.connectedComponentsWithStats((pixels[:,:,3]>32).astype(np.uint8),8)
    components=[i for i in range(1,n) if stats[i,4]>2000];assert len(components)==16
    rows=[[] for _ in range(4)]
    for label in components:rows[min(3,int(centroids[label,1]*4/source.height))].append(label)
    assert all(len(r)==4 for r in rows)
    ordered=[label for row in rows for label in sorted(row,key=lambda k:centroids[k,0])]
    scale=226/float(np.median([stats[i,3] for i in ordered]))
    frames=[];records=[]
    for pos,label in enumerate(ordered):
        x,y,w,h,area=[int(v) for v in stats[label]]
        rect=(max(0,x-2),max(0,y-2),min(source.width,x+w+2),min(source.height,y+h+2))
        cel=ImageOps.expand(source.crop(rect),border=16);a=np.array(cel)
        r,g,b=[a[:,:,j].astype(np.int16) for j in range(3)];yy,xx=np.indices(r.shape)
        gold=(r-g>30)&(g-b>30)&(r>130)&(a[:,:,3]>128)&(yy<cel.height*.34)
        ys,xs=np.where(gold);assert len(xs)>50
        bbox=cel.getchannel('A').point(lambda v:255 if v>=32 else 0).getbbox()
        dx=round(137-(xs.min()+xs.max())*.5*scale);dy=round(240-bbox[3]*scale)
        resized=cel.resize((round(cel.width*scale),round(cel.height*scale)),Image.Resampling.LANCZOS)
        frame=Image.new('RGBA',(256,256));frame.alpha_composite(resized,(dx,dy))
        data=np.array(frame);fy,fx=np.where(data[:,:,3]>=32)
        height=int(fy.max()-fy.min()+1);bottom=255-int(fy.max())
        assert 218<=height<=236 and 14<=bottom<=18
        assert fx.min()>4 and fx.max()<251
        path=OUT/f'walk-{pos+1:02}.png';frame.save(path)
        records.append({'frame':pos+1,'source_crop':rect,'uniform_scale':scale,'translation':[dx,dy],
            'height':height,'unity_ground_y':bottom,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
        frames.append(frame)
    assert len(set(r['sha256'] for r in records))==16
    display=[]
    for f in frames:
        canvas=Image.new('RGB',(512,256));canvas.paste(bg(f,'white'),(0,0));canvas.paste(bg(f,'#080808'),(256,0));display.append(canvas)
    normal=save_gif(display,OUT/'walk-1s.gif',[60,60,60,70]*4)
    slow=save_gif(display,OUT/'walk-slow-2s.gif',[120,130]*8)
    # 80 Hz is only a common video clock: source drawings are held for 4/5 ticks.
    writer=imageio_ffmpeg.write_frames(str(OUT/'timing-comparison.mp4'),(512,280),fps=80,codec='libx264',
        pix_fmt_in='rgb24',pix_fmt_out='yuv420p',macro_block_size=2,ffmpeg_log_level='error')
    writer.send(None)
    for tick in range(320):
        canvas=Image.new('RGB',(512,280),'#777777');draw=ImageDraw.Draw(canvas)
        for col,(idx,label) in enumerate([((tick//4)%16,'0.8s / 20fps'),((tick//5)%16,'1.0s / 16fps')]):
            canvas.paste(bg(frames[idx],'#080808'),(col*256,24));draw.text((col*256+5,5),label,fill='white')
        writer.send(np.array(canvas).tobytes())
    writer.close()
    result={'frame_count':16,'frame_rate':16,'period_seconds':1,'shared_uniform_scale':scale,
        'no_body_part_warp_or_compositing':True,'normal_gif':normal,'slow_gif':slow,'frames':records,
        'art_limitations':['Some foot spacing, arm timing and torso-angle variation remain.'],
        'unity_validation':'separate Unity test report; not inferred from export'}
    (OUT/'export.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print('16 whole PNGs, 1s GIF and timing comparison exported. Height/ground/uniqueness checks passed.')

if __name__=='__main__':main()
