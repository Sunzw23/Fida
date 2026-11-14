# Rules 重设计：规则手册 + 技能图鉴（每页一个主张）

状态：已实现（v3）。v1 把 8 个信息块塞进一页，密度过高；v2 改为「一页一件事」；实现阶段又把索引从侧栏改成独立页，并据实际渲染收紧了示例区与注脚的排版。
实现代码：`UI/Rules/`、`Campaign/SkillCodex*.cs`；渲染产物：`artifacts/rules-codex-720/`、`artifacts/rules-codex-1000/`、`artifacts/rules-codex-portrait/`。

## 0. 一句话方案

Rules 从「5 页静态说明」改为「4 页规则 + 每个技能各 1 页」，技能页只收录**已获得**与**已遇见**的技能。每个技能页只讲一件事：它做什么、一个例子、一行出处。其余信息进索引页与读装界面，不挤进同一页。

## 1. 现状诊断

当前实现：`UIReferenceSurface.cs` 的 `DrawRules` + `UIReferenceLayout`，页面固定在 `UIManager.RulesPageCount = 5`。
截图：`artifacts/online-soft2/rules-1280x720.png`、`rules-4-1280x720.png`，竖屏 `artifacts/audio-ui-portrait/rules-720x1280.png`。

结构问题：

1. 技能只是两页各 10 行的文字列表：没有图标、没有状态、没有层级，和 `DuelDraft`（奶油卡片 + 字形 + SP）差一档。
2. 清单永远展示全集，玩家看不出「我有哪几个」「我在哪里见过」。
3. 一页 10 条，每条都无法展开：没有例子、没有时机、没有来源。
4. 竖屏 5 个章节挤成两行 chip，技能页仍是长列表。

内容问题（重新设计必须一并修正，否则只是把错误排版得更漂亮）：

| # | 位置 | 现有文案 | 实际问题 |
|---|---|---|---|
| 1 | Your turn | 「Add the values and keep the last digit. 4 + 7 = 11, so the result is 1.」 | 现行规则不是求和取个位。流程是：改数字（`(base + addend) % 10`）→ 用**自己的两个数字**查效果表（`Logic/States/ApplyingEffectState.cs`）。这条示例在教一个不存在的规则。 |
| 2 | Skill timing | 「Skills with the same trigger resolve from top to bottom in your loadout.」 | 不成立。战役按目录（枚举）顺序结算（`CampaignBattleFactory` 用 `(int)Type` 排序），决斗按挑选顺序（`OnlineModeRules` 保留原顺序）。 |
| 3 | Number effects | 只列 7 行效果 | 缺三条关键规则：效果结算后数字归 1、0 是护盾（抵挡一次且跳过防御技能）、任何数字变化都会重新检定（连锁）。 |
| 4 | 技能说明（读装界面复用） | Same:「Once per turn, copy the result to both numbers.」 | 实现是重做一次选数字步骤、可同时改两个数字（`TongSkillState` → `ChooseNumberIndexState(changeBothNumbers: true)`）。 |
| 5 | 技能说明 | Immortal:「After attacking, set one number to 0-9.」 | 触发阶段是 `OrderedAttack`，即攻击已确认、伤害尚未结算时改数字。 |

数据统计：

- 可选中技能 **20** 个（Gui/Huo 无实现，不进图鉴）。
- 战役可产出/可遇见 **18** 个；**Seng、Dao 只在联机决斗出现**。
- 战役节点 29 个，奖励技能 18 种，敌方技能 13 种。

## 2. 设计原则：一页一件事

每条规则约束一页的上限，超出即删或移到别处：

| 页面 | 上限 |
|---|---|
| 技能页 | 4 个信息块、≤ 25 个英文单词、1 个数值例子、1 行注脚 |
| 规则页 | 4 个块（步骤 / 表格 / 阶段表 / 注脚），不叠加旁注与提示卡 |
| 索引页 | 1 个网格 + 1 行图例 |

三条硬约束：

1. **每页只有一个焦点**：技能页的焦点是「名字 + 效果」，其余元素都降到注脚级。
2. **留白 ≥ 35%**：内容区右侧与底部必须空出来，靠 64px 名字、170px 奖章、40px 正文的尺度差建立层级。
3. **不解释两遍**：一句话摘要与效果正文只保留一份（v1 的两者是重复的）。影响用法的事实（倍率先后、护盾）并入效果句，不另设 NOTE 段。

## 3. 页面模型

```
页码 0  Your turn        ┐
页码 1  Numbers          ├ 顶部 4 个章节 chip 直达
页码 2  Timing           │
页码 3  Skills（索引页）  ┘
页码 4… 每个技能 1 页
TotalPages = 4 + discoveredCount
```

- 索引页顺序 = 翻页顺序：`YOUR KIT` 在前，`SEEN IN PLAY` 在后，各自按 family → 名称排序。同一技能只出现一次（拥有优先）。
- 索引 tile：拥有的实心奶油底，遇见的 55% 透明度，两者都不显示未知技能。
- 深链接：Timing 页的阶段可点，直接跳到对应技能页（`4 + index`）。
- 翻页在 `0 … TotalPages-1` 环绕；发现集合变化后 clamp。

## 4. 桌面版式（画布 1920 × 1200）

保留 header 外框：`Rect(45,30,1830,135)`、返回键 `Rect(70,60,72,72)`、标题 `Rect(175,60,600,80)`。
**取消左栏**：20 行技能列表本身就是一页过载的信息，改为顶部 chip + 独立索引页。

| 区块 | 坐标（设计 px） |
|---|---|
| 章节 chip | `Rect(120 + i*400, 205, 380, 64)`，i=0..3 |
| 内容纸片 | `Rect(120, 310, 1680, 650)`，内边距 90 |
| 翻页 | `Rect(1400, 1020, 90, 80)` / `Rect(1660, 1020, 90, 80)`，页码居中 |
| 索引 tile | 4 列 × 3 行，`360 × 150`，列距 24，行距 24 |

### 4.1 技能页线框

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ ←  Rules                                                                     │
│   [ Your turn ]  [ Numbers ]  [ Timing ]  [ Skills ]                         │
│                                                                              │
│   ┌──────────────────────────────────────────────────────────────────────┐   │
│   │                                                                      │   │
│   │   OFFENSE · 2 SP                                                     │   │
│   │                                                                      │   │
│   │    ┌──────────┐                                                      │   │
│   │    │          │    ATTACK UP                                         │   │
│   │    │    A+    │                                                      │   │
│   │    │          │                                                      │   │
│   │    └──────────┘                                                      │   │
│   │                                                                      │   │
│   │    Before your attack's damage is set, add 1.                        │   │
│   │                                                                      │   │
│   │    ┌──────────────────────────────────────┐                          │   │
│   │    │  7 → 1 damage · with Attack Up → 2   │                          │   │
│   │    └──────────────────────────────────────┘                          │   │
│   │                                                                      │   │
│   │    Before your attack · every attack · earned at A1 Rustblade        │   │
│   └──────────────────────────────────────────────────────────────────────┘   │
│                                                        ←   5 / 18   →         │
└──────────────────────────────────────────────────────────────────────────────┘
```

纸片右下留空；仅叠一个 ink 4% 的巨号字形水印（视觉层次，不是信息）。

### 4.2 技能页的 4 个信息块

| 块 | 内容 | 规格 |
|---|---|---|
| 1 奖章 + kicker | `UIPaperFrameShape.Seal` 170×170，内嵌 56px 字形；上方 `FAMILY · N SP` | 一处即可，不再重复状态文字 |
| 2 名称 | 64px semibold，最多 2 行 | 页面唯一的大字 |
| 3 效果 | 40px，最多 2 行，≤ 18 个单词 | 讲清「什么时候、做什么、多大」 |
| 4 例子 | 奶油内嵌条，一行箭头链 | 数字用 TitleFont，30px |
| 注脚 | 22px muted 单行：`阶段 · 使用限制 · 获得处` | 三项用 `·` 分隔，不换行 |

状态（拥有/遇见）不再单独写文字：奖章与名字的墨色深浅已表达，索引页有图例。NEW 只加一个 8px 强调色角标，读过即消失。

### 4.3 索引页线框

```
│   [ Your turn ]  [ Numbers ]  [ Timing ]  [ Skills ]                 6 / 20  │
│   ┌──────────────────────────────────────────────────────────────────────┐   │
│   │  SKILLS                                                              │   │
│   │  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐             │   │
│   │  │ A+        │ │ D+        │ │ H2        │ │ A2        │             │   │
│   │  │ Attack Up │ │ Defense Up│ │ Double HP │ │ Double Atk│             │   │
│   │  │ 2 SP      │ │ 2 SP      │ │ 2 SP      │ │ 3 SP      │             │   │
│   │  └───────────┘ └───────────┘ └───────────┘ └───────────┘             │   │
│   │  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐             │   │
│   │  │ …         │ │ …         │ │ …         │ │ …         │             │   │
│   │  └───────────┘ └───────────┘ └───────────┘ └───────────┘             │   │
│   │  YOUR KIT · 4        SEEN ONLY · 2        1 / 2                      │   │
│   └──────────────────────────────────────────────────────────────────────┘   │
```

12 tile/页（20 个技能 = 2 页）。tile 只有字形、名字、SP 三项，点击进入该技能页。图例一行：`Filled = yours, faded = seen only`。

### 4.4 三张规则页（各自 ≤ 4 块）

- **Your turn**：4 个步骤块（`1 Choose` / `2 Add` / `3 Read the pair` / `4 Resolve`）横排，下方一条例子带 `1 and 1 → add 5 → 1 and 6 → pair attack, 2 damage`，再一行注脚「Effects reset your numbers to 1」。
- **Numbers**：两栏表格，左 `SINGLE`（7 / 8 → 1 damage；9 → heal 1；0 → shield），右 `PAIRS`（1+6 → 2；5+5 → 3；6+6 → 5；0+0 → heal 10）。底部一行注脚把「0 是护盾、抵挡一次」与「1+6 与 6+1 等价」并进同一行。
- **Timing**：8 行阶段表，每行 = 阶段名 + 该阶段已发现的技能名（用 ` · ` 分隔的文字，不做 chip）。底部一行注脚说明同阶段按固定顺序结算。点击技能名跳页。

```
│   [ Your turn ]  [ Numbers ]  [ Timing ]  [ Skills ]                         │
│   ┌──────────────────────────────────────────────────────────────────────┐   │
│   │  WHEN SKILLS FIRE                                                    │   │
│   │  Battle start      Double Health · Triple Health                     │   │
│   │  Turn start        Same · Emperor · Super Emperor                    │   │
│   │  Before attack     Attack Up · Double Attack · Dragon                │   │
│   │  Attack committed  Immortal                                          │   │
│   │  When hit          Defense Up · Double Defense · Stone               │   │
│   │  After dealing damage   Blood                                        │   │
│   │  After taking damage    God · Demon                                  │   │
│   │  After the attack ends  Dao                                          │   │
│   │  Stages resolve in the order shown.                                  │   │
│   └──────────────────────────────────────────────────────────────────────┘   │
```

## 5. 竖屏版式（画布 900 × 1600）

| 区块 | 坐标 |
|---|---|
| header | `Rect(30,30,840,135)`（不变） |
| 章节 chip | 2 × 2，`Rect(45 + c*405, 200 + r*86, 390, 76)` |
| 内容纸片 | `Rect(45, 380, 810, 980)` |
| 底部翻页 | 沿用 `layout.Previous / Next` |
| 索引 tile | 2 列 × 5 行，`385 × 150`，10 tile/页（20 个 = 2 页） |

```
┌─────────────────────────────┐
│ ←  Rules                    │
│ [Your turn]      [Numbers]  │
│ [Timing]         [Skills]   │
│ ┌─────────────────────────┐ │
│ │ OFFENSE · 2 SP          │ │
│ │ ┌────┐  ATTACK UP      │ │
│ │ │ A+ │                  │ │
│ │ └────┘                  │ │
│ │ Before your attack's    │ │
│ │ damage is set, add 1.   │ │
│ │ ┌─────────────────────┐ │ │
│ │ │ 7 → 1 · +1 → 2      │ │ │
│ │ └─────────────────────┘ │ │
│ │ Before attack · always  │ │
│ │ · earned at A1          │ │
│ └─────────────────────────┘ │
│        ←   5 / 18   →       │
└─────────────────────────────┘
```

竖屏注脚允许折成 2 行；效果正文最多 3 行、36px。

## 6. 技能页文案（1 句效果 + 1 个例子 + 1 行注脚）

规则：效果句 ≤ 18 词、第二人称、只讲一个事实；例子必须是一条箭头链；注脚只放阶段、限制、出处。影响用法的事实写进效果句，不另加注释。

**Attack Up**
- Effect: `Before your attack's damage is set, add 1.`
- Example: `7 → 1 damage · with Attack Up → 2`
- Footnote: `Before attack · every attack · earned at A1 Rustblade`

**Blood**
- Effect: `After your attack deals damage, heal the same amount.`
- Example: `Deal 3 → heal 3`
- Footnote: `After dealing damage · every attack · campaign reward`

**Dragon**
- Effect: `When you commit an attack, use your two numbers added together as the damage.`
- Example: `5 and 5 → 10 damage instead of 3`
- Footnote: `Before attack · every attack · campaign reward`

v1 里的 NOTE 段（倍率先后、护盾、回血上限）全部删除：能一句话说完的并入效果句，说不完的属于规则页（Timing / Numbers），不再进技能页。

## 7. 视觉规格

字阶（比 v1 更大，页内层级只有三档）：

| 用途 | 行高 | 字体 |
|---|---|---|
| 技能名 | 64 | `TitleFont`（SemiBold） |
| 效果句 | 40 | `Font`（Regular） |
| 例子 | 30 | `Font`，数字用 `TitleFont` |
| kicker / 注脚 | 22 | `CaptionFont`，kicker 全大写 |

材质与颜色：

- 底：`UIPaper.Background` + `UIPaper.Backdrop`（与菜单一致）。
- 纸片：`UIPaper.Face`，`Sheet(depth 4, cut 10)`；例子带用 `PanelLight` 半透明压在纸上。
- 唯一强调色 `UITheme.AccentBlue`：当前 chip、索引里「你的」tile 边缘、NEW 角标。正文一律 ink / muted。
- family 只用一个 6px 圆点：OFFENSE violet、DEFENSE cyan、VITALITY sand、COUNTER bright cyan、CONTROL muted。不染色文字。
- 发丝线只在「规则页表格」出现，技能页不用线框、不用描边。
- 水印字形 ink 4%，右下角，`UIPaperFrame` 裁切。

动效：进入 0.14s 位移（奖章 → 名字 → 效果 → 例子）；翻页沿用现有 crossfade；`UIMotion.Reduced` 全部直接到位。

## 8. 发现逻辑

```
Owned       = Local.Progress.UnlockedSkills ∪ Online.Progress.UnlockedSkills
Encountered = SeenSkill ∪ derive(completedNodes) ∪ battleRosters
derive(node)     = node.Rewards ∪ node.Enemies[].Skills ∪ node.Reinforcements[].Skills
battleRosters    = 每场战斗开始时，双方场上与援军的技能
```

口径：**进入战斗即记录**（看到对方带了什么即可，不需要它真的发动）。决斗同样适用，「只打决斗」的玩家会积累 `SEEN`，`OWNED` 为空。

存档：`%LOCALAPPDATA%/Fida/skill-codex.json`，与战役存档分开（本地/联机两条存档都合并进来）。

```json
{
  "version": 1,
  "encountered": ["DoubleAttack", "Dragon"],
  "read": ["DoubleAttack"],
  "firstSeen": { "DoubleAttack": "A1", "Dragon": "B2" }
}
```

- 用英文名持久化（`SkillCatalog.FromName` 解析），枚举重排不影响存档。
- 只增不减；重开战役**不**清空（图鉴记的是「见过」，不是进度）。
- 旧存档回填：首次加载用 `derive(completedNodes)` 幂等补齐。
- 写入时机：战斗开始、援军部署、战役完成、离开 Rules 页（清 NEW）。失败静默。

## 9. 代码落点

新增：

| 文件 | 职责 |
|---|---|
| `Campaign/SkillCodex.cs` | 发现集合（Encountered/Read/FirstSeen）、`Record`、`Derive`、`Merge` |
| `Campaign/SkillCodexStore.cs` | JSON 读写（名称字符串） |
| `UI/Rules/RulesBook.cs` | 页面模型：4 页 + 有序技能列表、`TotalPages`、`PageFor(skill)` |
| `UI/Rules/SkillCodexText.cs` | 20 条三段文案（Effect/Example/Footnote）+ family 映射 |
| `UI/Rules/UIRulesLayout.cs` | chip 条、纸片、索引网格几何（桌面 + 竖屏） |
| `UI/Rules/UIRulesSurface.cs` | 背景 / 页头 / chip 条 / 纸片外壳 |
| `UI/Rules/UIRulesReferencePage.cs` | Your turn / Numbers / Timing 三页 |
| `UI/Rules/UISkillCodexIndex.cs` | 索引页（4×3 或 2×5 网格 + 图例 + 分页） |
| `UI/Rules/UISkillCodexPage.cs` | 单技能页（4 块 + 注脚 + 水印） |

修改：

| 文件 | 改动 |
|---|---|
| `UI/UIReferenceSurface.cs` | 移除 Rules 绘制，只留 Online/Hosting/Connecting |
| `UI/UIScreenBuilder.Reference.cs` | `BuildReferenceRules` 装配 chip、内容页、索引、翻页 |
| `UI/UIManager.cs` | `RulesPageCount` → `RulesBook.TotalPages` |
| `GameStateMachine.cs` | `SetRulesPage` 用动态总数 clamp |
| `States/RulesState.cs` | 绑定 chip、索引 tile、阶段链接、翻页；键盘 ←/→ |
| `Scene/PlayingSceneManager.cs` | `Initialize*`、`AddCampaignRemotePlayer`、`OnReinforcementsDeployed` 记录阵容 |
| `UI/Campaign/CampaignSkillPresentation.cs` | 文案迁到 `SkillCodexText`，读装界面复用 |
| `tools/UiPreview/Program.cs` | 渲染新页面（空 / 3 / 20 技能、索引两页、竖屏） |
| `Doc/MenuUI.md` | 更新 Rules 段 |

规模估算：UI 约 700–1000 行（比 v1 少一页左栏组件）、领域与存档约 150 行、文案表约 120 行、测试约 200 行。

## 10. 20 个技能派生数据（验收对照，实现时由代码生成）

| 技能 | family | SP | 阶段 | 限制 | 来源 |
|---|---|---:|---|---|---|
| Attack Up | OFFENSE | 2 | Before attack | every attack | 战役奖励 |
| Defense Up | DEFENSE | 2 | When hit | unlimited | 战役奖励 |
| Double Health | VITALITY | 2 | Battle start | once per battle | 战役奖励 |
| Double Attack | OFFENSE | 3 | Before attack | every attack | 战役奖励 |
| Double Defense | DEFENSE | 3 | When hit | unlimited | 战役奖励 |
| Triple Health | VITALITY | 3 | Battle start | once per battle | 战役奖励 |
| God | CONTROL | 3 | After taking damage | once per turn | 战役奖励 |
| Immortal | CONTROL | 3 | Attack committed | once per turn | 战役奖励 |
| Seng | CONTROL | 2 | After taking damage | once per turn | 仅联机决斗 |
| Dao | CONTROL | 2 | After the attack ends | once per turn | 仅联机决斗 |
| Demon | COUNTER | 3 | After taking damage | unlimited | 战役奖励 |
| Blood | VITALITY | 3 | After dealing damage | unlimited | 战役奖励 |
| Same | CONTROL | 3 | Turn start | 每回合一次（重做选数字） | 战役奖励 |
| Emperor | CONTROL | 3 | Turn start | unlimited | 战役奖励 |
| Dragon | OFFENSE | 5 | Before attack | every attack | 战役奖励 |
| Transform | OFFENSE | 5 | Before attack | every attack | 战役奖励 |
| Super God | CONTROL | 6 | After taking damage | unlimited | 战役奖励 |
| Super Immortal | CONTROL | 6 | Attack committed | unlimited | 战役奖励 |
| Super Emperor | CONTROL | 6 | Turn start | unlimited | 战役奖励 |
| Stone | DEFENSE | 2 | When hit | unlimited | 战役奖励 |

## 11. 测试与验收

单测：

1. `RulesBook`：0 / 1 / 20 技能时 `TotalPages`；KIT 在 SEEN 前；同 family 按名称；已拥有不进 SEEN。
2. `SkillCodex`：合并幂等；`Derive` 补齐；名称 ↔ 枚举往返；坏 JSON 返回空集合不抛。
3. `SkillCodexText`：每个 `SelectableSkills` 有三段文案；效果句 ≤ 18 词、例子含 `→`、注脚含 3 项；费用/family/阶段与代码一致。
4. 页码：环绕与 clamp；发现集合收缩后不越界。
5. 索引分页：20 个技能 = 2 页，末页不足 12 个不越界点击。

渲染（`tools/UiPreview`）：

- 1600×1000、1280×720、720×1280；覆盖空图鉴、3 个、20 个技能、索引两页、NEW 角标、Timing 页跳转后的页面。
- 断言技能页内容块 ≤ 4、文字行数不超过规格（超出即截图失败，防止密度回潮）。
- F8 重叠调试器零告警；`UIMotion.Reduced` 无位移；手柄焦点顺序 = chip → 索引 tile → 翻页。

## 12. 风险与取舍

1. **信息被移走了**：限制、冲突、来源不在技能页主体，只在注脚一行和读装界面。若之后觉得不够，优先扩充读装界面，不往回塞技能页。
2. **新玩家打开 Rules 看不到技能**（需求直接后果）。空索引页给一行说明「skills appear here once you have owned or faced them」。
3. **页面可能显得空**。靠 64px 名字、170px 奖章、40px 正文和 90px 内边距成立；不加水印以外的装饰，不加第二个正文段。
4. **结算顺序不一致**（战役目录序、决斗挑选序）。统一会改变决斗手感，需单独确认；确认前 Timing 页只写「按固定顺序结算」。
5. 发现记录写盘过密会有 IO 抖动：只在战斗开始/结束、页面离开时写。

## 13. 待确认（实现时的默认取舍）

1. 「遇见」口径：进入战斗即记录（含双方阵容与援军），不要求技能真的发动。
2. 未遇见技能：完全隐藏；索引只显示「N of 20」。
3. 注脚保留三项：阶段 · 限制 · 获得处。
4. 同阶段结算顺序未改动玩法，Timing 页只写「按显示顺序结算」。

## 14. 实现说明

- 页面：`0 Your turn` / `1 Numbers` / `2 Timing` / `3 Skills` / `4…` 技能页；`RulesBook.TotalPages = 4 + 已发现`。
- 索引：桌面 4×3=12 tile/页，竖屏 2×5=10 tile/页；拥有实心 + 左侧强调条，仅见过降到 74% 透明度，未读点 9px 强调色方块。
- 技能页四块 + 一行注脚；示例区最终用上下两条发丝线而不是填充纸片——半透明纸片会带出材质 backing 的灰色底，在奶油色纸上显脏。
- 竖屏技能页把整组内容在纸片内垂直居中；桌面保持左上对齐、右下留白给水印。
- `SkillCodex` 以英文名写盘（`%LOCALAPPDATA%/Fida/skill-codex.json`）；已完成节点的奖励/敌方/援军技能在读取时推导，不迁移旧档。
- 遭遇记录点：`Initialize`（决斗）、`InitializeCampaign` / `InitializeCampaignFromSnapshot`、`AddCampaignRemotePlayer`、`OnReinforcementsDeployed`。
- 文案：20 条集中在 `UI/Rules/SkillCodexText.cs`；同时修正了读装界面的 Same、Immortal 两条与实现不符的说明。
- 字体：四个 spritefont 增加 `→`(U+2192) 字形，文案里不再用 ASCII `->` 代替箭头。

验证状态：

- `tools/UiPreview --reference-only` 通过结构检查：页数 = 4 + 已发现；拥有者排在仅见过之前；索引 tile 数与分页一致；Timing 链接可按页绑定；技能页自建纸片。
- 已渲染 1600×1000、1280×720、720×1280 三套截图（`artifacts/rules-codex-*`）。
- 新增 `Fida.Tests/SkillCodexTests.cs`、`Fida.Tests/RulesBookTests.cs`；本机沙箱禁止测试宿主打开父进程句柄，`dotnet test` 需在正常终端运行。

后续可做：把 `CampaignSkillPresentation` 的 family/说明完全并入 `SkillCodexText`（现在只对齐了文案）；索引 tile 的名字与 SP 仍可再放大一级；Android 真机触摸验收。
