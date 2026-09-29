from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pages=[pg for c in b.contexts for pg in c.pages]
    for i,pg in enumerate(pages):
        try:
            pg.wait_for_load_state("domcontentloaded",timeout=15000)
        except: pass
        print("PAGE",i,pg.url,pg.title())
        try:
            print(pg.locator("body").inner_text(timeout=5000)[-2500:])
        except Exception as e: print("ERR",e)
