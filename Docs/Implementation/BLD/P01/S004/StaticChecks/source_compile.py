from pathlib import Path
import subprocess,sys
out=Path('/tmp/lastsignal-s004-static');out.mkdir(exist_ok=True)
name=sys.argv[1]
source=Path('Library/Bee/artifacts/200b0aE.dag')/(name+'.rsp')
lines=source.read_text().splitlines()
result=[]
for line in lines:
    if line.startswith(('-out:','-refout:','-analyzer:','/additionalfile:','-additionalfile:','/debug:','-debug:')): continue
    if line.startswith('-r:'):
        for dep in ('LastSignal.Runtime','LastSignal.Editor'):
            if dep+'.ref.dll' in line or dep+'.dll' in line:
                if (out/(dep+'.dll')).exists():line='-r:"'+str(out/(dep+'.dll'))+'"'
    result.append(line)
folders={'LastSignal.Runtime':'Runtime/Gathering','LastSignal.Editor':'Editor','LastSignal.EditModeTests':'Tests/EditMode','LastSignal.PlayModeTests':'Tests/PlayMode'}
existing='\n'.join(result)
if name=='LastSignal.Runtime': additions=Path('Assets/LastSignal/Scripts/Runtime/Gathering').glob('Tree*.cs')
elif name=='LastSignal.Editor': additions=[Path('Assets/LastSignal/Scripts/Editor/TreePhysicsAuthoring.cs')]
else:additions=Path('Assets/LastSignal/Scripts/'+folders[name]).glob('P01Tree*.cs')
for p in additions:
    if str(p) not in existing: result.append('"'+str(p)+'"')
if name=='LastSignal.Editor' and 'RuntimeAssembly' not in existing:
    result.append('-r:"Library/ScriptAssemblies/RuntimeAssembly.dll"')
result.append('-out:"'+str(out/(name+'.dll'))+'"')
rsp=out/(name+'.rsp');rsp.write_text('\n'.join(result))
p=subprocess.run(['/usr/local/share/dotnet/dotnet','/usr/local/share/dotnet/sdk/9.0.306/Roslyn/bincore/csc.dll','@'+str(rsp)],capture_output=True,text=True)
(out/(name+'.log')).write_text(p.stdout+p.stderr)
print(p.stdout+p.stderr);sys.exit(p.returncode)
