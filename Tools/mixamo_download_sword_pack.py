from playwright.sync_api import sync_playwright
from pathlib import Path
out=Path(r"C:\MMUnityPort\Imports\Mixamo")
out.mkdir(parents=True,exist_ok=True)
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    sels=pg.locator("select")
    print("select count",sels.count())
    sels.nth(1).select_option(label="FBX for Unity(.fbx)")
    sels.nth(2).select_option(label="No Character")
    sels.nth(3).select_option(label="30")
    sels.nth(4).select_option(label="none")
    modal=pg.locator(".modal").filter(has_text="DOWNLOAD SETTINGS")
    print("modal",modal.count())
    dlbtn=modal.get_by_role("button",name="DOWNLOAD").last
    with pg.expect_download(timeout=120000) as di:
        dlbtn.click()
    d=di.value
    print("suggested",d.suggested_filename)
    dest=out/d.suggested_filename
    d.save_as(str(dest))
    print("saved",dest, dest.stat().st_size)
