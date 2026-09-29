from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    btn=pg.get_by_role("button",name="DOWNLOAD")
    print("download buttons",btn.count())
    btn.first.click()
    pg.wait_for_timeout(2000)
    print(pg.locator("body").inner_text(timeout=5000)[-3500:])
    print("SELECTS")
    for i,s in enumerate(pg.locator("select").all()):
        try:
            print(i,s.input_value(),s.locator("option").all_text_contents())
        except Exception as e: print("selerr",i,e)
    print("BUTTONS")
    for i,e in enumerate(pg.locator("button").all()):
        try:
            t=e.inner_text(timeout=200).strip()
            if t: print(i,repr(t))
        except: pass
