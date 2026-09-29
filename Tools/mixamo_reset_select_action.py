from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    pg.goto("https://www.mixamo.com/#/?page=1&query=Action+Adventure+Pack&type=Motion%2CMotionPack",wait_until="domcontentloaded",timeout=60000)
    pg.wait_for_timeout(3000)
    card=pg.locator(".product-animation-pack").filter(has_text="Action Adventure Pack").first
    print("card",card.count(),card.is_visible())
    img=card.locator(".product-image").first
    print("img",img.count(),img.is_visible())
    img.click()
    for i in range(20):
        pg.wait_for_timeout(500)
        tail=pg.locator("body").inner_text()[-600:]
        if "ACTION ADVENTURE PACK ON" in tail.upper():
            print("SELECTED",i); break
    print(pg.locator("body").inner_text()[-900:])
