from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    c=b.contexts[0]
    pg=c.pages[0] if c.pages else c.new_page()
    pg.goto("https://www.mixamo.com/", wait_until="domcontentloaded", timeout=60000)
    pg.wait_for_timeout(5000)
    print("URL",pg.url)
    print("TITLE",pg.title())
    print(pg.locator("body").inner_text(timeout=5000)[:2500])
