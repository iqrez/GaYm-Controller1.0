import sys, json, os
def main():
    if len(sys.argv)<2: print("usage: task_locator.py <ID>"); sys.exit(1)
    tid = sys.argv[1].strip()
    reg = json.load(open(os.path.join('agents','registry.json'),'r',encoding='utf-8'))
    for item in reg:
        if item['id'].lower()==tid.lower():
            print("Markdown:", item['md']); print("Machine:", item['json']); return
    print("Task not found:", tid); sys.exit(2)
if __name__=='__main__': main()
