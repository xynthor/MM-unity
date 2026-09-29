from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    c=b.contexts[0]
    pg=c.new_page()
    seen=set()
    for page in range(1,4):
        url=f"https://www.mixamo.com/#/?page={page}&query=Pack&type=Motion%2CMotionPack"
        pg.goto(url,wait_until="domcontentloaded",timeout=60000)
        pg.wait_for_timeout(2200)
        cards=pg.locator(".product-animation-pack")
        print("PAGE",page,"PACKS",cards.count())
        for i in range(cards.count()):
            t=" ".join(cards.nth(i).inner_text().split())
            if t not in seen:
                seen.add(t); print(t)
    pg.close()
