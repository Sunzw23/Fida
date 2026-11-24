# 同伴能力设计（v6 · 已实现）

> v6 是落地版本：v5 的四个待定项全部由设计方拍板并实现。
> 代码在 `Campaign/Battle/`、`Logic/Player.cs`、`Logic/States/PreDamageState.cs`；
> 数值在 `Content/campaign/companions.json`；测试在 `Fida.Tests/CompanionAbilityTests.cs`。

---

## 0. 最终形态

| 同伴 | 血量 | 技能 | 能力 | 实现 |
|---|---:|---|---|---|
| **Mara Venn** | 10 | 无 | 队伍最低血量越低，她的决策强度越高（40 → 95） | `AdaptiveCompanionController`（每名玩家独立 `AgentProfile`） |
| **Ila** | 7 | 加攻击、双倍攻击 | 她上一轮造成过伤害，玩家本回合的第一次攻击翻倍 | `CampaignBattleRules` 记录 + `Player.DoubleStrikeArmed` |
| **Bram** | 9 | 双倍防御 | 两个数字都是 1 时，每回合免第一次受击 | `CampaignBattleRules` 开关 + `Player.WardsFirstHit` + `PreDamageState` |
| **Sable** | 5 | 无 | 带一个 3 血墨影；墨影倒下后从棋盘移除，她花 2 个回合重写，可无限重复 | `CampaignBattleFactory` + `CampaignBattleRules` + `ControllerFactory` |

---

## 1. Mara —「The Long Road」

- **无主动技能**，10 血，是同伴里最耐打的。
- `r = min(队友当前生命 / 最大生命)`（含玩家、含墨影），
  `strength = clamp(40 + 55 * (1 - r), 40, 95)`。
- 对照：普通敌人默认 65；队伍满血时她只有 40（会走错步），有人濒死时 90+。
- 实现：`AdaptiveCompanionController.StrengthFor`；每次请求行动前重算，强度档位变化才重建
  `LightweightAgent`，不触碰全局 `AgentDefaults`。

---

## 2. Ila —「Tempered Edge」

- **判定**：Ila 在上一轮（或本轮更早时）造成过伤害 → 玩家回合开始时 `DoubleStrikeArmed` 置位。
- **效果**：玩家本回合的**第一次**攻击伤害 ×2（一次结算，防御只减一次）。
- **代价**：她只有 7 血、没有防御技能；技能表是加攻击 + 双倍攻击，负责把条件打出来。
- 实现：`CardActionStarted` 记录她的伤害轮次；`TurnStarted` 武装/清除；
  `PreDamageState` 扣血前消费标记并翻倍。

---

## 3. Bram —「The Gate」

- **判定**：两个数字都是 1（“关门”）；每次行动后与回合开始重算。
- **效果**：每回合抵挡**第一次**受击（不扣血），之后的攻击正常结算。
- 与「石」相反（石是第一下照常挨、之后全免），所以他**不带** `Shi`。
- **代价**：如果他自己没回到 1/1（例如停在 4/1），这一轮没有免伤；双倍防御是唯一技能。
- 实现：`Player.WardsFirstHit`；`PreDamageState` 在扣血前判定，
  免伤的那一下也计入 `AttacksReceivedThisTurn`，所以每回合只免一次；
  该计数已随联机快照同步。

---

## 4. Sable —「Margin」（召唤师）

### 4.1 循环

```
开场            → 墨影已在场（3 血、无技能、1/1）
墨影阵亡        → 立即开始"书写"（2 个她的回合），当轮结束时从棋盘移除
书写第 1 回合   → 她的控制器提交 SkipTurn
书写第 2 回合   → 同上
下一个她的回合  → 墨影重新上场，可再次战斗
```

- **无限次**：只要 Sable 活着，循环可以一直重复。
- **同时只有一个**墨影；棋盘上限是玩家 + 2 同伴 + 墨影 = 4 个己方单位。
- **代价**：每次重召要损失她 2 个整回合的行动；5 血的她很怕被直接压死。

### 4.2 实现

- `CampaignBattleFactory`：邀请 Sable 时多放一个墨影 `Player`，在 `AgentTypes` 里登记为 AI，
  并在 `CampaignBattleSetup.Summons` 记录“召唤者 → 墨影”的映射。
- `CampaignBattleRules`：监听 `TurnStarted` / `HealthChanged` / `CardActionStarted`；
  在轮次边界移除尸体（`GameManager.RemovePlayer`），在轮次内累计书写回合；
  触发 `Summoned` / `Removed` 事件。
- `SummonerCompanionController`：轮到 Sable 且正在书写时提交 `SkipTurn`（与人类跳过同一路径），
  否则交给 `LightweightAgentController`。
- `PlayingSceneManager`：监听两个事件，重建 UI、补/删控制器、播放入场。

---

## 5. 组合

| 组合 | 解决什么 | 代价 |
|---|---|---|
| Mara + Bram | 10 血 + 9 血 + 每回合免一下，最稳 | 输出极低 |
| Ila + Bram | 一攻一守，还能触发翻倍 | 没有改数 |
| Sable + Ila | 墨影分担火力，Ila 制造翻倍 | 5 + 7 + 3 血，容易被逐个清 |
| Sable + Mara | 无限重召 + 危险时 Mara 变聪明 | 前期输出低 |

**强度预算**：同伴不提供额外 SP；`alone` 禁同伴；`bare_hands` 放行；
墨影算第 4 个己方单位，不额外提供技能。

---

## 6. 已实现清单

| 文件 | 内容 |
|---|---|
| `Content/campaign/companions.json` | 最终四人数值、`passive`、`summons` |
| `Campaign/CampaignModels.cs` | `CampaignAgentType.Adaptive`、`CompanionIds`、`Summons` |
| `Campaign/CampaignCompanions.cs` | `Passive`、`CampaignSummon` |
| `Campaign/CampaignBattleFactory.cs` | 墨影席位、召唤映射、同伴 id 映射 |
| `Campaign/Battle/AdaptiveCompanionController.cs` | Mara 的强度曲线 |
| `Campaign/Battle/SummonerCompanionController.cs` | Sable 的书写跳过 |
| `Campaign/Battle/CampaignBattleRules.cs` | Bram 免伤开关、Ila 记录、Sable 召唤/移除/书写 |
| `Campaign/Battle/CampaignControllerFactory.cs` | `Adaptive` 与召唤师分支 |
| `Logic/Player.cs` | `WardsFirstHit`、`DoubleStrikeArmed` |
| `Logic/States/PreDamageState.cs` | 免伤分支、翻倍分支 |
| `Logic/GameManager.cs` | `RemovePlayer`（轮次边界移除尸体） |
| `Scene/PlayingSceneManager.cs` | 召唤/移除事件 → 控制器与 UI 重建 |
| `Fida.Tests/CompanionAbilityTests.cs` | 免伤只一次、翻倍只一次、强度曲线、墨影循环、名册数值 |

**验证**：`Fida.Tests` 290 passed / 0 failed；`UiPreview` 全量 sweep 通过。

---

## 7. 后续可调项

1. 墨影 3 血是否要降到 2（现在偏稳）。
2. Bram 9 血是否降到 8（每回合免一下其实不弱）。
3. 联机：召唤/移除目前按轮次规则在两端各自执行；如果出现分歧，再把召唤改为主机权威。
