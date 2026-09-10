import xml.etree.ElementTree as ET
tree = ET.parse(r'c:\Personal\Project\ratkin-patch\鼠族HSK拓展\Patches\91_鼠族衣物科技树拆分.xml')
root = tree.getroot()
ops = root.findall('Operation')
add_ops = [op for op in ops if op.get('Class') == 'PatchOperationAdd']
rep_ops = [op for op in ops if op.get('Class') == 'PatchOperationReplace']
print(f'总Operation: {len(ops)}')
print(f'  Add: {len(add_ops)}')
print(f'  Replace: {len(rep_ops)}')
add0 = add_ops[0]
val = add0.find('value')
rps = val.findall('ResearchProjectDef')
print(f'  新增研究节点数: {len(rps)}')
for rp in rps:
    print(f'    {rp.find("defName").text} ({rp.find("label").text})')
# 列出所有 Replace 的 xpath 目标
print()
print('Replace 的目标(前10个):')
for op in rep_ops[:10]:
    xpath = op.find('xpath')
    if xpath is not None:
        print(f'  {xpath.text[:80]}')
print(f'  ... 共 {len(rep_ops)} 个 Replace')