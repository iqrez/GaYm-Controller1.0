import os, json, glob, sys, hashlib

SCHEMA_REQ = {"task_id","title","version","component","reference","files","wiring","results"}

def sha256(path):
    h=hashlib.sha256()
    with open(path,'rb') as f:
        for ch in iter(lambda:f.read(8192),b''): h.update(ch)
    return h.hexdigest()

def main():
    reports_dir = 'reports'
    os.makedirs(reports_dir, exist_ok=True)
    out = ['# WIRING GUIDE\n']
    schema = json.load(open(os.path.join(reports_dir,'schema.json'),'r',encoding='utf-8'))
    ok=True
    for jp in sorted(glob.glob(os.path.join(reports_dir,'GC-*.json'))):
        data=json.load(open(jp,'r',encoding='utf-8'))
        missing = [k for k in SCHEMA_REQ if k not in data]
        if missing:
            print("Missing fields in", jp, ":", missing); ok=False
        ref = data.get('reference',{})
        out.append(f"## {data.get('task_id')} — {data.get('title')}\n")
        out.append(f"**Component:** {data.get('component')}  ")
        out.append(f"**Reference consulted:** {ref.get('consulted')}  ")
        if ref.get('files'): out.append(f"**Reference files:** {', '.join(ref['files'])}  ")
        wiring = data.get('wiring',{})
        out.append("### Wiring Instructions\n")
        out.append(wiring.get('how_to_hook','(not provided)') + "\n")
        files = data.get('files',[])
        if files:
            out.append("### Files\n")
            for f in files:
                path=f.get('path'); h=f.get('sha256')
                if path and os.path.isfile(path) and not h:
                    h=sha256(path); f['sha256']=h
                out.append(f"- `{path}`  `{h or ''}`")
        out.append("")
    open(os.path.join(reports_dir,'WIRING_GUIDE.md'),'w',encoding='utf-8').write("\n".join(out))
    if not ok: sys.exit(3)

if __name__=='__main__': main()
