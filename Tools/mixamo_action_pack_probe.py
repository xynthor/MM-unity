from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    box=pg.locator('input[type="search"]').first
    box.fill("Action Adventure Pack")
    box.press("Enter")
    pg.wait_for_timeout(1800)
    cards=pg.locator(".product")
    for i in range(cards.count()):
        t=cards.nth(i).inner_text().strip()
        if "Action Adventure Pack" in t:
            print("FOUND",i,repr(t))
            print(cards.nth(i).evaluate("(e)=>e.outerHTML")[:2500])
