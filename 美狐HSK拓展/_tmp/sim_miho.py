# -*- coding: utf-8 -*-
"""美狐 HSK 适配补丁模拟验证: 验证新补丁 xpath 命中 + 最终 def 状态"""
import lxml.etree as ET, glob, os, sys, re

BASE = r"C:\Personal\Project\ratkin-patch\美狐HSK拓展"
def build_doc():
    defs = ET.Element("Defs")
    added = set()
    def add_file(path):
        try: t = ET.parse(path)
        except Exception as e: return
        root = t.getroot()
        if root.tag != "Defs": return
        for ch in root:
            key = (ch.tag, ch.get("Name") or ch.findtext("defName"))
            if key in added: continue
            added.add(key); defs.append(ch)
    for f in glob.glob(os.path.join(BASE, "1.6", "Defs", "**", "*.xml"), recursive=True):
        add_file(f)
    return defs, added

def apply_patch(doc, patch_file):
    pt = ET.parse(patch_file)
    ok = fail = 0
    for op in pt.getroot():
        cls = op.get("Class")
        xpath = op.findtext("xpath")
        if xpath is None: continue
        if not xpath.startswith('/'): xpath = '/' + xpath
        try:
            hits = doc.xpath(xpath)
            if hits: ok += 1
            else:
                fail += 1
                print(f"  !! 未命中: {cls} {xpath}")
        except Exception as e:
            fail += 1
            print(f"  !! 异常: {xpath}: {e}")
    print(f"  {os.path.basename(patch_file)}: 命中 {ok}, 未命中 {fail}")
    return fail

doc, added = build_doc()
print(f"虚拟文档 {len(added)} 个 def")
total = 0
for pf in ['HSK_1.6/Patches/ThingDefs_Misc/Miho_Apparel_Gap.xml',
           'HSK_1.6/Patches/ThingDefs_Misc/Weapons_Melee_HSK.xml',
           'HSK_1.6/Patches/ThingDefs_Misc/Miho_Buildings_Menu.xml']:
    total += apply_patch(doc, os.path.join(BASE, pf))
print(f"\n总未命中: {total}")
