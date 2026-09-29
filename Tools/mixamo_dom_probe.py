from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    print("buttons:")
    for i,el in enumerate(pg.locator("button").all()):
        try:
            print(i, repr(el.inner_text(timeout=500)))
        except: pass
    print("links:")
    for i,el in enumerate(pg.locator("a").all()[:120]):
        try:
            t=el.inner_text(timeout=300).strip()
            if t: print(i,repr(t),el.get_attribute("href"))
        except: pass
