from playwright.sync_api import sync_playwright
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    data=pg.evaluate("""() => {
      const out=[];
      for (const e of document.querySelectorAll('*')) {
        const t=(e.innerText||'').trim();
        if (t==='Sword And Shield Pack' || (t.includes('Sword And Shield Pack') && t.length<200)) {
          out.push({tag:e.tagName, cls:e.className, id:e.id, text:t, html:e.outerHTML.slice(0,1800)});
        }
      }
      return out.slice(-10);
    }""")
    import json; print(json.dumps(data,indent=2))
