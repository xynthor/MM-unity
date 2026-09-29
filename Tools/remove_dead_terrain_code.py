from pathlib import Path
p=Path('Assets/Editor/MMFinalRealismPass.cs'); s=p.read_text(); start=s.index('        bool snow=',s.index('static int BuildTerrainForms'));end=s.index('        return made;',start)+len('        return made;\n'); Path('Backups/StructuralCleanup_20260920/MMFinalRealismPass.cs').write_text(s); p.write_text(s[:start]+s[end:])
