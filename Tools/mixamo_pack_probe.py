from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    loc=pg.get_by_text("Sword And Shield Pack", exact=True)
    print("count",loc.count())
    for i in range(loc.count()):
        el=loc.nth(i)
        print("TAG",el.evaluate("(e)=>e.tagName"))
        print("HTML",el.evaluate("(e)=>e.outerHTML"))
        print("PARENT",el.evaluate("(e)=>e.parentElement.outerHTML")[:4000])
