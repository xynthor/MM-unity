from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\MMSourceGridExactPostPass.cs')
s=p.read_text(encoding='utf-8')
old='''            string ap=SourceAssetPath(r.gameObject).ToLowerInvariant();
            if(!ap.Contains("/vegetation/trees/"))continue;
'''
print('found',old in s)
s=s.replace(old,'')
p.write_text(s,encoding='utf-8')
