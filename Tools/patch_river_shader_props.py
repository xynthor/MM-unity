from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Shaders\MMRiverWater.shader')
s=p.read_text(encoding='utf-8')
old='''    Properties\n    {\n        _ShallowColor'''
new='''    Properties\n    {\n        _Color("Color",Color)=(1,1,1,1)\n        _ShallowColor'''
if old not in s: raise SystemExit('color marker missing')
s=s.replace(old,new,1)
old='''        _WaveSpeed("Wave Speed",Float)=1.0\n        _Distortion'''
new='''        _WaveSpeed("Wave Speed",Float)=1.0\n        _WaveAmp2("Wave Amp 2",Float)=0\n        _WaveScale2("Wave Scale 2",Float)=0\n        _WaveSpeed2("Wave Speed 2",Float)=0\n        _WorldSize("World Size",Float)=1536\n        _Distortion'''
if old not in s: raise SystemExit('dummy props marker missing')
s=s.replace(old,new,1)
p.write_text(s,encoding='utf-8')
print('RIVER_SHADER_PROPS_OK')
