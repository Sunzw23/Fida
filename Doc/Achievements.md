# 成就系统（已实现）

状态：**已实现**。成就页从主菜单进入；其中「Every Combination」就是数字组合表的彩蛋钥匙。

## 1. 设计原则：派生，不存档

除了「连击」和「用遍效果表」两条，其余成就全部由现有进度算出来——不新增存档字段，旧档不需要迁移。

| 成就 | 条件 | 数据来源 |
|---|---|---|
| First Blood | 通关任意一关 | `CampaignProgress.CompletedNodes` |
| Empty-Handed | 通关一场 `bare_hands` 的关卡 | 同上 + 该节点的 `Vows` |
| No One With You | 通关一场 `alone` 的关卡 | 同上 |
| Open Wound | 通关一场 `wounded` 的关卡 | 同上 |
| Field Reader | 技能图鉴里见过 10 个技能 | `SkillCodex.Encountered` |
| **Chain** | **一次数字变化触发两个效果** | `GameManager.ChainDepth` → `CombinationLedger.BestChain` |
| **Every Combination** | **效果表 8 条各命中一次** | **`CombinationLedger`** |

### 连击是怎么判定的

一次数字变化可以连出两个效果：`[8,6]` 先由 8 触发攻击 1，8 归 1 后变成 `[1,6]`，再触发配对攻击 2；`[9,6]` 同理（治疗 → `1&6`）。

`GameManager.ChainDepth` 在每次效果结算时 +1；`ApplyingEffectState` 发现"没有更多效果"时就是链的终点，此时把深度交给 `CombinationLedger.RecordChain()`（只记最大值），然后把计数归零。**深度 ≥ 2 即达成连击。**

## 2. 效果表的记录点

8 条里有 7 条在 `ApplyingEffectState` 命中时记；`0`（护盾）是**被动**的、不经过那里，所以在 `Player.SetNumber` 里"数字变成 0"时记。

记录的存储：`%LOCALAPPDATA%/Fida/achievements.json`（失败静默，读不到就是空进度）。写入时机：

- 打开成就页时；
- 每场战斗结束进入结算画面时（`State.GameOver`）；
- 离开成就页时。

> 注意：`EnsureLoaded()` 是**合并**而不是覆盖——一局战斗可能在界面第一次加载之前就已经记录了行，加载不能把它们冲掉（这是实现时踩到的一个真 bug，已修）。

## 3. 界面

- 入口：主菜单 `Achievements`，排在 `Rules` 之后；
- **版式与 Rules 完全同一套语言**：`UIPaper.Background` + 动画背板、顶部 header 纸片（返回键 + 标题）、内容纸片、右下翻页箭头与 `N / M`；
- **分页**才是可扩展的关键：桌面 4 行 / 页、竖屏 5 行 / 页，新增成就永远不会撑破版式；
- 每行：左侧墨条（实心＝已达成）＋名称＋描述，右侧状态/计数（`earned` / `not yet` / `best 2` / `0 / 8 rows`）；
- 只读，只有返回与翻页（`←` `→` 键也可翻页）。

### 踩过的两个坑（供后来者）

1. **`UITheme.Ink` 是奶油色**（237,232,219），专给深色面板用；画在纸面上会"隐形"。纸面上的文字必须用 `UIPaper.Ink` / `UIPaper.MutedInk`。第一版渲染出来整页像被冲淡了，就是这个原因。
2. **字阶不能乘 `layout.Scale` 直接用**：字体本身已按设计尺寸排好，正确的是 `设计行高 × scale ÷ font.LineSpacing`（`UIRulesText.ScaleFor` 就是这个公式）。乘两次会把"描述"画得比"标题"还大。

代码位置：`UI/UIAchievementsPage.cs`（绘制，版式取 `UIRulesLayout`）、`UI/UIScreenBuilder.Achievements.cs`（装配 + 翻页按钮）、`States/AchievementsState.cs`（输入 + 翻页）、`Campaign/Achievements.cs`（规则与存储）。

## 4. 与彩蛋的关系

`Achievements.EasterEggUnlocked(progress)` 就是"全表命中"的判定。数字组合表的装配页（MARGINS）将来由它来开门——见 [CombinationTable.md](./CombinationTable.md)。

## 5. 尚未做

1. 彩蛋章节与 MARGINS 装配页（等卡池定稿）；
2. 成就的"新达成"提示（现在只有页面上的状态，没有弹出提示）；
3. 线上车道：成就目前只读本地战役进度（`CampaignRuntime.Progress`），线上进度未合并。
