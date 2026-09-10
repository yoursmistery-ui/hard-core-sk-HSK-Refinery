# Material Numbers（材料数值）

[English](README.md) | [简体中文](README.zh-CN.md)

![材料数值预览](About/Preview.zh-CN.png)

Material Numbers 是一款面向 RimWorld 1.6 的材料对比 Mod，设计思路参考并结合了 [Stuff List (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=2798767227) 和 [Numbers](https://steamcommunity.com/sharedfiles/filedetails/?id=1414302321)：它像 Stuff List 一样集中列出游戏及其他 Mod 已加载的材料，又像 Numbers 一样允许玩家自由选择属性列、排序数据、调整表格布局并保存预设。Material Numbers 是独立实现，不需要安装这两个 Mod。

## 设计参考

- [Stuff List (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=2798767227)：提供了“把大量材料集中到同一张表中进行横向比较”的核心使用场景。
- [Numbers](https://steamcommunity.com/sharedfiles/filedetails/?id=1414302321)：提供了“由玩家选择列、排序数据和保存常用视图”的表格交互思路。

Material Numbers 将两种思路用于材料管理，并重新实现了属性发现、材料分类、倍率语义和全局预设系统。以上项目仅为设计参考，不是本 Mod 的前置依赖。

## 快速上手

1. 打开游戏底部的“材料”主标签。
2. 从左上角选择一套内置预设，例如“建造”“护甲与衣物”或“采集与生产”。
3. 使用第二行按钮限定材料大类、物品类型和库存范围，例如“全部定义 + 食物 + 当前地图”。
4. 点击“选择列”添加或移除需要比较的属性；点击列标题进行排序，拖动标题调整顺序或宽度。
5. 调整满意后点击“保存”。内置预设会提示另存为自定义预设，之后可在所有存档中继续使用。

表头中的 `=`、`×`、`+` 分别代表基础值、材料倍率和材料加成。第一次使用时先选一个接近目标的内置预设，再少量增删列，通常比从空白表格开始更方便。

## 主要功能

- 自动发现所有已加载的 `ThingDef` 物品定义，并支持按物品类型筛选。
- 展示物品数量、基础属性、材料倍率、材料加成和来源 Mod 等信息。
- 自动发现 Mod 扩展中可安全读取的 `IEnumerable<StatModifier>` 与 `IDictionary<StatDef, float>` 属性容器。
- 无需硬依赖即可识别 `SurvivalToolsLite.StuffPropsTool.toolStatFactors` 工具属性。
- 支持列搜索、添加和移除、拖动排序、宽度调整、横向滚动及数值排序。
- 支持材料大类、物品类型、当前地图、当前储存区以及物品名称筛选。
- 支持全局自定义预设；同一套预设可以在不同存档中使用。
- 暂时缺失的 Mod 属性列不会从预设中删除，重新启用对应 Mod 后会再次出现。

本 Mod 不强制依赖 Stuff List、Numbers、Survival Tools Lite、HSK 或 Harmony。

## 属性标记

Material Numbers 根据属性在 `ThingDef` 中的实际来源判断语义，而不是根据属性名称或数值大小猜测：

| 标记 | 数据来源 | 含义 | 中性值 |
| --- | --- | --- | --- |
| `=` | `ThingDef.statBases` | 材料自身的基础属性 | 未定义时显示 `-` |
| `×` | `stuffProps.statFactors` | 使用该材料时应用的倍率 | `100%` |
| `+` | `stuffProps.statOffsets` | 使用该材料时追加的加成 | `0` |

例如，`MaxHitPoints` 材料倍率为 `4` 时会显示为“耐久度上限 × 400%”，而 `Beauty` 材料加成为 `6` 时会显示为“美观度 + 6”。未显式定义的中性倍率或加成会以灰色显示。

这些列展示的是游戏定义中的原始组成部分，不会把基础值、倍率和加成合并计算成某个具体建筑或装备的最终属性。

## 内置预设

Mod 提供七套内置布局：

- **概览**：数量、价值、重量、耐久度、美观度、易燃性和堆叠上限。
- **建造**：建造与制造耗时、耐久度、美观度、易燃性、开门速度和重量。
- **护甲与衣物**：锐器、钝器和热能护甲，冷热绝缘、负重、移动速度及耐久度。
- **采集与生产**：采矿、挖掘产量、砍树、植物工作、建造、锻造、烹饪和屠宰效率。
- **武器制作**：武器伤害、穿透、攻击冷却、制造耗时、重量和耐久度。
- **舒适与睡眠**：休息效率、舒适度、清洁度、美观度、耐久度和易燃性。
- **交易与库存**：数量、市场价值、重量、堆叠上限、制造耗时和美观度。

内置预设不会覆盖玩家保存的自定义预设。修改内置布局后可以使用“另存为”创建自己的版本。

## 筛选方式

材料范围和库存范围是两套相互独立的筛选条件：

- 材料大类：常用材料、全部定义、金属、石材与陶瓷、木材、织物与皮革、塑料与玻璃、其他定义。
- 物品类型：按当前 Mod 列表中实际存在的 `ThingCategoryDef` 多选，例如食物、药物、武器、衣物和资源。
- 库存范围：全部已加载、当前地图、当前储存区。

默认“常用材料”会隐藏无法识别的特殊材料定义，减少箱子、部件或其他非典型材料的干扰。选择物品类型时会自动切换到“全部定义”；兼容性排查时也可以手动切换到“全部定义”或“其他定义”。

## 安装

1. 下载仓库或发布包。
2. 将整个 `Material-Numbers` 文件夹放入 RimWorld 的本地 `Mods` 目录。
3. 确认安装后至少存在以下路径：

```text
Material-Numbers/
├─ About/About.xml
├─ 1.6/Assemblies/MaterialNumbers.dll
├─ 1.6/Defs/
└─ Languages/
```

4. 在游戏 Mod 列表中启用 **Material Numbers**。

仅支持 RimWorld 1.6。

## 构建

项目目标框架为 .NET Framework 4.7.2，并使用 `Krafs.Rimworld.Ref` 1.6 引用包。Release 构建会将 `MaterialNumbers.dll` 复制到 `1.6/Assemblies`。

本仓库开发工作区使用以下隔离构建命令：

```powershell
& "G:/Github/Game/Rimworld/_tooling/scripts/Build-RimWorldMod.ps1" `
  -Project "G:/Github/Game/Rimworld/Material-Numbers/Source/MaterialNumbers/MaterialNumbers.csproj" `
  -Configuration Release
```

项目不需要 Harmony，也不需要在构建时引用或分发 RimWorld 私有反编译源码。

## 扩展接口

其他 Mod 可以实现 `IMaterialColumnProvider`，并调用 `MaterialNumbersRegistry.Register(provider)` 注册新的材料列。扩展只需描述列和读取函数；属性发现缓存、布局状态与表格绘制由 Material Numbers 负责。

## 当前范围

- 仅支持 RimWorld 1.6。
- 预设在所有存档间全局共享。
- 当前版本不提供预设导入与导出。
- 本项目采用 MIT License，详见 [LICENSE.md](LICENSE.md)。

## TODO

- 为食物、药物、武器、种植作物等定义类型补充专用属性提供器，例如营养值、药效、伤害和产量。
- 在现有通用 `ThingDef` 属性表基础上，提供面向不同物品类型的专用列预设。
