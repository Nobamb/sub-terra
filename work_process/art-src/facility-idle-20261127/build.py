import sys, json; sys.path.insert(0,'.')
from common import *
K=0.25; THICK=12.5
FR=list(range(18,129,2))
art=Image.open(ART).convert("RGBA")
# 1) union bbox in art space (top-down px)
boxes=[]; alphas={}
for i in FR:
    v=np.array(Image.open(f"frames/f{i:03d}.png").convert("RGB")).astype(np.int16)
    a=alpha_for(v,THICK); alphas[i]=a
    ys,xs=np.where(a>0.01)
    boxes.append((xs.min()*S_+TX, ys.min()*S_+TY, (xs.max()+1)*S_+TX, (ys.max()+1)*S_+TY))
b=np.array(boxes); x0,y0=b[:,0].min(),b[:,1].min(); x1,y1=b[:,2].max(),b[:,3].max()
M=8
x0=int(np.floor(min(x0,224)-M)); y0=int(np.floor(min(y0,113)-M)); x1=int(np.ceil(max(x1,1307)+M)); y1=int(np.ceil(max(y1,893)+M))
cw=int(round((x1-x0)*K)); ch=int(round((y1-y0)*K))
# 셀 크기에 맞춰 R을 다시 맞춤(정확한 K 유지)
x1=x0+cw/K; y1=y0+ch/K
print("R art px", x0,y0,x1,y1, "cell", cw,ch)
cols=7; rows=(len(FR)+cols-1)//cols
sheet=Image.new("RGBA",(cw*cols,ch*rows),(0,0,0,0))
for n,i in enumerate(FR):
    v=Image.open(f"frames/f{i:03d}.png").convert("RGB")
    rgba=np.dstack([np.array(v),(alphas[i]*255).astype(np.uint8)])
    im=Image.fromarray(rgba,"RGBA")
    # cell pixel (cx,cy) -> art = x0 + cx/K -> video = (art - TX)/S_
    a_=1/(K*S_); c_=(x0-TX)/S_; e_=(y0-TY)/S_
    # premultiply to avoid dark fringes when resampling
    pm=np.array(im).astype(np.float32); pm[...,:3]*=pm[...,3:4]/255
    pmI=[Image.fromarray(pm[...,c].astype(np.float32),"F") for c in range(4)]
    out=[ch_.transform((cw,ch),Image.AFFINE,(a_,0,c_,0,a_,e_),resample=Image.BICUBIC) for ch_ in pmI]
    o=np.dstack([np.array(x) for x in out]); al=np.clip(o[...,3],0,255)
    rgb=np.where(al[...,None]>0.5, o[...,:3]/np.maximum(al[...,None],1e-3)*255,0)
    cell=np.dstack([np.clip(rgb,0,255),al]).astype(np.uint8)
    sheet.paste(Image.fromarray(cell,"RGBA"),((n%cols)*cw,(n//cols)*ch))
sheet.save("StorageOpenSheet.png")
meta=dict(rect_art_topdown=[x0,y0,x1,y1],cell=[cw,ch],cols=cols,count=len(FR),fps=12,K=K)
json.dump(meta,open("meta.json","w")); print(meta, sheet.size)
