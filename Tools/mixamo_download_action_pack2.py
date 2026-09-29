from playwright.sync_api import sync_playwright
from pathlib import Path
out=Path(r"C:\MMUnityPort\Imports\Mixamo"); out.mkdir(parents=True,exist_ok=True)
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pages=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url]
    pg=None
    for q in pages:
        try:
            if "ACTION ADVENTURE PACK ON" in q.locator("body").inner_text(timeout=3000).upper():
                pg=q; break
        except: pass
    if pg is None: raise RuntimeError("Action Adventure tab not found")
    pg.get_by_role("button",name="DOWNLOAD").first.click()
    pg.wait_for_timeout(800)
    sels=pg.locator("select")
    print("selects",sels.count())
    sels.nth(1).select_option(label="FBX for Unity(.fbx)")
    sels.nth(2).select_option(label="No Character")
    sels.nth(3).select_option(label="30")
    sels.nth(4).select_option(label="none")
    modal=pg.locator(".modal").filter(has_text="DOWNLOAD SETTINGS")
    with pg.expect_download(timeout=120000) as di:
        modal.get_by_role("button",name="DOWNLOAD").last.click()
    d=di.value
    dest=out/d.suggested_filename
    d.save_as(str(dest))
    print("saved",dest,dest.stat().st_size)
