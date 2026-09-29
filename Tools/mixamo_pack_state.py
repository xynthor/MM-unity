from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    pg.wait_for_timeout(7000)
    print(pg.locator("body").inner_text(timeout=5000)[-2500:])
    print("BUTTONS")
    for i,el in enumerate(pg.locator("button").all()):
        try:
            t=el.inner_text(timeout=300).strip()
            if t: print(i,repr(t))
        except: pass
