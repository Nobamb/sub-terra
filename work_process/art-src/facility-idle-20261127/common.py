from PIL import Image
import numpy as np
from scipy import ndimage as nd
S_=1.305; TX=239-178*S_; TY=129-97*S_
ART=r"C:/Users/USER/Desktop/develop/develop-list/sub-terra/sub-terra/Assets/_Project/Resources/Facilities/StorageGrounded.png"
def core(rgb, th=55):
    mx=rgb.max(axis=2)
    m=nd.binary_closing(mx>th,iterations=3)
    lab,n=nd.label(m); sizes=nd.sum(m,lab,range(1,n+1))
    keep=np.isin(lab, np.where(sizes>1500)[0]+1)
    return nd.binary_fill_holes(keep)
def alpha_for(rgb, thick):
    c=core(rgb)
    dist=nd.distance_transform_edt(~c)
    a=np.clip(thick+0.5-dist,0,1)
    # 바깥 테두리 안쪽 구멍(뚜껑 틈)도 채움
    filled=nd.binary_fill_holes(a>0.5)
    a=np.maximum(a, filled.astype(float))
    return a
