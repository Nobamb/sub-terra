"""Remove only border-connected flat GIF background; preserve source motion and RGB."""
from pathlib import Path
import sys,json,hashlib,zipfile
sys.path.insert(0,str(Path(__file__).resolve().parent/'video-deps'))
import cv2,numpy as np
from PIL import Image,ImageSequence,ImageDraw

ROOT=Path(__file__).resolve().parents[2]
FOLDER=ROOT/'work_process/images/teammate-walk-gif-20261027'
NATIVE=FOLDER/'transparent-native';RUNTIME=FOLDER/'runtime-frames'

def background(frame):
    rgb=np.array(frame.convert('RGB'));h,w=rgb.shape[:2];color=rgb[0,0]
    # GIF frames quantize the visually black background to several near-black colors.
    # Only the exterior-connected dark component is removed; closed eyes/ink stay opaque.
    maximum=np.max(rgb,axis=2)
    equal=(maximum<=32).astype(np.uint8)
    padded=np.pad(equal,1,constant_values=1)
    cv2.floodFill(padded,None,(0,0),2,flags=4)
    exterior=padded[1:-1,1:-1]==2
    solid=(~exterior).astype(np.uint8)
    # Retain source dark outlines next to actual body color, without distant GIF matte trails.
    rim=cv2.dilate(solid,np.ones((3,3),np.uint8),iterations=2)>0
    ink=rim&(maximum>6)&(maximum<=32)
    solid[ink]=1
    alpha=solid*255
    rgba=np.dstack((rgb,alpha))
    assert np.array_equal(rgba[:,:,:3],rgb)
    assert np.all(alpha[~equal.astype(bool)]==255)
    return Image.fromarray(rgba)

def composite(im,color):
    canvas=Image.new('RGBA',im.size,color);canvas.alpha_composite(im);return canvas.convert('RGB')

def main():
    NATIVE.mkdir(exist_ok=True);RUNTIME.mkdir(exist_ok=True)
    src=Image.open(FOLDER/'source.gif')
    clean=[];durations=[];records=[]
    for index,frame in enumerate(ImageSequence.Iterator(src)):
        im=background(frame);clean.append(im);durations.append(frame.info.get('duration',100))
        im.save(NATIVE/f'walk-{index+1:02}.png')
        box=im.getchannel('A').getbbox()
        records.append({'frame':index+1,'duration_ms':durations[-1],'native_bbox':box,'rgb_unchanged':True,
                        'non_background_color_pixels_retained':True})
    assert len(clean)==47 and set(durations)=={100}, 'Do not silently reinterpret GIF timing'
    scale=226/float(np.median([r['native_bbox'][3]-r['native_bbox'][1] for r in records]))
    for i,(im,record) in enumerate(zip(clean,records)):
        rgb=np.array(im);r,g,b=[rgb[:,:,j].astype(np.int16) for j in range(3)];yy,xx=np.indices(r.shape)
        box=record['native_bbox'];gold=(r-g>30)&(g-b>30)&(r>130)&(rgb[:,:,3]>128)&(yy<box[1]+.32*(box[3]-box[1]))
        ys,xs=np.where(gold);assert len(xs)>50
        dx=round(137-(xs.min()+xs.max())*.5*scale);dy=round(240-box[3]*scale)
        resized=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.LANCZOS)
        runtime=Image.new('RGBA',(256,256));runtime.alpha_composite(resized,(dx,dy))
        data=np.array(runtime);by,bx=np.where(data[:,:,3]>=32)
        assert bx.min()>4 and bx.max()<251 and 14<=255-by.max()<=18
        assert 218<=by.max()-by.min()+1<=236
        path=RUNTIME/f'walk-{i+1:02}.png';runtime.save(path)
        record.update({'uniform_scale':scale,'translation':[dx,dy],'runtime_sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
    # Transparent GIF keeps the source durations. Reserve palette index 255 for alpha.
    atlas=Image.new('RGB',(512*8,539*6),'white')
    for i,im in enumerate(clean):atlas.paste(composite(im,'white'),((i%8)*512,(i//8)*539))
    palette=atlas.quantize(colors=255)
    frames=[]
    for im in clean:
        q=im.convert('RGB').quantize(palette=palette,dither=Image.Dither.NONE)
        indices=np.array(q);indices[np.array(im.getchannel('A'))==0]=255
        q=Image.fromarray(indices,'P');q.putpalette(palette.getpalette()[:765]+[0,0,0]);q.info['transparency']=255;frames.append(q)
    frames[0].save(FOLDER/'transparent-walk.gif',save_all=True,append_images=frames[1:],duration=durations,
                   loop=0,transparency=255,disposal=2,optimize=False)
    preview=[]
    for im in clean:
        out=Image.new('RGB',(512,270));out.paste(composite(im,'white').resize((256,270)),(0,0));out.paste(composite(im,'#080808').resize((256,270)),(256,0));preview.append(out)
    display=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in preview]
    display[0].save(FOLDER/'white-dark-preview.gif',save_all=True,append_images=display[1:],duration=durations,loop=0,disposal=2)
    for color,name in [('white','white'),('#080808','dark')]:
        sheet=Image.new('RGB',(1024,960),'#777777');draw=ImageDraw.Draw(sheet)
        for i,im in enumerate(clean):
            x,y=(i%8)*128,(i//8)*160;sheet.paste(composite(im,color).resize((128,135)),(x,y+20));draw.text((x+3,y+3),str(i+1),fill='white')
        sheet.save(FOLDER/f'all-frames-{name}.png')
    decoded=Image.open(FOLDER/'transparent-walk.gif');total=0;count=0
    for frame in ImageSequence.Iterator(decoded):
        a=np.array(frame.convert('RGBA').getchannel('A'));assert a[0,0]==0
        assert np.array_equal(a,np.array(clean[count].getchannel('A'))), 'GIF disposal left stale pixels'
        total+=frame.info.get('duration',0);count+=1
    assert count==47 and total==sum(durations)
    report={'frame_count':len(clean),'fps':10,'period_seconds':sum(durations)/1000,'source_size':src.size,
        'method':'exterior-connected near-black matte removed; source dark ink retained within 2px of body; enclosed dark details retained',
        'source_rgb_preserved':True,'durations_preserved':True,'decoded_transparent_gif_frames':count,
        'unity_applied':False,'frames':records}
    (FOLDER/'checks.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    with zipfile.ZipFile(FOLDER/'transparent-47-png.zip','w',zipfile.ZIP_DEFLATED) as z:
        for path in sorted(NATIVE.glob('*.png')):z.write(path,path.name)
        z.write(FOLDER/'checks.json','checks.json')
    print('47 original-RGB transparent PNGs, original-timed GIF and runtime-aligned PNGs exported.')

if __name__=='__main__':main()
