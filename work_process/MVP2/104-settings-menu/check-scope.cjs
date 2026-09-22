const fs = require('fs');
const {execFileSync} = require('child_process');
const paths = [
  'sub-terra/Assets/_Project/Prefabs/UI/MainMenuPanel.prefab',
  'sub-terra/Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab',
  'sub-terra/Assets/_Project/Scenes/App/Mine_Demo_Integration.unity'
];
function read(text) {
  const blocks = new Map();
  for (const m of text.replace(/\r/g, '').matchAll(/^--- !u!\d+ &(\d+).*\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)) blocks.set(m[1],m[2]);
  const objects = new Set([...blocks].filter(([,b])=>/^  m_Name: SettingsPanel$/m.test(b)).map(([id])=>id));
  const transforms = new Set();
  for (let changed=true;changed;) {
    changed=false;
    for (const [id,b] of blocks) {
      if (!/^(Rect)?Transform:/m.test(b)) continue;
      const go=b.match(/m_GameObject: \{fileID: (\d+)/)?.[1];
      const parent=b.match(/m_Father: \{fileID: (\d+)/)?.[1];
      if ((objects.has(go)||transforms.has(parent))&&!transforms.has(id)) {
        objects.add(go);transforms.add(id);changed=true;
      }
    }
  }
  const allowed=new Set(objects);
  for (const [id,b] of blocks) if(objects.has(b.match(/m_GameObject: \{fileID: (\d+)/)?.[1]))allowed.add(id);
  return {blocks,allowed};
}
for (const path of paths) {
  const before=read(execFileSync('git',['show','HEAD:'+path],{encoding:'utf8',maxBuffer:20000000}));
  const after=read(fs.readFileSync(path,'utf8'));
  const changed=[...new Set([...before.blocks.keys(),...after.blocks.keys()])].filter(id=>before.blocks.get(id)!==after.blocks.get(id));
  const outside=changed.filter(id=>!before.allowed.has(id)&&!after.allowed.has(id));
  console.log(JSON.stringify({path,changed:changed.length,outsideSettings:outside}));
  if(process.argv.includes('--details')) for(const id of outside) {
    const oldLines=(before.blocks.get(id)||'').split('\n'), newLines=(after.blocks.get(id)||'').split('\n');
    console.log(JSON.stringify({id,removed:oldLines.filter(s=>!newLines.includes(s)),added:newLines.filter(s=>!oldLines.includes(s))}));
  }
  if(outside.length)process.exitCode=1;
}
