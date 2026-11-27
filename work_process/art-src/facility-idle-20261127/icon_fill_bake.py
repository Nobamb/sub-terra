# 충전기 번개·보건소 하트 아이콘: 빈 껍데기 → 아래에서 위로 액체가 차오르는 프레임 시트를 굽는다.
import sys, json, math
from PIL import Image
import numpy as np
from scipy import ndimage as nd
src, out, rim, liquid_hex, crest_hex = sys.argv[1], sys.argv[2], float(sys.argv[3]), sys.argv[4], sys.argv[5]
N=24; CELL=160; COLS=5
def hexc(h): return np.array([int(h[i:i+2],16) for i in (0,2,4)],float)/255
LIQ=hexc(liquid_hex); CREST=hexc(crest_hex)
a=np.array(Image.open(src).convert('RGBA')).astype(float)/255
H,W=a.shape[:2]
sil=a[...,3]>0.5
dist=nd.distance_transform_edt(sil)
inner=np.clip((dist-rim)/1.5,0,1)            # 내부(액체가 차는 곳), 경계 1.5px 부드럽게
ys,xs=np.where(inner>0.5); iy0,iy1=ys.min(),ys.max()
rgb=a[...,:3]; al=a[...,3]
lum=(rgb*[0.3,0.59,0.11]).sum(-1,keepdims=True)
glass=np.array([0.04,0.06,0.09])
empty_in=glass*0.85+lum*0.22*LIQ+0.02       # 어두운 빈 유리(모양만 살짝 비침)
yy,xx=np.mgrid[0:H,0:W].astype(float)
rng=np.random.default_rng(7)
bubbles=[(rng.uniform(0.25,0.75),rng.uniform(0,1),rng.uniform(3,6)) for _ in range(6)]
def frame(level, k):
    # level 0..1: 내부 아래(iy1)에서 위(iy0)로
    surf_y=iy1+1-(iy1+1-iy0+6)*level
    wave=surf_y+ 5.0*np.sin(2*math.pi*(xx/ (W*0.42)) + k*0.9) + 2.0*np.sin(2*math.pi*(xx/(W*0.17)) - k*1.7)
    below=np.clip((yy-wave)/1.2+0.5,0,1)
    if level<=0: below[:]=0
    if level>=1: below[:]=1
    fill=rgb*1.08+0.03
    crest=np.clip(1-np.abs(yy-wave-2.0)/3.0,0,1)*(level>0)*(level<1)
    col=empty_in*(1-below[...,None])+fill*below[...,None]
    col=col*(1-crest[...,None]*0.85)+CREST*crest[...,None]*0.85
    # 액체 속 거품
    if 0<level<1:
        for bx,by,br in bubbles:
            cx=W*bx; cy=iy1-(iy1-iy0)*((by+k*0.045)%1.0)*level
            d=np.sqrt((xx-cx)**2+(yy-cy)**2)
            ring=np.clip(1-np.abs(d-br)/1.2,0,1)*below
            col=col*(1-ring[...,None]*0.6)+CREST*ring[...,None]*0.6
    m=inner[...,None]
    outc=rgb*(1-m)+np.clip(col,0,1)*m
    return np.dstack([outc,al])
cells=[]
for i in range(N+1):
    lv=i/N
    lv_e=lv if i in (0,N) else min(1.0,max(0.0,lv))
    f=frame(lv_e,i)
    im=Image.fromarray((np.clip(f,0,1)*255).astype(np.uint8),'RGBA').resize((CELL,CELL),Image.LANCZOS)
    cells.append(im)
rows=math.ceil(len(cells)/COLS)
sheet=Image.new('RGBA',(CELL*COLS,CELL*rows),(0,0,0,0))
for i,c in enumerate(cells): sheet.paste(c,((i%COLS)*CELL,(i//COLS)*CELL))
sheet.save(out)
print(out, sheet.size, len(cells), 'interior y', iy0, iy1)
