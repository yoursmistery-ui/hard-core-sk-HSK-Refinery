AFU男女发型_HSK适配  (packageId: local.ratkin.afu.hairstyles)

== 内容 ==
整合两个 Steam 创意工坊 mod 为一个本地 mod:
  - AFU女士发型  (2020779666, 82 款)  原作者: 金兔子拉面
  - AFU男士发型  (2072106893, 37 款)  原作者: 金兔子拉面
共 119 款发型, 适配 HSK 各族(鼠族 / 金鼠族 / 美狐 / 阿莎丽 等 Humanlike/HAR 种族)。

== ID 规范(采用女士 mod 编号) ==
  - 女士: defName AFUhairF001~AFUhairF082, texPath Hair/AFUf001~AFUf082 (原样)
  - 男士: 由两位号对齐为三位数 —— defName AFUhairM001~AFUhairM037,
          label/texPath 同步为 AFUm001~AFUm037 (贴图文件一并重命名)
  - 男女共用单一分类 StyleItemCategoryDef: AFUHairCategoryA (合并后仅定义一次)
  - 每款发型均带 styleTags, 并统一追加 AFU_Style 供文化白名单命中

== 脸变体贴图(全部保留, 依 LoadFolders 自动切换) ==
  - Textures/Hair                = 默认: 女士根贴图(253) + 男士 Core 贴图(113, 已重命名)
  - VanillaFace/Textures/Hair    = 女士原版脸放大版覆盖(20 款, IfModNotActive 时加载)
  - ChibiFaceMaleOnly/...        = 男士 NL 静态脸覆盖(37, IfModActive Nals.* 时加载)
  - GloomyFaceMK2/...            = 男士 GL 脸覆盖(37, IfModActive Gloomy.* 时加载)
  - VeeFaceFA/...                = 男士 VEE 脸覆盖(37, IfModActive Lat.VeeFace 时加载)

== 种族适配补丁 ==
  Patches/10_文化AFU发型白名单.xml
  给各 HSK 种族文化的 styleItemTags 追加 AFU_Style 标签, 使 AFU 发型在角色生成/各派系中可选。
  目标文化: RK_Culture_Virtuard / RatkiniaTraditionCulture_Warlord / RatkiniaExoticCulture /
            OA_RK_RatkiniaTraditionCulture(金鼠族) / MihoCultures(美狐) / AsariCulture(阿莎丽)。
  采用 PatchOperationConditional 的 xpath 存在性门控(1.6 Operation 级 MayRequire 已失效),
  对应 mod 未启用时空转不报错。本 mod 已 loadAfter 上述 mod 与 ModIndicator。

== 加载顺序 ==
  Core_SK / HAR(erdelf.HumanoidAlienRaces) / RatkinRaceHSK / 鼠族HSK拓展 / 金鼠族 / 美狐 / 阿莎丽 ... 之后。

== 维护提示 ==
  若新增未知 HSK 种族的文化未覆盖, 在 10_文化AFU发型白名单.xml 里按同格式补一条 Conditional 即可。
  创意工坊自动更新会覆盖 Workshop 目录的两个原版 mod, 但本 mod 为独立本地拷贝, 不受影响。
