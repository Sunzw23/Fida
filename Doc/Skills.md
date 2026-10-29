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

### 已实现技能（14 个）

| 图标 | 技能名 | 枚举值 | 触发时机 | 消耗值 | 使用限制 | 效果说明 |
|------|--------|--------|----------|--------|----------|----------|
| ⚔️ | 加攻击 | PlusAttack | OrderingAttack | 1 | Unlimited | 攻击伤害 **+1**（需确认） |
| ⚔️ | 双倍攻击 | DoubleAttack | OrderingAttack | 1 | Unlimited | 攻击伤害 **×2**（需确认） |
| 🛡️ | 加防御 | PlusDefence | Defending | 1 | Unlimited | 受到伤害 **-1**（最低 1） |
| 🛡️ | 双倍防御 | DoubleDefence | Defending | 1 | Unlimited | 受到伤害 **减半**（最低 1） |
| ❤️ | 双倍生命 | DoubleHealth | GameStart | 1 | Limited (1次) | 最大生命值 **×2**，回满血 |
| ❤️ | 三倍生命 | TripleHealth | GameStart | 1 | Limited (1次) | 最大生命值 **×3**，回满血 |
| 🔢 | 神 | Shen | Damaged | 2 | Unlimited | 受到伤害后，可将自己的一个数字**改为任意值(0-9)**（两步操作：先选位置，再选数值） |
| 🔢 | 仙 | Xian | OrderedAttack | 1 | Unlimited | 准备攻击时，可将自己的一个数字**改为任意值(0-9)**（两步操作） |
| 👹 | 魔 | Mo | Damaged | 1 | Unlimited | 受到伤害后，对任意目标造成 **ceil(伤害/2)** 点**真实伤害**（无视护盾和防御技能） |
| 🩸 | 血 | Xue | Damaging | 1 | Unlimited | 造成伤害后，**回复等量生命值**（自动触发，无需选择） |
| 🔄 | 同 | Tong | TurnStart | 2 | Unlimited | 回合开始时，可**重新执行选数字步骤**（允许同时选择自己的两个数字） |
| 👑 | 帝 | Di | TurnStart | 2 | Unlimited | 回合开始时，可将自己的一个数字**改为任意值(0-9)**（两步操作） |
| 🎯 | 变 | Bian | OrderingAttack | 3 | Unlimited | 准备攻击时，可将伤害值设为 **1-5 之间的任意值** |
| 🐉 | 龙 | Long | OrderingAttack | 3 | Unlimited | 准备攻击时，可将伤害值设为**自己两个数字之和** |

### 未实现技能（3 个）

以下技能在枚举和文本中有定义，但尚未实现具体的技能类和技能状态：

| 图标 | 技能名 | 枚举值 | 计划触发时机 | 计划效果 |
|------|--------|--------|-------------|----------|
| 👻 | 鬼 | Gui | — | 在 `skillNames` 中有中文名映射，无详细描述 |
| 🔥 | 火 | Huo | — | 攻击可同时攻击所有目标 |
| 🪨 | 石 | Shi | — | 每回合第一次受到攻击时不受伤 |

## 技能消耗值 (Value)

每个技能有一个消耗值（`Value`），目前在代码中定义但尚未在游戏流程中强制消耗。计划用途可能是技能点数系统：

| 消耗值 | 技能 |
|--------|------|
| 1 | PlusAttack, DoubleAttack, PlusDefence, DoubleDefence, DoubleHealth, TripleHealth, Xian, Mo, Xue |
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
