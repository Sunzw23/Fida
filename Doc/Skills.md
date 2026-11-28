# 技能系统

## 技能触发时机 (SkillTrigger)

技能在游戏流程中的特定时机触发。共有 7 种触发时机：

| 触发时机 | 枚举值 | 触发条件 |
|----------|--------|----------|
| 游戏开始 | `GameStart` | 游戏开始时触发一次 |
| 回合开始 | `TurnStart` | 技能拥有者的回合开始时触发 |
| 攻击排序 | `OrderingAttack` | 准备发动攻击时触发（攻击增益类） |
| 攻击排序（优先） | `OrderedAttack` | 与 OrderingAttack 同时推入栈，但在其之前结算 |
| 防御中 | `Defending` | 即将受到伤害时触发（防御减伤类） |
| 造成伤害后 | `Damaging` | 对目标造成伤害后触发（攻击方反应） |
| 受到伤害后 | `Damaged` | 受到伤害后触发（受伤方反应） |

## 技能使用限制 (SkillUsageLimit)

| 限制类型 | 说明 |
|----------|------|
| `Unlimited` | 无限制，每次触发时机都可以使用 |
| `PerRound` | 每轮可使用一次（暂未完全实现） |
| `PerTurn` | 每个玩家回合重置一次 |
| `Limited` | 有总使用次数上限（如 DoubleHealth/TripleHealth 只能使用 1 次） |

## 技能一览

### 已实现技能（24 个）

> 数字改写类技能（神/仙/帝/超神/超仙/超帝）**只能改成 1-9**：0 是护盾，只能靠加法凑出来（两数之和 ≡ 0 mod 10），不能直接点出来。

> 下表原有 14 行未跟上代码：僧、道、石、超神、超仙、超帝 均已在 `SkillCatalog` 中可选，待补。

| 图标 | 技能名 | 枚举值 | 触发时机 | 消耗值 | 使用限制 | 效果说明 |
|------|--------|--------|----------|--------|----------|----------|
| ⚔️ | 加攻击 | PlusAttack | OrderingAttack | 1 | Unlimited | 攻击伤害 **+1**（需确认） |
| ⚔️ | 双倍攻击 | DoubleAttack | OrderingAttack | 1 | Unlimited | 攻击伤害 **×2**（需确认） |
| 🛡️ | 加防御 | PlusDefence | Defending | 1 | Unlimited | 受到伤害 **-1**（最低 1） |
| 🛡️ | 双倍防御 | DoubleDefence | Defending | 1 | Unlimited | 受到伤害 **减半**（最低 1） |
| ❤️ | 双倍生命 | DoubleHealth | GameStart | 1 | Limited (1次) | 最大生命值 **×2**，回满血 |
| ❤️ | 三倍生命 | TripleHealth | GameStart | 1 | Limited (1次) | 最大生命值 **×3**，回满血 |
| 🔢 | 神 | Shen | Damaged | 2 | Unlimited | 受到伤害后，可将自己的一个数字**改为 1-9**（两步操作：先选位置，再选数值） |
| 🔢 | 仙 | Xian | OrderedAttack | 1 | Unlimited | 准备攻击时，可将自己的一个数字**改为 1-9**（两步操作） |
| 👹 | 魔 | Mo | Damaged | 1 | Unlimited | 受到伤害后，对任意目标造成 **ceil(伤害/2)** 点**真实伤害**（无视护盾和防御技能） |
| 🗡️ | 刺 | Ci | Damaged | 1 | Unlimited | 受到伤害后，对**攻击者**造成 **1 点真实伤害**（自动触发，无需选目标；战役 SP 2，出自 B5 Mirror Siege） |
| 🩸 | 血 | Xue | Damaging | 1 | Unlimited | 造成伤害后，**回复等量生命值**（自动触发，无需选择） |
| 🔄 | 同 | Tong | TurnStart | 2 | Unlimited | 回合开始时，可**重新执行选数字步骤**（允许同时选择自己的两个数字） |
| 👑 | 帝 | Di | TurnStart | 2 | Unlimited | 回合开始时，可将自己的一个数字**改为 1-9**（两步操作） |
| 🎯 | 变 | Bian | OrderingAttack | 3 | Unlimited | 准备攻击时，可将伤害值设为 **1-5 之间的任意值** |
| 🐉 | 龙 | Long | OrderingAttack | 3 | Unlimited | 准备攻击时，可将伤害值设为**自己两个数字之和** |
| 🗡️ | 破 | Po | OrderingAttack | 1 | Unlimited | 准备攻击时，本次攻击**无视护盾与防御技能**（护盾不消耗；战役 SP 1，出自 AS2 The Test Cut） |
| ✴️ | 真 | Zhen | 被动（全局） | 1 | Unlimited | 你的**所有攻击**都按**真实伤害**结算：跳过护盾、防御技能、免伤、以及双方所有受伤后反应（包括自己的「血」吸血；战役 SP 3，出自 E1 The Blank Archive） |
| 🧩 | 裂 | Shatter | Defending（盾被打破时） | 1 | Unlimited | 你的护盾被打破时，把这个数字改成 **1-9** 的任意值，然后照常检定效果表（敌人打你 = 帮你起手；战役 SP 2，出自 DS1 The Stone Tally） |

### 未实现技能（2 个）

以下技能在枚举和文本中有定义，但尚未实现具体的技能类和技能状态：

| 图标 | 技能名 | 枚举值 | 计划触发时机 | 计划效果 |
|------|--------|--------|-------------|----------|
| 👻 | 鬼 | Gui | — | 在 `skillNames` 中有中文名映射，无详细描述 |
| 🔥 | 火 | Huo | — | 攻击可同时攻击所有目标 |

> 石（Shi）已实现（`Logic/Skills/StoneSkill.cs`，Defending，战役 SP 2），不再属于未实现项。

### 反向技能（已登记，未接入流程 · 3 个）

它们是「缺陷」：**不消耗 SP，而是返还 SP**。目前只登记了名字、费用、图标与文案（`SkillCatalog` 里 `Factory = null`），因此**不在 `SelectableSkills` 内**，不会出现在配装、图鉴、战役奖励里，也没有任何效果被接入伤害管线。

| 图标 | 技能名 | 英文名 | 枚举值 | 费用 | 计划效果 |
|---|---|---|---|---:|---|
| 🪓 | 攻击减半 | Half Attack | `HalfAttack` | **-3** | 你的攻击伤害减半，向下取整（1 点攻击打空） |
| 💔 | 生命减半 | Half Health | `HalfHealth` | **-2** | 你的生命上限减半（11 → 5） |
| 🩸 | 失血 | Bleed | `Bleed` | **-2** | 每回合开始你失去 1 点生命 |

**接入时需要的规则（尚未实现）：**

- **净预算**：`可用预算 = 节点 budget + 返点`；界面上返点必须与「已花费」分开显示；
- **上限**：缺陷槽 ≤ 2 个、总返点 ≤ 3（否则一堆 -2 能买穿牌池）；
- **结算顺序**：缺陷**最后结算**（先乘除后缺陷，避免出现"先除后乘"的抵免）；
- **互斥**：生命减半 ↔ 双倍生命 / 三倍生命（否则 1 SP 换 11 血，是纯亏陷阱）；
- **定价规则**：缺陷的返点 = 它等效于「免费发给每个敌人」的那张技能的价格
  （攻击减半 ≈ 敌人人手双倍防御 = 3，生命减半 ≈ 双倍生命的反向 = 2，失血 ≈ 加防御的反向 = 2）。
- **待定**：攻击减半究竟是 -3（账面）还是 -1（只在挑战关卡出现），因为 -3 会催生"不打只磨"的乌龟流，和设计目标相反。


## 技能消耗值 (Value)

每个技能有一个消耗值（`Value`），目前在代码中定义但尚未在游戏流程中强制消耗。计划用途可能是技能点数系统：

| 消耗值 | 技能 |
|--------|------|
| 1 | PlusAttack, DoubleAttack, PlusDefence, DoubleDefence, DoubleHealth, TripleHealth, Xian, Mo, Ci, Po, Zhen, Shatter, Xue |
| 2 | Shen, Tong, Di |
| 3 | Bian, Long |

## 技能结算流程

以一次典型攻击为例，技能结算顺序如下：

```
攻击方: OrderingAttack 技能 (PlusAttack → DoubleAttack → Long → Bian)
    ↓
攻击方: OrderedAttack 技能 (Xian)
    ↓
选择目标
    ↓
[护盾检查] → 有护盾则护盾破碎，伤害抵消，跳过后续
    ↓
防御方: Defending 技能 (PlusDefence → DoubleDefence)
    ↓
伤害结算 (TakeDamage)
    ↓
攻击方: Damaging 技能 (Xue — 吸血)
    ↓
防御方: Damaged 技能 (Mo — 反伤 → Shen — 改数字)
```

### 真实伤害

"魔"(Mo) 技能造成的真实伤害走 **TrueDamageState**，会：
- 跳过护盾检查
- 跳过防御技能（PlusDefence, DoubleDefence）
- 直接扣除目标生命值

---

> 相关：[CombinationTable.md](./CombinationTable.md)（草案）——可收集/替换的数字组合表，与技能无关但会改变"数字一变，检定什么"。
