from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    print("INPUTS")
    for i,e in enumerate(pg.locator("input").all()):
        try:
            print(i, "type",e.get_attribute("type"),"placeholder",e.get_attribute("placeholder"),"value",e.input_value())
        except: pass
