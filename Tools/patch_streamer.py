from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Scripts\OpenWorld\MMWorldRegionStreamer.cs')
s=p.read_text(encoding='utf-8-sig')
s=s.replace('''        public string sceneName;
        public Vector3 offset;''','''        public string sceneName;
        public Vector3 offset;
        public bool keepTerrain;''',1)
s=s.replace('''                        new Vector2(r.offset.x, r.offset.z));''','''                        new Vector2(r.offset.x + 768f, r.offset.z + 768f));''',1)
s=s.replace('''                foreach (var t in root.GetComponentsInChildren<Terrain>(true))
                {
                    t.enabled = false;
                    var tc = t.GetComponent<TerrainCollider>();
                    if (tc) tc.enabled = false;
                }''','''                foreach (var t in root.GetComponentsInChildren<Terrain>(true))
                {
                    if (!r.keepTerrain)
                    {
                        t.enabled = false;
                        var tc = t.GetComponent<TerrainCollider>();
                        if (tc) tc.enabled = false;
                    }
                    else
                    {
                        t.enabled = true;
                        var tc = t.GetComponent<TerrainCollider>();
                        if (tc) tc.enabled = true;
                    }
                }''',1)
p.write_text(s,encoding='utf-8')
print('patched streamer',len(s.splitlines()))
