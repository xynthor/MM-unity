from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    btn=pg.get_by_role("button",name="DOWNLOAD")
    print("buttons",btn.count())
    btn.first.click()
    pg.wait_for_timeout(1200)
    print(pg.locator("body").inner_text(timeout=5000)[-2500:])
    print("SELECTS",pg.locator("select").count())
    for i,s in enumerate(pg.locator("select").all()):
        try:
            print(i,s.input_value(),s.locator("option").all_text_contents())
        except Exception as e: print("ERR",i,e)
