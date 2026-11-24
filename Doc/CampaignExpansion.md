# 战役扩张设计：支线、难度与稀疏技能

> 设计稿 v1 · 尚未实现
>
> 目标：把战役从「一条会拿满技能的路线」变成「一条主线 + 许多有代价的支线」。
> 游戏内文本一律英文（受 `CampaignDisplayText_UsesNoCjkCharacters` 约束），本文用中文说明。

---

## 0. 一句话结论

| 项目 | 现在 | 设计后 |
|---|---:|---:|
| 战斗节点 | 29 | **64** |
| 章节 | 8 个路径（全为主线） | **24 个章节条目（主线 11 / 支线 13）** |
| 支线节点 | 4（都在第三章，且只是可选） | **39**（原有 4 个迁入支线章节 + 35 个新增） |
| 技能奖励条目 | 21 条 / 18 个不同 | **20 条 / 20 个不同（每个技能唯一来源）** |
| 全清能拿到的技能 | 18 | **20** |
| 其中来自主线 / 支线 | 15 / 3 | **10 / 10** |
| 每节点出技能的概率 | 72% | **31%** |
| 可招募同伴 | 1（Mara，默认就在队里） | **4**（Mara + 3 名靠支线结识） |
| 难度表达 | 血量 + 敌人技能 | **4 档难度 + 4 种誓约（限制）** |
| 章节视觉 | 全部同一种纸色 | **支线章节换纸色 + S 编号** |

新增节点字段只有 3 个（`tier`、`vows`、`playerHealth`），新增 1 个数据文件
（`Content/campaign/chapter-index.json`，章节索引），新增 1 个校验（难度契约），
其余全部是 JSON 内容。

**已落地（全部内容）**

- 章节索引 24 条：主线 11（Prologue、The Long Road、Chapter I、The Wall、Chapter II、
  Bell Tower、Courtyard、Chapter III、Dragon Tomb、Blank Archive、Finale），支线 13。
  Chapter I/II/III 是纯容器，Chapter I 的原有关卡收进 **The Wall**。
- 13 个支线章节 / 39 个支线节点，全部是 `frost`（Shelter 是 `slate`），带 `S` 编号与难度契约。
- 技能来源：**20 个技能 20 个唯一来源**。血/同/神/仙/石/僧/道 来自支线试炼，
  魔/帝 来自 BG/CG，超神/超仙/超帝 来自第三章可选节点；T01–T03 不再发重复技能。
- 同伴 4 名：Mara、Ila（AS3）、Bram（DS6）、Sable（ES3），未结识前不进名册。
- D3/D4 迁入 **Tomb Afterimages**，E3/E4 迁入 **Archive Overwritten**。
- 难度契约覆盖全部新支线；旧主线节点不声明 `tier`，保持原样。
- 界面：冷色纸、S 编号、难度徽章、战前简报誓约页、招募过滤、章节计数只算主线。
- 战役节点 29 → **64**；支线章节 2 → **13**；支线节点 5 → **39**。
---

## 1. 现状诊断

用数字说明为什么现在需要改：

1. **技能拿得太满。** 20 个可选技能里全清能拿到 18 个：其中 15 个挂在主线上，3 个在高阶可选节点上；
   只有僧（Seng）、道（Dao）没有任何来源。一周目走完，配装界面里几乎每个图标都亮着，
   「发现」的乐趣被「签到」取代。
2. **奖励有重复。** 加攻击、加防御、双倍生命各出现两次（P/T01、P/T02、T03/A3），
   21 条奖励里只有 18 个不同技能。
3. **预算才是真正的强度上限。** 节点预算 0–6（多数 3–5），`CampaignSkillBalance` 的费用是 2–6：
   就算解锁 18 个技能，一关也只能带 1–3 个。所以「技能多」目前只带来选择冗余，没有带来难度。
4. **支线几乎不存在。** D3、D4、E3、E4 是仅有的四个「可跳过」节点，全在第三章，
   而且难度只是「血量更多、技能更超模」，缺少不同的**难法**。
5. **难度没有可读的标签。** 地图上分不出「这一关是难在人多、难在没技能，还是难在不能带同伴」。

---

## 2. 三条设计主张

### 2.1 支线是别人的请求，条件就是难度

每个支线由一个具体的人提出具体的事（延续《长路》的写法：每个人都有自己没做完的事），
对方的**条件**直接变成这场战斗的限制：空手、独行、带伤、以寡敌众。
难度不靠"血更厚"，而靠"你能用的东西更少"。

### 2.2 技能是支线的终点，不是路边的补给

主线只给"语言"——起步技能和每个 Boss 的一件东西；支线给"方言"——构筑型技能；
最难的两条支线给"禁术"——僧、道（每回合额外的数字操作）。

### 2.3 难度是可校验的，不是凭感觉的

每个节点声明难度档与誓约，`CampaignDifficulty.Audit` 用一组带宽规则检查它是否自洽
（例如"空手节点最多 3 个敌人、总血量不超过 18"）。内容测试跑全目录，
新写的节点无法偷偷变成不可通关或名不副实。

### 2.4 章节是一本册子，支线是另一种纸

主线与支线的区别不只写在数值里，也写在**装订**上：支线是独立的章节，
换一种纸色、换一种编号。玩家扫一眼章节列表就知道哪几本可以不读，
而不必逐个点开看难度。

---

## 3. 难度体系

### 3.1 现有的四个难度要素（无需新代码）

| 要素 | 数据 | 实际效果 |
|---|---|---|
| 行动经济 | 敌人数量、增援时机 | 4 个敌人 = 一轮四次行动，玩家只有一次 |
| 技能组合 | `enemies[].skills` | 血（吸血）、石（每回合挡第一击）、魔（反伤）会把战斗拖长 |
| 技能点 | `skillBudget` | 0 = 完全空手；3 = 只能带一件三费技能 |
| 同伴名额 | `companions` | 显式 `[]` = 没人能来 |

### 3.2 新增：誓约（vow）

誓约是"别人提出的条件"，既是叙事也是机械约束，并且能被校验：

| vow | 地图显示 | 机械含义 | 校验规则 |
|---|---|---|---|
| `bare_hands` | BARE HANDS | `skillBudget = 0` | 必须为 0；单位 ≤ 3；总血量 ≤ 18；不得与 `wounded` 同用 |
| `alone` | ALONE | `companions = []` | 必须显式写 `[]` |
| `wounded` | WOUNDED 8 | `playerHealth` 低于 11 | 5 ≤ 生命 ≤ 9 |
| `outnumbered` | OUTNUMBERED | 开场就有 4 个单位 | `enemies.Count == 4`，且无增援 |

`bare_hands` 是最锋利的一条：玩家只剩数字本身。因此它被硬性限制为"3 个敌人以内、总血 ≤ 18"，
否则就是不可通关而不是难。

### 3.3 四档难度

| 档 | 标记 | 出现位置 | 单位数 | 总血量 | 誓约数 | 典型预算 | 预期回合 |
|---|---|---|---:|---:|---:|---:|---:|
| 1 | ◆ | 第一章见闻 | 2–3 | 6–12 | 0 | 3–4 | 5–8 |
| 2 | ◆◆ | 第一章末 ~ 第二章 | 3 | 12–17 | 0–1 | 3–4 | 8–12 |
| 3 | ◆◆◆ | 第二章末 ~ 第三章 | 3–4 | 15–21 | 1–2 | 0–4 | 12–18 |
| 4 | ◆◆◆◆ | 第三章末 ~ The Roofless Shelter | 4 | 21–28 | 2–3 | 0–3 | 18–30 |

规则：

- **主线永远不超过 2 档。** 想挑战才去支线。
- 同一档内用**誓约的种类**区分挑战类型，而不是继续加血：
  空手考数字基本功，独行考目标管理，带伤考节奏，以寡敌众考优先击杀。
- 空手节点的预算固定为 0，其余预算不参与难度契约（它是自由旋钮）。
- 同一种回复/改数类技能在敌方最多出现两次（见 D6）——它们会把战斗从 20 回合拖到 40 回合。

### 3.4 敌人角色表（新增内容按这套模板写）

| 角色 | 血量 | 技能 | 用途 |
|---|---:|---|---|
| 杂兵 | 3–4 | 无 | 提供行动次数，可被快速清掉 |
| 打手 | 4–5 | 加攻击 / 双倍攻击 | 输出压力 |
| 盾卫 | 5–6 | 加防御 / 双倍防御 | 拖慢集火 |
| 术士 | 5–6 | 神 / 仙 / 变 | 改变局面 |
| 精英 | 7–9 | 1–2 个技能 | 需要优先处理 |
| 头目 | 9–10 | 2 个技能 | 支线终点的对手 |

### 3.5 难度契约（新增校验，`CampaignDifficulty`）

| 编号 | 规则 |
|---|---|
| D1 | `bare_hands` ⇒ 预算 0，单位 ≤ 3，总血 ≤ 18，无 `wounded` |
| D2 | `alone` ⇒ `companions` 必须存在且为空 |
| D3 | `wounded` ⇒ 5 ≤ `playerHealth` ≤ 9 |
| D4 | `outnumbered` ⇒ 初始敌人正好 4 个且无增援 |
| D5 | 总血量、单位数、誓约数必须落在所属档的带宽内；`bare_hands` 节点豁免单位数与血量带（改由 D1 约束） |
| D6 | 同一种 `Xue` / `Shi` / `Shen` 最多出现在 2 个敌人身上 |
| D7 | 增援回合 `reinforcementRound` ≥ 2（不能一开场就来） |
| D8 | 没有任何主线节点以支线节点为父节点（支线可以串成链，但永远不锁主线） |
| D9 | 非容器、非终章的主线章节，至少有一个节点被其他章节的节点引用（防止"假主线"） |

---

## 4. 技能稀疏化

### 4.1 唯一来源原则

- 20 个可选技能，**每个只有一个来源节点**（`rewards` 里最多出现一次）。
- 主线 10 个，支线 10 个。
- 僧、道只出现在 4 档支线。

这条原则由内容测试保证：所有节点的 `rewards` 合并后正好等于 `SkillFactory.SelectableSkills`，
不重不漏。

### 4.2 奖励重排（旧 → 新）

| 节点 | 旧奖励 | 新奖励 | 说明 |
|---|---|---|---|
| P | PlusAttack, PlusDefence | 不变 | 国王教的两件 |
| T01 | PlusAttack | **无** | 与 P 重复；第一课的目的就是「没有新东西」 |
| T02 | PlusDefence | **无** | 与 P 重复 |
| T03 | DoubleHealth | **无** | 让位给 A3 作为唯一来源 |
| A1 | DoubleAttack | 不变 | |
| A2 | DoubleDefence | 不变 | |
| A3 | DoubleHealth | 不变 | 成为唯一来源 |
| AG | TripleHealth | 不变 | Boss 里程碑 |
| B1 | Xue | **无** | → BS3 |
| B2 | Tong | **无** | → BS6 |
| B3 | Mo | **无** | → BG |
| BG | — | **Mo** | Boss 里程碑 |
| B4 | Shi | **无** | → DS3 |
| C1 | Shen | **无** | → CS3 |
| C2 | Xian | **无** | → CS6 |
| C3 | Di | **无** | → CG |
| CG | — | **Di** | Boss 里程碑 |
| D2 / D3 / D4 | Long / SuperEmperor / SuperGod | 不变 | D3、D4 属于可选支线 |
| E2 / E3 | Bian / SuperImmortal | 不变 | E3 属于可选支线 |
| 新增支线 | — | Xue, Tong, Shen, Xian, Shi, Seng, Dao | 7 个 |
| 新增支线 | — | 招募 Ila, Bram, Sable | 同伴 |

结果：主线 10（PlusAttack、PlusDefence、DoubleAttack、DoubleDefence、DoubleHealth、
TripleHealth、Mo、Di、Long、Bian），支线 10（Xue、Tong、Shen、Xian、Shi、SuperEmperor、
SuperGod、SuperImmortal、Seng、Dao）。

### 4.3 玩家权力曲线（只走主线）

| 进度 | 已解锁 | 预算 | 可组出的东西 |
|---|---|---:|---|
| T02 | PlusAttack, PlusDefence | 2 | 只能挑一件（两件都是 2 费） |
| A1–A3 | + DoubleAttack / DoubleDefence / DoubleHealth | 3 | 挑一件：3 费的攻击技，或 2 费的生存技 |
| AG | + TripleHealth | 4 | 两件填充（加攻击 + 加防御），或一件三费技能 |
| BG / CG | + Mo / Di | 5 | 魔 / 帝 + 加防御（3+2），或双倍攻击 + 加攻击 |
| D2 / E2 | + Long / Bian | 5–6 | 龙 / 变 单独带（5 费；预算 6 也塞不进第二件 2 费技能） |
| F | 同上 | 5 | 龙或变单独带，或 双倍攻击 + 加防御 |

关键点：**预算一直是瓶颈，所以砍掉 8 个技能不会让玩家变弱，只是让选择变少。**
真正的强度提升来自支线技能——它们正好是"多一种解法"，而不是"数值更高"。

### 4.4 费用保持现状（已定）

不改 `CampaignSkillBalance`：加攻击、加防御仍然是 2 费。稀疏之后「预算 4 只能带一件」是
有意保留的取舍——玩家要在"两件便宜货"和"一件三费大招"之间做选择，而不是永远塞满。

### 4.5 不是简单删技能

被移走的 10 条奖励里，3 条是与 P 重复的（加攻击、加防御、双倍生命），7 个技能改由支线或 Boss 给出，
而且**难度更高**：血（吸血）来自一条关于债务的三节支线，僧、道来自通关前的最后一章。
玩家的记忆点是"我从谁手里学会的"，不是"我打了第几关"。

---

## 5. 支线结构

### 5.1 三节弧

每条支线是一个 3 节点的弧：

```text
[进入] 无誓约，介绍请求人        →  [变奏] 1 条誓约，条件变严        →  [试炼] 2–3 条誓约，给出技能/同伴
```

- 第 2、3 节必须以第 1 节为父节点（数据里是普通 `parents` 链，不需要新机制）。
- 试炼可以反复挑战，但技能只给一次（`CampaignProgress` 是集合，天然成立）。
- 弧与弧之间不互相依赖，玩家可以只做一半。

### 5.2 每章两条支线 + 见闻

| 章 | 支线弧 | 给什么 |
|---|---|---|
| 第一章 | The Smith's Errand / Names on the Wall | 同伴 Ila / 见闻 |
| 第二章 钟楼 | The Hunters' Debt / The Thirteenth Ring | Xue / Tong |
| 第二章 庭院 | The Altar's Answer / The Unfinished Gallery | Shen / Xian |
| 第三章 龙墓 | The Stone's Due / The Queue at the Gate | Shi / 同伴 Bram |
| 第三章 书库 | The Missing Page / The Quiet Reading Room | 同伴 Sable / 见闻 |
| The Roofless Shelter（支线章节） | The Weight / The Bench Repaired | Seng / Dao |

### 5.3 见闻线

给剧情、不给数值的支线（第一章 2 个、书库 3 个）。它们短、敌人弱，作用是让
"支线"不等于"刷数值"：有人只是想让你听完一件事。现有数据里本来就有 7 个无奖励节点，
这类内容与整体节奏一致。

### 5.4 地图呈现

- 支线是**独立的支线章节**（不是主线章节里的子文件夹），出现在同一张章节列表里，
  用不同的纸色 + `S` 编号区分。玩家一眼能看出哪几本册子可以不读。
- 章节卡片的 `n / m`：主线章节只统计主线节点，支线章节统计自己；
  支线永远不计入主线完成度。
- 章节详情面板：`SIDE CHAPTER` 眉标、强调色分隔线、索引里的章节简介。
- 节点卡片：难度菱形 + 誓约图标；详情面板一行 `SIDE CHAPTER · ◆◆◇ · VOW ALONE`。
- 配装界面顶部显示誓约横幅（`VOW — ALONE: no companion joins this battle`），
  避免玩家在开打前才发现队伍被禁。

### 5.5 章节扩展：索引文件与配色

**为什么要章节索引。** 现在章节名是从节点的 `chapter` 字符串里推出来的
（`"Chapter II - Bell Tower"` → `Chapter II / Bell Tower`），它能分组，但没法表达
「这是支线」「用哪种纸」。所以新增一个独立的数据文件：

`Content/campaign/chapter-index.json`

```json
{
  "chapters": [
    { "id": "bell-tower", "path": ["Chapter II", "Bell Tower"], "title": "Bell Tower",
      "kind": "main", "palette": "paper", "order": 30,
      "summary": "The tower keeps the last day of the war. Every bell raises the dead." },
    { "id": "side-hunters", "path": ["The Hunters' Debt"],
      "title": "The Hunters' Debt", "kind": "side", "palette": "frost", "order": 34,
      "art": "bell tower",
      "summary": "The hunters owe the tower a life. The collectors have come for it." }
  ]
}
```

**层级规则：每一页要么全是章节，要么全是关卡。** 现有地图在"有子章节"时画章节卡，
在"没有子章节"时画关卡卡，两者混在一页会重叠。所以：

- `Chapter I`、`Chapter II`、`Chapter III` 是纯容器，里面只有子章节；
- `Prologue`、`The Long Road`、`The Wall`、`Bell Tower`、`Courtyard`、`Dragon Tomb`、
  `Blank Archive`、各支线章节、`Finale` 直接挂关卡；
- 支线章节挂在自己所属的容器章节下（Chapter I 的支线在 Chapter I 里），
  `The Roofless Shelter` 是顶层支线章节，因为它是决战前的独立一站。
  这样任何一页的渲染方式都是确定的，根目录也始终只有 7 张卡。

| 字段 | 说明 |
|---|---|
| `id` | 稳定标识，用于测试、存档统计与本地化键 |
| `path` | 章节在列表里的层级（最后一段是显示名）；与节点 `chapter` 拆 ` - ` 的结果一致 |
| `title` | 显示名，省略时用 `path` 最后一段 |
| `kind` | `main` / `side`；**节点不再单独声明 kind，一律从所属章节继承** |
| `palette` | 纸色键，见下表；省略时继承最近的已声明祖先，再缺省为 `paper` |
| `art` | 复用现有章节插画的关键字（`bell tower` / `courtyard` / `dragon tomb` / `blank archive`）；省略时走现有的 `ChapterIndex` 规则。**不加新美术资源** |
| `order` | 列表排序（按阅读顺序；支线紧跟在入口章节之后） |
| `summary` | 章节详情面板里的一句话，替代目前"取第一个节点的 story"的临时做法 |

**配色（纸册感，不引入新美术资源）：**

| palette | 纸色 | 墨 | 强调色 | 用在 |
|---|---|---|---|---|
| `paper` | `#EDE8DB`（现状） | `#18222C` | 青 `#6EB2BE`（纸上的深青；选中描边仍用现有 `#8EDEE2`） | 主线章节 |
| `frost` | `#CBD8DA` | `#1A2A33` | 靛 `#5E6E9E` | 支线章节（默认） |
| `slate` | `#AEBAC2` | `#16232B` | 淡紫 `#7C74A8` | 决战前/番外（先只给 The Roofless Shelter） |

三组都是冷色：主线保持现有米白纸，支线换成偏蓝的霜色纸，决战前再压深一层。
青色留给"选中"、灰色留给"锁定"，支线用靛蓝——三种状态各有各的颜色，不会撞。

**章册化样式（都用现有绘制能力）：**

- 支线章节卡片：靛蓝编号 + 比主线更冷的纸色。**不加印章、不加贴纸**——按反馈去掉了，
  编号本身已经足够区分，画面也更干净。
- 编号规则：主线章节按 `order` 编 `01`、`02`……；支线章节在同一层里编 `S1`、`S2`……
  一眼能数出这一层有几条支线。
- 进入支线章节后，节点卡片左侧的标签（现在选中是青色）也用该章的强调色，
  纸面整体偏冷、偏蓝，玩家不会忘记自己在支线里。
- 面包屑（`Campaign` 标题）在支线章节里可以换成该章的强调色。

**不新建 UI。** 颜色本来就只是现有绘制方法的一个 `Color` 参数，所以这套方案不新增
贴图、控件、界面或状态机分支：

- `UICampaignRouteView` 的 `DrawChapterCard` / `DrawChapterDetails` / `DrawNode` / `DrawDetails`
  各加一个**可选**的 `ChapterStyle`（纸色、墨、弱墨、强调色、是否支线），默认值等于现在的常量；
  不传调用点的行为完全不变，所以现有预览截图和测试不用改。
- `CampaignChapterPalette` 只是一张三行的颜色表（key → 四个 `Color`），没有新资源。
- 章节卡、节点卡、详情面板、面包屑全部复用现有控件。**章节卡和节点卡是 `UIPaperFrame.Draw`
  的纯色面，本来就接受任意 `Color`**，纸色直接可用；详情面板的叠纸底色与按钮走现有 skin，
  保持不变，支线只换文字、分隔线、强调色和编号的颜色。
- 因此不需要新纸纹：`UIPaper.Sheet` 的四张 skin 不参与章节配色，现有四种纸纹继续用在
  按钮、票据和队伍条上。
- 章节插画也复用现有四张板块图：索引里的 `art` 关键字直接喂给 `UIPaper.ChapterLocation`，
  没有就直接落到现有的 `ChapterIndex` 兜底。支线章节能"借用"所在地的插画
  （猎人的债 → bell tower、石头的账 → dragon tomb），视觉上仍然属于那一章。
- 锁定态仍然走现有的 `LockedFace` + 洗淡插画，颜色不参与"锁没锁"。
- 唯一可选的额外改动：`UIPaper.ChapterLocation(..., Color tint = default)` 给插画加一个轻微冷调，
  默认 `Color.White` 等于不改。插画本身是暖色剪纸，放在霜色卡上就像"照片贴在冷色卡纸上"，
  不调也成立。

**主线章节 vs 支线章节的规则：**

| | 主线章节 | 支线章节 |
|---|---|---|
| 能否成为后续章节的入口 | 是 | **否**（校验：主线节点不得以支线节点为父） |
| 计入主线的 `n / m` | 是 | 否 |
| 章节内可以有子章节 | 可以（`Chapter II` 是纯容器） | 可以（一般不用） |
| 最短长度 | 1 个节点（Prologue） | 1 个节点起；见闻线通常 2–3 个 |
| 配色 | `paper` | `frost` / `slate` |
| 前提 | 由节点 `parents` 决定 | 由节点 `parents` 决定，可挂在任意主线里程碑之后 |

如果新增的主线章节没有被后续主线节点引用，它实际上就是支线——校验会把这种"假主线"报出来，
免得玩家以为必须打。

**章节清单（24 条）：**

| # | 章节 | 路径 | kind | palette | 节点 |
|---:|---|---|---|---|---|
| 1 | Prologue | Prologue | main | paper | P |
| 2 | The Long Road | The Long Road | main | paper | T01–T03 |
| 3 | Chapter I | Chapter I | main | paper | （容器） |
| 4 | The Wall | Chapter I / The Wall | main | paper | A1–A3, AG |
| 5 | The Smith's Errand | Chapter I / … | side | frost | AS1–AS3 |
| 6 | Names on the Wall | Chapter I / … | side | frost | AS4–AS5 |
| 7 | Chapter II | Chapter II | main | paper | （容器） |
| 8 | Bell Tower | Chapter II / Bell Tower | main | paper | B1–B5, BG |
| 9 | Courtyard | Chapter II / Courtyard | main | paper | C1–C5, CG |
| 10 | The Hunters' Debt | Chapter II / … | side | frost | BS1–BS3 |
| 11 | The Thirteenth Ring | Chapter II / … | side | frost | BS4–BS6 |
| 12 | The Altar's Answer | Chapter II / … | side | frost | CS1–CS3 |
| 13 | The Unfinished Gallery | Chapter II / … | side | frost | CS4–CS6 |
| 14 | Chapter III | Chapter III | main | paper | （容器） |
| 15 | Dragon Tomb | Chapter III / Dragon Tomb | main | paper | D1, D2 |
| 16 | Blank Archive | Chapter III / Blank Archive | main | paper | E1, E2 |
| 17 | The Stone's Due | Chapter III / … | side | frost | DS1–DS3 |
| 18 | The Queue at the Gate | Chapter III / … | side | frost | DS4–DS6 |
| 19 | Tomb Afterimages | Chapter III / … | side | frost | D3, D4（原属龙墓） |
| 20 | The Missing Page | Chapter III / … | side | frost | ES1–ES3 |
| 21 | The Quiet Reading Room | Chapter III / … | side | frost | ES4–ES6 |
| 22 | Archive Overwritten | Chapter III / … | side | frost | E3, E4（原属书库） |
| 23 | The Roofless Shelter | The Roofless Shelter | side | slate | WS1–WS6 |
| 24 | Finale | Finale | main | paper | F |

每个主线章节现在要么是纯容器（Chapter I/II/III），要么直接挂关卡（Prologue、The Long Road、
The Wall、Bell Tower、Courtyard、Dragon Tomb、Blank Archive、Finale）——同一页永远不会
既要画章节卡又要画关卡卡。Chapter I 的原有关卡因此收进 **The Wall** 子章节。

现有四个可选节点也换了归属：D3、D4 从「龙墓」移到 **Tomb Afterimages**，
E3、E4 从「书库」移到 **Archive Overwritten**。这样每个章节要么全主线、要么全支线，
颜色不会自相矛盾；主线的 `n / m` 也不再被可选节点稀释。
支线章节的插画按所在地借用现有板块（`art` 字段）：

| 支线章节 | `art` |
|---|---|
| The Smith's Errand / Names on the Wall | 省略（落到 `ChapterIndex` 兜底） |
| The Hunters' Debt / The Thirteenth Ring | `bell tower` |
| The Altar's Answer / The Unfinished Gallery | `courtyard` |
| The Stone's Due / The Queue at the Gate / Tomb Afterimages | `dragon tomb` |
| The Missing Page / The Quiet Reading Room / Archive Overwritten | `blank archive` |
| The Roofless Shelter | 省略（兜底） |

**添加一个章节的步骤**（全部是数据，不改代码）：

1. 在 `Content/campaign/chapters/` 新增或追加节点，`chapter` 写成 `"Chapter II - The Hunters' Debt"`。
2. 在 `chapter-index.json` 里加一条 `path: ["Chapter II", "The Hunters' Debt"]`，
   `kind: "side"`、`palette: "frost"`。
3. 节点用 `parents` 挂在主线节点下方（支线可以串链，但主线节点不能反过来以支线为父）。
4. 不需要改 UI、布局或坐标：章节列表、路线图、面包屑都从索引读取。

**兼容性。** 节点所属章节若没有在索引里声明，按 `main` + `paper` 处理并继续显示，
所以旧内容、测试里临时构造的节点、`--nodes-only` 预览都不会因为缺索引而失败。
索引里声明了但没有任何节点的章节会被忽略（相当于未启用）。

---

## 6. 内容清单（35 个新节点）

命名：`AS` = 第一章支线，`BS` = 钟楼，`CS` = 庭院，`DS` = 龙墓，`ES` = 书库，`WS` = 无顶棚。
文件：`11-chapter-one-side.json`、`23-bell-tower-side.json`、`24-courtyard-side.json`、
`32-dragon-tomb-side.json`、`33-blank-archive-side.json`、`50-roofless-shelter.json`。

### 6.1 第一章「失声的城墙」：5 节点（2 个支线章节）

弧① **The Smith's Errand**（支线章节 · frost · A1 的铁匠与学徒 Ila）

| ID | 节点名 | 父节点 | 档 | 誓约 | 敌人 | 预算 | 奖励 |
|---|---|---|---|---|---|---|---:|
| AS1 | Cold Quench | T03 | ◆ | — | Quench Tender 4、Hearth Guard 5（加防御） | 4 | — |
| AS2 | The Test Cut | AS1 | ◆ | — | Straw Man 3、Drill Master 4（加攻击）×2 | 3 | — |
| AS3 | Delivery at Dusk | AS2 | ◆◆ | ALONE | Door Guard 4（加防御）×2、Receiver 6（双倍防御） | 3 | **同伴 Ila** |

> 铁匠还在磨那把剑。Ila 替他送剑，条件是不许人陪——"你站在旁边，他们就不开门。"

弧② **Names on the Wall**（支线章节 · frost · 刻名字的石匠、没被换下的哨兵）

| ID | 节点名 | 父节点 | 档 | 誓约 | 敌人 | 预算 | 奖励 |
|---|---|---|---|---|---|---|---:|
| AS4 | Names on the Wall | T03 | ◆ | — | Mason 5（加防御）、Wall Hand 3 | 4 | — |
| AS5 | The Watch Relieved | AS4 | ◆◆ | WOUNDED 8 | Standing Watch 5（加防御）、Night Relief 4×2 | 3 | — |

### 6.2 第二章 · 钟楼支线：6 节点（2 个支线章节）

弧① **The Hunters' Debt**（支线章节 · frost → 血 Xue）

| ID | 节点名 | 父节点 | 档 | 誓约 | 敌人 | 预算 | 奖励 |
|---|---|---|---|---|---|---|---:|
| BS1 | Debt Collectors | AG | ◆◆ | — | Collector 5（血）、Ledger 6（双倍防御）、Hunter 4 | 4 | — |
| BS2 | The Blood Ledger | BS1 | ◆◆◆ | BARE HANDS | Ledger Keeper 7（血）、Collector 4×2 | 0 | — |
| BS3 | Final Payment | BS2 | ◆◆◆ | ALONE | Debt Master 8（血、加防御）、Collector 4（血）、Collector 4；阵亡 1 人后 **Ledger Shade 4** 加入 | 3 | **Xue** |

> 猎人欠钟楼一条命，收债的把账本摊开："按我们的规矩签，别用王冠那套。"

弧② **The Thirteenth Ring**（支线章节 · frost → 同 Tong）

| ID | 节点名 | 父节点 | 档 | 誓约 | 敌人 | 预算 | 奖励 |
|---|---|---|---|---|---|---|---:|
| BS4 | The Hour They Kept | BG | ◆◆ | — | Bell Twin 5（同）×2、Hour Hand 4 | 4 | — |
| BS5 | Two of One Hour | BS4 | ◆◆◆ | ALONE | Hour Keeper 8（同、加防御）、Bell Twin 5（同）×2 | 3 | — |
| BS6 | The Thirteenth Ring | BS5 | ◆◆◆ | BARE HANDS | Bell Twin 5（同）×2、Hour Keeper 7 | 0 | **Tong** |

### 6.3 第二章 · 庭院支线：6 节点（2 个支线章节）

弧① **The Altar's Answer**（支线章节 · frost → 神 Shen）

| ID | 节点名 | 父节点 | 档 | 誓约 | 敌人 | 预算 | 奖励 |
|---|---|---|---|---|---|---|---:|
| CS1 | Acolytes at the Altar | AG | ◆◆ | — | Acolyte 4（神）×2、Altar Voice 6（加防御） | 4 | — |
| CS2 | What the Wound Asks | CS1 | ◆◆◆ | WOUNDED 8 | Priest 6（神）、Acolyte 4（神）、Statue 5（双倍防御） | 3 | — |
| CS3 | The Answer Given | CS2 | ◆◆◆ | ALONE | Priest 8（神、加防御）、Acolyte 4（神）、Acolyte 4、Acolyte 3 | 3 | **Shen** |

弧② **The Unfinished Gallery**（支线章节 · frost → 仙 Xian）

| ID | 节点名 | 父节点 | 档 | 誓约 | 敌人 | 预算 | 奖励 |
|---|---|---|---|---|---|---|---:|
| CS4 | The Understudy's Line | CG | ◆◆ | — | Gallery Guide 5（仙）、Understudy 4×2 | 4 | — |
| CS5 | The Last Entrance | CS4 | ◆◆◆ | OUTNUMBERED | Guide 7（仙）、Understudy 5（加攻击）、Understudy 4、Doorman 4（双倍防御） | 3 | — |
| CS6 | No One Left to Play | CS5 | ◆◆◆ | BARE HANDS | Guide 6（仙）、Understudy 5（仙）、Exit Keeper 5 | 0 | **Xian** |

### 6.4 第三章 · 龙墓支线：6 节点（2 个支线章节）

弧① **The Stone's Due**（支线章节 · frost → 石 Shi）

| ID | 节点名 | 父节点 | 档 | 誓约 | 敌人 | 预算 | 奖励 |
|---|---|---|---|---|---|---|---:|
| DS1 | The Stone Tally | D2 | ◆◆ | — | Scale Keeper 6（石）、Cinder 5（龙）、Ward 5（双倍防御） | 4 | — |
| DS2 | What the Bones Counted | DS1 | ◆◆◆ | BARE HANDS | Scale Keeper 7（石）、Cinder 5、Cinder 4 | 0 | — |
| DS3 | The Keeper's Refusal | DS2 | ◆◆◆◆ | ALONE, WOUNDED 8 | First Scale 9（石、双倍防御）、Keeper 6（加防御）、Cinder 5；阵亡 2 人后 **Ward 4** 加入 | 3 | **Shi** |

> 守墓人守着一份早已算完的账："石头每回合只挡一次。你也只有一次。"

弧② **The Queue at the Gate**（支线章节 · frost · D1 的守门人 → 同伴 Bram）

| ID | 节点名 | 父节点 | 档 | 誓约 | 敌人 | 预算 | 奖励 |
|---|---|---|---|---|---|---|---:|
| DS4 | No Replacement Appointed | D1 | ◆◆ | — | Relieved Guard 5（加防御）、Gate Hand 4×2 | 4 | — |
| DS5 | The Long Watch | DS4 | ◆◆◆ | WOUNDED 8 | Watch 6（加防御）、Watch 5、Gate Hand 4 | 3 | — |
| DS6 | A Place at the Gate | DS5 | ◆◆◆ | ALONE | Gate Captain 8（双倍防御、加防御）、Gate Hand 5×2 | 3 | **同伴 Bram** |

> 替班的人终于来了。守门人放下矛，才发现自己已经不会走路——"你能带我走一段吗？"

### 6.5 第三章 · 书库支线：6 节点（2 个支线章节）

弧① **The Missing Page**（支线章节 · frost → 同伴 Sable）

| ID | 节点名 | 父节点 | 档 | 誓约 | 敌人 | 预算 | 奖励 |
|---|---|---|---|---|---|---|---:|
| ES1 | What Was Written Over | E2 | ◆◆ | — | Margin Warden 6（双倍防御）、Erased Clerk 5（变）、Ink Drudge 4 | 4 | — |
| ES2 | The Page Nobody Signed | ES1 | ◆◆◆ | BARE HANDS | Clerk 5（变）、Clerk 5（仙）、Warden 5 | 0 | — |
| ES3 | The Page Returned | ES2 | ◆◆◆◆ | ALONE, OUTNUMBERED | Chief Copyist 9（变、加防御）、Clerk 5（变）、Clerk 4、Drudge 4 | 3 | **同伴 Sable** |

弧② **The Quiet Reading Room**（支线章节 · frost · 见闻）

| ID | 节点名 | 父节点 | 档 | 誓约 | 敌人 | 预算 | 奖励 |
|---|---|---|---|---|---|---|---:|
| ES4 | The Quiet Reading Room | E1 | ◆◆ | — | Reading Room Warden 5（加防御）、Reader 4×2 | 4 | — |
| ES5 | A Name in the Margin | ES4 | ◆◆◆ | WOUNDED 8 | Warden 6（双倍防御）、Reader 5（变）、Reader 4 | 4 | — |
| ES6 | The Last Reader | ES5 | ◆◆◆ | OUTNUMBERED | Last Reader 7（变）、Warden 5×2、Clerk 4 | 3 | — |

### 6.6 支线章节「The Roofless Shelter」：6 节点

挂在地图最外层、与各章并列的一个**支线章节**（章节索引里 `path: ["The Roofless Shelter"]`、
`kind: "side"`、`palette: "slate"`）。两条弧的首节点父节点都是 **D2 与 E2（需完成 2 个）**，
即两条第三章路线都走完才出现。它是决战前最后一处"等人"的地方。
颜色与其它支线不同（石板灰 + 淡紫），因为这两条弧是终局挑战，不是见闻。

弧① **The Weight**（Queue → One More Turn → Weight，→ 僧 Seng）

| ID | 节点名 | 父节点 | 档 | 誓约 | 敌人 | 预算 | 奖励 |
|---|---|---|---|---|---|---|---:|
| WS1 | The Queue at the Shelter | D2, E2（2） | ◆◆◆ | OUTNUMBERED | Waiting Traveller 6（血）、Porter 4×3 | 4 | — |
| WS2 | One More Turn | WS1 | ◆◆◆◆ | BARE HANDS, ALONE | Waiting Traveller 6（血）、Porter 4×2 | 0 | — |
| WS3 | The Weight | WS2 | ◆◆◆◆ | ALONE, WOUNDED 8 | Ferry Master 10（僧、双倍防御）、Hand 5（血）、Hand 5；阵亡 2 人后 **Hand 4（血）** 加入 | 3 | **Seng** |

弧② **The Bench Repaired**（→ 道 Dao）

| ID | 节点名 | 父节点 | 档 | 誓约 | 敌人 | 预算 | 奖励 |
|---|---|---|---|---|---|---|---:|
| WS4 | The Bench Repaired | D2, E2（2） | ◆◆◆ | OUTNUMBERED | Carpenter 6（加防御）、Helper 4、Helper 4、Bench Warden 3 | 4 | — |
| WS5 | Two Coats | WS4 | ◆◆◆◆ | OUTNUMBERED, WOUNDED 8 | Carpenter 8（道）、Helper 5（双倍攻击）、Helper 5、Helper 4 | 3 | — |
| WS6 | Fare for One | WS5 | ◆◆◆◆ | BARE HANDS, ALONE | Ferryman 9（加防御）、Oar 5（道）、Oar 4 | 0 | **Dao** |

> 长凳修好了一端。有人问："你等的人会来吗？"——"我不知道。但我可以再等一会儿。"

### 6.7 结构示意

```text
主线（paper）：
P → T01 → T02 → T03 ─┬─ A1 ─┐
                     ├─ A2 ─┼→ AG ─┬─ B1..B3 → BG → D1 → D2 ─┐
                     └─ A3 ─┘      └─ C1..C3 → CG → E1 → E2 ─┴→ F

支线（frost / slate，挂在主线节点下，主线不会以支线为父）：
T03 → AS1 → AS2 → AS3        T03 → AS4 → AS5              （第一章）
AG  → BS1 → BS2 → BS3        BG  → BS4 → BS5 → BS6          （钟楼）
AG  → CS1 → CS2 → CS3        CG  → CS4 → CS5 → CS6          （庭院）
D2  → DS1 → DS2 → DS3        D1  → DS4 → DS5 → DS6          （龙墓）
E2  → ES1 → ES2 → ES3        E1  → ES4 → ES5 → ES6          （书库）
D2  → D3 → D4                E2  → E3 → E4                  （Tomb Afterimages / Archive Overwritten）
D2+E2 → WS1 → WS2 → WS3      D2+E2 → WS4 → WS5 → WS6        （The Roofless Shelter，slate）
```

---

## 7. 同伴招募

### 7.1 规则

- 同伴新增字段 `unlockNode`：完成该节点后，这名同伴才会出现在名册上。
- **未结识的同伴不显示卡片**（延续「Alone 不需要占位卡」的决定），
  名册界面只展示已结识的人；总人数体现在标题行 `ROSTER 3`。
- 招募是**派生**的：由 `CampaignProgress.CompletedNodes` 推导，不写新存档字段，旧存档无需迁移。
- 招募与技能互不占用：一个节点可以只给同伴，也可以只给技能。

### 7.2 名册

| id | 名字 | 来源 | 生命 | 技能 | 性格 / 作用 |
|---|---|---|---:|---|---|
| `mara_venn` | Mara Venn | P | 10 | — | 队伍最低血量越低，AI 强度越高（40 → 95） |
| `ila` | Ila | AS3 | 7 | 加攻击、双倍攻击 | 她上一轮造成过伤害，玩家本回合的攻击翻倍 |
| `bram` | Bram | DS6 | 9 | 双倍防御 | 两个数字都是 1 时，每回合免第一次受击 |
| `sable` | Sable | ES3 | 5 | — | 带 3 血墨影；倒下后移除，她花 2 回合重写，可无限重召 |

三名新同伴先用 3×2 名册图集（`atlasCell` 2/3/4）作立绘占位，
之后只要给 `artKey` 或 `artPath` 就能换成正式立绘，不需要改代码。

---

## 8. 数据与代码改动

### 8.1 节点新字段

| 字段 | 类型 | 默认 | 用途 |
|---|---|---|---|
| `tier` | 1–4 | 1 | 难度菱形、排序、契约校验 |
| `vows` | 字符串数组 | `[]` | 誓约显示 + 机械强制校验 |
| `playerHealth` | 整数 | 11 | 带伤等限制；写入 `CampaignBattleFactory` 的玩家初始生命 |

节点的 `kind` **不单独声明**：它从章节索引继承（章节是 `side`，它里面的节点就是支线）。
这样一条主线节点混进支线章节、或一章里颜色不一致的情况都不会出现。

同伴字段：`unlockNode`（字符串，空 = 一开始就在名册里；Mara 设为 `P`）。
章节索引：`Content/campaign/chapter-index.json`，字段见 §5.5。

一个完整的支线节点（可直接作为写的模板）：

```json
{
  "id": "BS3",
  "tier": 3,
  "chapter": "The Hunters' Debt",
  "name": "Final Payment",
  "story": "The hunters owe the tower a life. The collectors have come to take it back.",
  "vows": ["alone"],
  "skillBudget": 3,
  "mapX": 210,
  "mapY": -120,
  "parents": ["BS2"],
  "requiredCompletedParents": 1,
  "rewards": ["Xue"],
  "enemies": [
    { "name": "Debt Master", "health": 8, "skills": ["Xue", "PlusDefence"] },
    { "name": "Collector", "health": 4, "skills": ["Xue"] },
    { "name": "Collector", "health": 4 }
  ],
  "reinforcements": [
    { "name": "Ledger Shade", "health": 4 }
  ],
  "reinforcementAfterEnemyDeaths": 1,
  "beforeStory": [
    { "speaker": "Collector", "text": "Sign it our way. None of the crown's tricks." },
    { "speaker": "The Long Road", "text": "Mara waits outside the ledger room. The door is not wide enough for a witness." }
  ],
  "afterStory": [
    { "speaker": "The Long Road", "text": "The ledger closes. Arith keeps his hand steady long enough to write his own name." }
  ]
}
```

```json
{ "id": "ila", "name": "Ila", "portraitKey": "tutorial_ally", "atlasCell": 2,
  "unlockNode": "AS3", "health": 7, "agentType": "Rule", "skills": ["PlusAttack"],
  "description": "The smith's apprentice. Carries the sword until someone takes it." }
```

### 8.2 代码触点

| 文件 | 改动 |
|---|---|
| `Campaign/CampaignModels.cs` | `CampaignVow` 枚举；`Tier`、`Vows`、`PlayerHealth`；`RewardText` 合并技能与招募文案 |
| `Campaign/CampaignChapters.cs`（新） | 章节索引：加载 `chapter-index.json`、`ForNode(node)`、`Kind`/`Palette`/`Summary`、未声明章节回退 `main`+`paper` |
| `Campaign/CampaignDifficulty.cs`（新） | 档位带宽、誓约元数据、`Audit(node)` 返回违规项 |
| `Campaign/CampaignGraphValidator.cs` | 调用 `Audit`；主线节点不得以支线为父节点；`palette` 键已注册；`unlockNode` 必须存在 |
| `Campaign/CampaignBattleFactory.cs` | 玩家初始生命改为 `node.PlayerHealth`（默认 11） |
| `Campaign/CampaignCompanions.cs` | `UnlockNode`；`Available(node, progress)` 重载（`null` 进度 = 全部，供测试/预览） |
| `Campaign/CampaignCatalog.cs` | `MainNodes` / `SideNodes` 便捷视图（按章节索引的 kind 划分） |
| `UI/Campaign/CampaignChapterPalette.cs`（新） | palette 键 → 纸色/墨/弱墨/强调色四个 `Color`；三行表，无新贴图 |
| `UI/Campaign/CampaignMapHierarchy.cs` | 按索引的 `order` 排序；章节层级仍来自 `path` |
| `UI/Campaign/UICampaignRouteView.cs` | 四个现有绘制方法各加一个**可选** `ChapterStyle`（默认 = 现状）；章节卡上色与 S 编号、节点卡标签取强调色、面包屑上色、难度菱形与誓约行 |
| `UI/Campaign/UICampaignMap*.cs` | 章节计数：主线章只统计主线节点，支线章统计自己；章节详情用索引 `summary` |
| `UI/Campaign/UICampaignCompanionSelect*.cs` | 名册按招募过滤；标题行 `ROSTER n` |
| `UI/Campaign/UICampaignPartyStrip.cs` | 誓约说明行（`ALONE` 时说明本场无人可带） |
| `UI/Campaign/UICampaignScreenBuilder.cs` | 誓约横幅、结算面板显示招募 |
| `Content/campaign/chapter-index.json`（新） | 24 条章节条目（主线 11 / 支线 13） |
| `Content/campaign/companions.json` | 3 名新同伴 |

### 8.3 测试

- 固定值已改为 **64** 个节点、**24** 条章节条目；测试会同时校验唯一来源与难度契约。
- 新增：技能来源唯一且覆盖全部 20 个（随 P6 奖励重排落地）；主线节点不以支线为父节点；
  难度契约对全目录通过；同伴招募按进度过滤；`playerHealth` 写入战斗。
- 章节：每条被节点使用的 `path` 都能在索引里命中；`kind`/`palette` 合法；
  未声明章节的节点回退为 `main`+`paper`；主线章节的计数不含支线节点。
- 预览工具：`UiPreview --chapters-only` 输出 `chapters`（章节列表）、`chapters-side`
  （支线章节内部）、`chapters-side-detail`（难度徽章 + 誓约 + 招募文案）三张图，
  并校验支线纸色与强调色编号。

### 8.4 旧存档

技能按「解锁时写入」保存，删掉某个节点的奖励不会回收老存档里已经拿到的技能；
同伴按完成节点派生，老存档若已完成对应节点会直接获得新同伴。
版本说明里注明：想按新规则重新体验，需要重置战役进度。

---

## 9. 实施阶段

| 阶段 | 内容 | 产出 | 状态 |
|---|---|---|---|
| P0 | 章节索引 + 新字段、难度契约、校验、测试骨架 | 数据契约可运行 | **已完成** |
| P1 | 章节配色与 S 编号、难度菱形/誓约、章节计数、面包屑、招募过滤 | UI 能表达难度与支线章节 | **已完成** |
| P2 | 第一章 5 节点（2 个支线章节）+ Ila | 第一批可玩支线 | **已完成** |
| P3 | 钟楼 + 庭院 12 节点、4 个支线章节（Xue/Tong/Shen/Xian） | 第二章支线 | **已完成** |
| P4 | 龙墓 + 书库 12 节点、4 个支线章节（Shi、Bram、Sable）+ D3/D4/E3/E4 迁章 | 第三章支线 | **已完成** |
| P5 | The Roofless Shelter 6 节点（Seng、Dao） | 终局挑战 | **已完成** |
| P6 | 奖励重排 + 平衡试玩、难度契约校准、文档 | 可发布 | **已完成**（20 个技能 20 个唯一来源） |

奖励重排（把 Xue/Tong/Shen/Xian/Shi 从主线节点移到支线试炼、Mo/Di 改由 BG/CG 给）
必须与「技能唯一来源」测试同时落地，所以放在 P6 与平衡一起做；在那之前主线仍然
按旧规则发奖励，只是新增支线不再发技能。

---

## 10. 风险

1. **对白质量。** 35 个节点要维持《长路》的语气（具体的动作、没说完的请求、不解释世界）。
   建议每批先写 2 个节点试读，再批量生产。
2. **难度契约可能过严。** 带宽按 5 个节点实测校准一次；必要时放宽的是带宽，不是誓约。
3. **地图信息过载。** 章节颜色本身就是一层分组；如果列表仍然拥挤，
   主线容器章节（Chapter II / III）可以再加"只看主线 / 全部"的切换，而不是继续加层级。
4. **颜色太花。** 支线统一用一种 frost、终局一种 slate，先别给每章单独配色；
   等 2–3 个真实章节落地、截图确认后再决定要不要扩色板。
5. **技能稀疏导致后期强度不足。** 兜底方案：把双倍生命或石下放到第一章支线，
   让玩家在第二章之前多一件东西。
6. **空手节点不好玩。** 如果实测发现数字基本功不足以支撑 3 个敌人，
   优先降低敌人血量，而不是取消誓约——誓约是这类支线的全部意义。

---

## 11. 已确认的决定

| # | 事项 | 决定 |
|---:|---|---|
| 1 | 规模 | **按 64 节点 / 13 个支线章节执行**（比保守方案多）；后续加章节只加数据 |
| 2 | 主线技能保底 | **10 个**（支线 10 个；每个技能唯一来源） |
| 3 | 第一章支线 | **不给技能**：只给同伴 Ila 和见闻，让"支线给技能"从第二章开始 |
| 4 | 技能费用 | **保持现状**（加攻击、加防御仍是 2 费），不做 1 费填充 |
| 5 | 章节配色 | **只做冷色**：主线 `paper`、支线 `frost`、决战前 `slate`；不逐章分色 |
| 6 | 迁章 | **执行**：D3、D4 → Tomb Afterimages；E3、E4 → Archive Overwritten |

仍未定的只有文案层面的事：每章的 `summary` 与 35 个节点的对白要逐条写，
按批次（第一章 → 钟楼 → 庭院 → 龙墓 → 书库 → 无顶棚）试读后再定稿。
