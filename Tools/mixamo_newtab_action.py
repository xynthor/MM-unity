from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    c=b.contexts[0]
    pg=c.new_page()
    pg.goto("https://www.mixamo.com/#/?page=1&query=Action+Adventure+Pack&type=Motion%2CMotionPack",wait_until="domcontentloaded",timeout=60000)
    pg.wait_for_timeout(2500)
    print("URL",pg.url)
    card=pg.locator(".product-animation-pack").filter(has_text="Action Adventure Pack").first
    print("card",card.count(),card.is_visible())
    card.click()
    pg.wait_for_timeout(2500)
    print(pg.locator("body").inner_text(timeout=5000)[-1200:])
