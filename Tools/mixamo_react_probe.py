from playwright.sync_api import sync_playwright
import json
with sync_playwright() as p:
    b=p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    pg=[pg for c in b.contexts for pg in c.pages if "mixamo.com" in pg.url][0]
    data=pg.evaluate("""() => {
      const e=[...document.querySelectorAll('.product-animation-pack')].find(x=>(x.innerText||'').includes('Action Adventure Pack'));
      if(!e) return null;
      const keys=Object.keys(e).filter(k=>k.startsWith('__react'));
      const out={keys};
      for(const k of keys){
        let v=e[k];
        let n=0;
        while(v && n<8){
          try{
            out['lvl'+n]={
              key:k,
              type:v.elementType?.name||v.type?.name||String(v.type||''),
              memoizedProps:v.memoizedProps,
              pendingProps:v.pendingProps
            };
          }catch{}
          v=v.return;n++;
        }
      }
      return out;
    }""")
    print(json.dumps(data,default=str,indent=2)[:12000])
