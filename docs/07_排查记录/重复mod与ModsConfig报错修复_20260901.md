# 重复 mod 与「再次点击加入报错」修复记录

日期：2026-09-01
环境：RimWorld 1.6.4871 rev591 + Core_SK + HSK 系列（231 个激活 mod）

---

## 一、问题现象

在 Mod 管理器里把 **工作动画HSK（Show Me Your Tools - Forked）** 和 **美狐HSK拓展** 加入激活列表后，
左侧 mod 栏里它们仍然可见，于是又点了一次「加入」，随后报错。

## 二、根因

### 根因 A：ModsConfig.xml 的 activeMods 出现重复条目（直接报错源）

`C:\Users\admin\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\ModsConfig.xml`

```
第 180 行：    <li>meathax.ShowMeYourTools</li>     ← 原本正确位置（近战挥砍动画HSK 之前）
第 230 行：    <li>meathax.ShowMeYourTools</li>     ← 第二次点击「加入」时追加进去的
```

第二次点击后，同一个 packageId 在 activeMods 里存在两条。Gagarin（VR.MissileGirl）遍历激活列表去合并 def，
同一个 mod 被处理两次，同一个 XML 文件路径被塞进 Dictionary 两次，直接抛异常 —— Player.log 铁证：

```
GAGARIN: Loading error with error System.ArgumentException: An item with the same key has already been added.
Key: C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\工作动画HSK\1.6\Defs\Flecks_JobEffects.xml
  at Gagarin.LoadedModManager_Patch+LoadModXML_Patch.Postfix (...LoadModXML_Patch.cs:0)
  at Verse.LoadedModManager.LoadModXML / LoadAllActiveMods (...)
```

### 根因 B：23 组 mod 在本地 Mods 目录和 Steam 创意工坊各存一份（「重复 mod」本体）

扫描 `RimWorld\Mods`（237 个）+ `workshop\content\294100`（52 个）后，发现 23 个 packageId 同时存在两份。
本地那份是 HSK 改造版（比工坊原版多出 `1.6/Patches/99_ModAssistant标记.xml`、根目录 `HSK` 标记文件，About.xml 被改过），
工坊那份是原版。

RimWorld 的处理规则（取自 Assembly-CSharp.dll 内字符串）：

```
"Adding mods from mods folder: "  ->  "Adding mods from Steam: "
"Tried loading mod with the same packageId multiple times: {0}. Ignoring the duplicates."
```

即扫描顺序为 **官方 DLC → 本地 Mods 目录 → Steam 工坊**，先到先得，重复的直接忽略。
所以这 23 组当前生效的一直是本地改造版，工坊副本只是白占 356MB，且让人无法在两者间切换。

## 三、修复动作

| # | 动作 | 对象 | 结果 |
|---|------|------|------|
| 1 | 删除 activeMods 中后加入的重复条目 | `meathax.ShowMeYourTools`（原第 230 行），保留第 180 行位置（近战挥砍动画HSK 依赖它，必须在前） | 232 → 231 条，无重复 |
| 2 | 把 23 个工坊重复副本移出扫描范围 | `workshop\content\294100\<id>` → `_tmp\workshop_dup_隔离_20260901\<id>` | 磁盘 packageId 重复 23 → 0 组 |

- 备份：`_tmp/modsconfig_dedupe/ModsConfig.20260901-015112.bak`
- 隔离清单：`_tmp/workshop_dup_隔离_20260901/_manifest_20260901-020213.json`
- 一键还原：`_tmp/workshop_dup_隔离_20260901/restore.py`

## 四、复检结果

```
[1] activeMods 重复条目: 无
[2] 磁盘 packageId 重复（本地 Mods 目录 vs Steam 工坊）: 0 组
[3] 幽灵条目（激活列表里有、磁盘上找不到）: 0
[4] 依赖未激活: 0
[5] loadAfter 顺序违规: 0
结论: 全部通过
```

激活 mod 数 231 保持不变，游戏内容零变化（原本生效的就是本地改造版）。

## 五、后续注意事项

1. **Steam 可能把隔离的工坊副本重新下载回来**——订阅关系还在。若发现 `_tmp/workshop_dup_隔离_20260901`
   里的目录又被 Steam 拉回工坊目录，需要在 Steam 创意工坊退订这些 mod（本地改造版已在 Mods 目录，不受影响）。
   被隔离的 23 个工坊 ID 见 `_manifest_*.json`。
2. **体检脚本**：`_tmp/check_modlist.py`
   - `python check_modlist.py` 只体检不写盘
   - `python check_modlist.py --fix-dup` 自动去重 activeMods（保留首次出现 + 自动备份 + XML 校验）
   建议每次在 Mod 管理器里大改列表后跑一次。
3. **坑记录**：扫 About.xml 取 packageId 时，**不能**用正则抓第一个 `<packageId>`——
   那很可能是 `modDependencies` 里的依赖（例如 Camera+ 的第一个 packageId 是 `brrainz.harmony`）。
   必须解析 XML 后取 `ModMetaData` 的**直接子节点** `packageId`。

## 六、脚本清单

| 脚本 | 用途 |
|------|------|
| `_tmp/check_modlist.py` | mod 列表体检（重复 / 幽灵 / 依赖 / 顺序），`--fix-dup` 自动去重 |
| `_tmp/fix_modsconfig_dedupe.py` | 单次去重 activeMods（已执行，保留备用） |
| `_tmp/isolate_ws_dup.py` | 隔离工坊重复副本（已执行） |
| `_tmp/workshop_dup_隔离_20260901/restore.py` | 把隔离副本搬回工坊目录 |
| `_tmp/scan_dup_mods.py`、`scan2.py`、`scan3.py` | 排查期用的扫描脚本（`scan3.py` 是正确的 XML 解析版） |
