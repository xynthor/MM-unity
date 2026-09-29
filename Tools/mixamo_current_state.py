from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pages=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url]
    print("pages",len(pages))
    for i,pg in enumerate(pages):
        try:
            print(i,pg.url,pg.title())
            txt=pg.locator("body").inner_text(timeout=3000)
            print(txt[-1800:])
        except Exception as e: print("ERR",e)
