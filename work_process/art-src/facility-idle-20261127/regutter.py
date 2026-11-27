# 셀 사이에 투명 여백(바이리니어 번짐 방지)을 넣어 시트를 다시 배치한다.
import sys
from PIL import Image
src,out,cw,ch,cols,count,pad=sys.argv[1],sys.argv[2],*map(int,sys.argv[3:8])
s=Image.open(src); sw,sh=cw+2*pad,ch+2*pad; rows=(count+cols-1)//cols
o=Image.new('RGBA',(sw*cols,sh*rows),(0,0,0,0))
for i in range(count):
    c=s.crop(((i%cols)*cw,(i//cols)*ch,(i%cols)*cw+cw,(i//cols)*ch+ch))
    o.paste(c,((i%cols)*sw+pad,(i//cols)*sh+pad))
o.save(out,optimize=True); print(out,o.size)
