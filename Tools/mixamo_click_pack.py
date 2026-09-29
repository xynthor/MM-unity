from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    target=pg.locator("div.product-animation-pack").filter(has_text="Sword and Shield Pack").first
    print("count",target.count())
    target.click()
    pg.wait_for_timeout(4000)
    print("URL",pg.url)
    print(pg.locator("body").inner_text(timeout=5000)[:6000])
