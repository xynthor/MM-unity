from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    card=pg.locator(".product-animation-pack").filter(has_text="Action Adventure Pack").first
    print("before",pg.locator("body").inner_text()[-250:])
    print("card count",card.count())
    card.scroll_into_view_if_needed()
    card.click(force=True)
    for i in range(20):
        pg.wait_for_timeout(500)
        tail=pg.locator("body").inner_text()[-500:]
        if "ACTION ADVENTURE PACK ON" in tail.upper():
            print("selected at",i); break
    print("after",pg.locator("body").inner_text()[-700:])
