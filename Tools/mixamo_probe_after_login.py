from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pages=[pg for c in b.contexts for pg in c.pages]
    for i,pg in enumerate(pages):
        print("PAGE",i,pg.url,pg.title())
        try:
            txt=pg.locator("body").inner_text(timeout=5000)
            print(txt[:5000])
        except Exception as e:
            print("ERR",e)
