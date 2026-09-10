# -*- coding: utf-8 -*-
import csv, html, os
BASE = r"c:\Personal\Project\ratkin-patch\特性拓展modHSK"
OUT  = os.path.join(BASE, "表")

def load(fn):
    with open(os.path.join(OUT, fn), encoding='utf-8-sig') as f:
        return list(csv.DictReader(f))

bpool = load('规则-背景关联特质池.csv')
hab   = load('规则-生活习惯特性.csv')

style = """<style>
 body{font-family:"Microsoft YaHei",sans-serif;margin:24px;color:#2c3e50}
 h1{font-size:22px;border-bottom:2px solid #3498db;padding-bottom:8px}
 h2{color:#7f8c8d;font-weight:normal;font-size:13px;margin-top:-8px}
 table{border-collapse:collapse;width:100%;font-size:13px;margin-top:14px}
 th{background:#34495e;color:#fff;padding:6px 8px;text-align:left;position:sticky;top:0}
 td{border:1px solid #ddd;padding:5px 8px;vertical-align:top}
 tr:nth-child(even){background:#f8f9fa}
 .dom{display:inline-block;padding:1px 8px;border-radius:10px;color:#fff;font-size:12px}
</style>"""

dom_color = {'战斗域':'#e74c3c','社交域':'#9b59b6','信仰域':'#8e44ad','情爱域':'#e91e63',
             '工作域':'#2ecc71','生活域':'#f39c12','身体域':'#16a085','灵能域':'#34495e'}

def render(rows, fields, title, anchor):
    h = [f'<div id="{anchor}"></div><h1>{title}（{len(rows)} 行）</h1>']
    h.append('<table><tr>' + ''.join(f'<th>{c}</th>' for c in fields) + '</tr>')
    for r in rows:
        tds = []
        for c in fields:
            v = html.escape(str(r.get(c,'')))
            if c in ('人格域','正负') or '人格域' in c and c=='人格域':
                if v in dom_color:
                    v = f'<span class="dom" style="background:{dom_color[v]}">{v}</span>'
            tds.append(f'<td>{v}</td>')
        h.append('<tr>' + ''.join(tds) + '</tr>')
    h.append('</table>')
    return '\n'.join(h)

html_bpool = render(bpool, list(bpool[0].keys()), '规则表A · 背景关联特质池（机制一）', 'a')
html_hab   = render(hab,   list(hab[0].keys()),   '规则表B · 生活习惯特性（机制二）', 'b')

doc = f"""<!DOCTYPE html><html><head><meta charset="utf-8">
<title>特性拓展modHSK · 规则表</title>{style}</head><body>
<div id="toc" style="background:#ecf0f1;padding:12px;border-radius:8px;margin-bottom:18px">
<b>目录：</b><a href="#a">规则表A·背景关联特质池</a> &nbsp;|&nbsp; <a href="#b">规则表B·生活习惯特性</a>
</div>
{html_bpool}
<hr style="margin:34px 0">
{html_hab}
</body></html>"""

with open(os.path.join(OUT, '规则表.html'), 'w', encoding='utf-8') as f:
    f.write(doc)
print('[OK] 规则表.html')