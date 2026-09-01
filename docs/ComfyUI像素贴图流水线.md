# ComfyUI 像素贴图流水线（搭建与使用）

> 状态（2026-08-31 实测）：**像素化/规格段已全绿，抠图段 5/10 不合格（背景泄漏），修复中**。
> 修复并重跑全绿前，本文档**不登记** `00_HSK参考文档合集导航.md`（AGENTS.md §1：未自验通过的成果不进导航）。
> 工具装在 `C:/Personal/tools/`（工作区外），**不进 Mods 双目录同步**。

## 1. 定位与规格链

AI 产出定位 = **草稿**（画风铁律见 skill `rimworld-texture` §3：成品仍需手修或脚本核对）。
流水线单格物件规格链，全部整数比，任何一步非整数缩放即报废：

```
SD1.5 512×512 生成 (txt2img / img2img)
  → ImageScale nearest-exact /4 → 128×128 像素设计网格
  → ImageQuantize 16色, dither=none（平涂）
  → 抠图: sprite_post 容差边界漫水（Python 侧，见 §5-2）
  → choke 1px 吃抗锯齿 halo → 描边环 4px 纯黑（= 8px@256，对齐规范）
  → 索引重复 ×2（绝不插值）→ 256×256 RGBA 交付档
```

对照 AGENTS.md §4 / skill §2：256ppt 单格、描边 8px@256、最小笔画 6px@256（=3px@128）、透明区保留黑色数据防白边、半尺寸 128 判读合格才算数。

## 2. 环境

| 项 | 值 |
|---|---|
| 安装 | `C:/Personal/tools/ComfyUI`（源码版 0.34.0，venv py 3.12.10，torch cu128，sm_75 确认支持） |
| 启动 | `bash /c/Personal/tools/comfy_pixel/run_comfy.sh`（8188 端口） |
| 启动参数 | `--lowvram --disable-smart-memory`（8GB 卡：SD1.5 权重常驻显存、留后处理余量） |
| 网络铁律 | `HF_ENDPOINT=https://hf-mirror.com`（脚本已设）；**禁直连 huggingface.co** |
| 硬件约束 | RTX 2070 8GB / sm_75 无 FA2 → 只走 SD1.5，不跑 SDXL/Flux（§5-6） |

模型（`ComfyUI/models/`）：`checkpoints/v1-5-pruned-emaonly-fp16` + `loras/PixelArtRedmond15V`(0.85) + `vae/vae-ft-mse-840000-ema-pruned` + `controlnet/control_v11f1e_sd15_tile`、`control_v11f1p_sd15_depth` + `ipadapter/ip-adapter-plus_sd15`。
**缺口**：`clip_vision/`（IPAdapter 必需）、BiRefNet 权重 —— 补齐时只走 hf-mirror。

自定义节点（Stage A 刻意零依赖，下列均已装但当前 workflow 不用）：Manager、essentials、KJNodes、controlnet_aux、IPAdapter_plus、BiRefNet-Hugo。PixelArt-Detector 未装、非必需。

## 3. 组件职责（`C:/Personal/tools/comfy_pixel/`）

| 文件 | 职责 |
|---|---|
| `pipeline.py` | **图单一真源**：`GRAPH()` 语义图 → `to_api`/`to_ui` 双导出 workflow_api.json / workflow_ui.json，界面 Load 的 == 脚本提交的，不漂移。含 probe/run/verify/ui 四模式 |
| `sprite_post.py` | 确定性后处理：边界漫水抠图 → 描边环 → ×2 升交付档。**抠图在这，不在图里**（理由 §5-2） |
| `check_texture.py` | 交付规格六项核对（§6 表）。只依赖 numpy+Pillow |
| `draft_ref.py` | 无参考物件的体块草稿生成器（工字钢/口字钢等型材，PIL 画 3/4 视角条材、品红底 128），产出当 `--ref` 用，几何由草稿锁死 |
| `test_mask_algebra.py` | 掩膜代数（grow/subtract）单元测试 |
| `workflow_api.json` / `workflow_ui.json` | 由 pipeline.py 导出，勿手改 |
| `out/` | ComfyUI 原始产物（`out/ratkin_pixel/` 为像素稿 128 RGB） |
| `final/` | 交付稿（`*_rgba.png`，256 RGBA） |

当前 workflow 节点（全 core）：Checkpoint→LoRA→CLIP×2→KSampler(dpmpp_2m+karras, steps 24, cfg 6.5)→VAEDecode→ImageScale(/4)→ImageQuantize(16)→[matte=post 时到此为止，抠图后置]；`--ref` 时 LoadImage+VAEEncode 走 img2img。

## 4. 标准操作

```bash
# 启动服务
bash /c/Personal/tools/comfy_pixel/run_comfy.sh

cd /c/Personal/tools/comfy_pixel
# 探针: 只跑生成+像素化, 量四角背景色(提示词要求纯品红底, 以实测为准)
python pipeline.py probe --seed 20260831

# txt2img 生成并走完整后处理+自验（默认 --matte post）
python pipeline.py run --seed <defName专属seed>

# 改皮: 原版同类做 img2img 锁形状（路径=参考素材树 texPath, 勿凭空画）
python pipeline.py run --ref "C:/Personal/Project/ratkin-patch/参考素材/LudeonArtSource/RimWorld/Things/Item/Weapons/<同类>.png" --denoise 0.68

# 非刀具物件: 内置提示词是刀, 必须用 --prompt 覆盖, 并用 --prefix 区分产物名
python pipeline.py run --ref <参考png> --prompt "pixel art, one single <物件> sprite, ..." --prefix ratkin_pixel/<物件名>

# 核对（⚠ 无参只扫 out/; 交付稿必须显式传 final/*.png）
python pipeline.py verify final/*.png
# 或直调; Windows 控制台必须 PYTHONIOENCODING=utf-8, 参数是文件列表不是目录
PYTHONIOENCODING=utf-8 python check_texture.py final/*.png
```

约定：每 defName 固定 seed 并记录（复现/回归只改 prompt）；`--matte graph` 仅当背景绝对平涂时可用（图内精确色键，见 §5-2）。

## 5. 关键决策记录

1. **图单一真源**：workflow 两份 JSON 从 `GRAPH()` 生成，界面与脚本永不漂移；改图只改 `GRAPH()` 再 `python pipeline.py ui` 重导出。
2. **抠图放 Python 侧边界漫水，不放图内**：core `ImageColorToMask` 是精确等值匹配，而 median-cut 量化会把背景散成多个近似色条目 → 精确键既漏抠、又把同色主体（如棕刀柄）一起打洞。漫水只吃**与外框连通**的背景色，天然免疫同色孤岛；`build()` 有保护：可透明区 <3% 直接 REJECT 该候选（背景非纯色/主体顶满画框 → 作废重出）。
3. **参考图预处理必须 NEAREST**：双线性会把原版描边糊掉、破坏与 128 网格的对齐（同 skill §4 服装底图规则）。img2img 用 `--denoise 0.68` 贴参考。
4. **整数比铁律**：512/4→128、128×2→256。中间出现任何非整数缩放，量化后必炸脏色（check 第 3 项 2×2 块恒定率就是抓这个的）。
5. **quantize dither=none**：环世界平涂，抖动进游戏即噪点。
6. **不上 SDXL/Flux**：8GB 显存预算（SDXL UNet 5.2G + bigG 文本编码器 2.9G 已超卡，Turing 无 FA2）；1024→128 的 /8 会把 SDXL 的细节优势正好压在量化层下。要更细网格用 SD1.5 出 768（/3=256）。

## 6. 交付规格核对（check_texture 六项）

| # | 项 | 判据 |
|---|---|---|
| 1 | 尺寸 | 交付 256×256（像素稿 128×128） |
| 2 | alpha | 存在且基本二值，软边 ≤0.5%（防灰边） |
| 3 | 像素网格 | 2×2 块恒定率 =100%（抓非整数缩放/插值） |
| 4 | 描边 | 外轮廓带约 8px@256、以深色为主 |
| 5 | 色数 | ≤20 |
| 6 | 背景泄漏 | 贴边不透明像素=0；不透明区内与背景同色占比低 |

判定自动进行：RGBA→按交付档(256,step2)，RGB→按像素稿(128,step1)。

## 7. 实测结果与待办（2026-08-31，两轮）

**刀批次**：`final/` 10 张中 5 张全绿、5 张不合格。**根因已定论：5 张全是生成端候选缺陷**（背景非纯品红 / 主体顶满画框），后处理不可救——3 张被「可透明区<3%」REJECT 拦截，2 张主体贴边者曾漏进 final（已补「主体贴边即 REJECT」门）。坏稿移入 `final_quarantine/`，**换 seed 重出**是唯一出路。

**金属锭试产（img2img + Vile 重构包参考图）**：3 次迭代到全绿——①denoise 0.68 输出抽象化（人眼否决，规格却全绿：**核对脚本不能替代判读**）；②0.5 成形但灰锭撞灰合成底（同色占比 25%）；③改品红合成底后规格全绿，又揪出 VAE 品红过渡光晕残留（粉边）并修复。最终 `final/ingot_00003__rgba.png` 六项全绿 + 判读通过（亮面微带淡紫，手修项）。

**08-31 修复清单**：① `prep_ref` 合成底 灰→品红（无彩色主体撞色）；② `sprite_post` 增品红哨兵延伸（吃 VAE 过渡光晕，主体黑描边挡护不被误吃）；③ `build` 增主体贴边 REJECT；④ `pipeline` 增 `--prompt`/`--prefix` 覆盖（内置提示词是刀，非刀具物件必用）。

**型钢扩展试产（同日）**：工字钢/口字钢在 Core_SK/Vile 均无参考 → `draft_ref.py` 画体块草稿当 `--ref`（skill Stage 1 认可路线），工字钢 0.5 躯体发花、**0.42 干净**（几何敏感型 denoise 越低越贴草稿），口字钢 0.5 一次全绿。双双规格全绿 + 判读通过。

**待办**：
1. 5 张坏刀候选换 seed 重出（生成端缺陷；重出前先 `probe` 量背景、prompt 强化 flat background）。
2. 补 `clip_vision` 权重启用 IPAdapter + 接 ControlNet tile/depth（权重已就位未接线，Stage B）。
3. 全绿后再把本文档登记 `00_HSK参考文档合集导航.md`。
