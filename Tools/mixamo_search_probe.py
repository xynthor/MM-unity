from playwright.sync_api import sync_playwright
terms=["dodge","roll","falling","get up","hit reaction"]
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    box=pg.locator('input[type="search"]').first
    for term in terms:
        box.fill(term)
        box.press("Enter")
        pg.wait_for_timeout(1800)
        names=pg.locator(".product .product-info p").all_text_contents()
        print("\nSEARCH",term,"COUNT",len(names))
        for n in names[:20]: print(n.strip())
