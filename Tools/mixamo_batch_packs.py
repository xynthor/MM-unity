from playwright.sync_api import sync_playwright
from pathlib import Path
import re, time, traceback
packs=[
    "Great Sword Pack",
    "Pro Melee Axe Pack",
    "Male Injured Pack",
    "Locomotion Pack",
    "Pro Longbow Pack",
    "Magic Spell Pack",
]
out=Path(r"C:\MMUnityPort\Imports\Mixamo"); out.mkdir(parents=True,exist_ok=True)

def norm(s): return " ".join((s or "").split()).strip().lower()

with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    c=b.contexts[0]
    for pack in packs:
        try:
            print("\n===",pack,"===",flush=True)
            pg=c.new_page()
            q=pack.replace(" ","+")
            pg.goto(f"https://www.mixamo.com/#/?page=1&query={q}&type=Motion%2CMotionPack",wait_until="domcontentloaded",timeout=60000)
            pg.wait_for_timeout(2500)
            cards=pg.locator(".product-animation-pack")
            target=None
            for i in range(cards.count()):
                txt=norm(cards.nth(i).locator(".product-info p").inner_text())
                if txt==norm(pack):
                    target=cards.nth(i); break
            if target is None:
                raise RuntimeError(f"pack card not found; cards={cards.count()}")
            print("card found",flush=True)
            target.click()
            # Wait for selection title; preview itself may stay at 0%, that's okay.
            wanted=(pack+" ON").upper()
            ok=False
            for _ in range(30):
                pg.wait_for_timeout(500)
                try:
                    tail=pg.locator("body").inner_text(timeout=3000)[-1200:].upper()
                    if wanted in tail:
                        ok=True; break
                except: pass
            if not ok:
                raise RuntimeError("pack did not become selected")
            print("selected",flush=True)
            pg.get_by_role("button",name="DOWNLOAD").first.click()
            pg.wait_for_timeout(800)
            sels=pg.locator("select")
            if sels.count()<5:
                raise RuntimeError(f"download settings missing, selects={sels.count()}")
            sels.nth(1).select_option(label="FBX for Unity(.fbx)")
            sels.nth(2).select_option(label="No Character")
            sels.nth(3).select_option(label="30")
            sels.nth(4).select_option(label="none")
            modal=pg.locator(".modal").filter(has_text="DOWNLOAD SETTINGS")
            with pg.expect_download(timeout=120000) as di:
                modal.get_by_role("button",name="DOWNLOAD").last.click()
            d=di.value
            dest=out/d.suggested_filename
            d.save_as(str(dest))
            print("SAVED",dest.name,dest.stat().st_size,flush=True)
            pg.close()
        except Exception as e:
            print("ERROR",pack,repr(e),flush=True)
            traceback.print_exc()
            try: pg.close()
            except: pass
