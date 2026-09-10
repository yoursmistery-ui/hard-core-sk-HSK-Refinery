Performance Fish 本地修复说明(2026-08-10)
============================================

问题
----
游戏日志中反复出现:
  System.InvalidOperationException: Operation is not valid due to the current state of the object.
  at FisheryLib.Collections.IndexedFishSet`1[T].Add
  at PerformanceFish.Listers.ThingsPrepatches.AddToTypeList
  at Verse.ListerThings.Add

表现: 小人搬运/放置物品到工作台时工作失败;小人死亡流程 phase 5(生成尸体)抛异常,
导致尸体不生成、小人直接消失。

根因
----
Performance Fish 用 Fishery 的 IndexedFishSet(按 thingIDNumber 去重的集合)缓存
ListerThings 按类型分组的物品表。ListerThings.Add 被调用时若同一物品已在缓存中
(例如被搬运中的物品并未从 lister 移除,或某些 mod 的生成路径重复调用 Add),
IndexedFishSet.Add 会抛 InvalidOperationException,中断 Thing.SpawnSetup,
进而中断放置物品 / 生成尸体等流程。

修复内容(仅 1.6 目录的 PerformanceFish.dll)
--------------------------------------------
在 ThingsPrepatches 中新增静态方法 TryAddToList(IList, object):
  若集合已包含该物品(按 key 判重)则跳过 Add,否则正常 Add。
并把 AddToTypeList 中原本直接调用 IList.Add 的指令改为调用该新方法。
效果: 重复添加不再抛异常,缓存保持集合语义;正常添加行为与性能不变。

涉及文件
--------
1.6\Assemblies\PerformanceFish.dll          已打补丁(2026-08-10)
1.6\Assemblies\PerformanceFish.dll.orig     原始文件备份(用于回退)
1.6\Assemblies\PerformanceFish.pdb.orig     原始 PDB 备份

回退方法
--------
关闭游戏后:
  1) 删除 1.6\Assemblies\PerformanceFish.dll
  2) 把 1.6\Assemblies\PerformanceFish.dll.orig 重命名为 PerformanceFish.dll
  3) 把 1.6\Assemblies\PerformanceFish.pdb.orig 重命名为 PerformanceFish.pdb

或者直接重新订阅/重新下载原版 mod 覆盖。

替代方案(不改 DLL)
------------------
游戏内 Mod 设置 -> Performance Fish -> Prepatches 分类下,关闭 ThingsPrepatches
一组的全部条目(Add / Remove / Contains / Clear / GetThingsOfType 等),重启游戏,
可恢复原版 ListerThings 行为(代价: 失去该项性能优化)。

重新打补丁
----------
工具位于工作区 C:\Personal\Project\ratkin-patch\tmp\pffix\PFFixTool.exe
运行前关闭游戏,执行:
  PFFixTool.exe patch
验证:
  PFFixTool.exe verify

注意
----
已消失的小人无法找回(尸体未生成);建议回退到出现该 bug 之前的存档。
1.4 / 1.5 目录的 DLL 未改动。
