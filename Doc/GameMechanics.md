# 游戏深层机制

本文档描述 Fida 的底层游戏机制，面向开发者阅读。

## 状态机架构

### 应用层状态机

`State.cs` 定义了应用层面的 13 个屏幕状态：

```
MainMenu ─→ LevelSelect ─→ Playing ─→ Paused ─→ GameOver
MainMenu ─→ OnlineMenu ─→ Hosting ─→ OnlineLobby ─→ OnlinePlaying
MainMenu ─→ OnlineMenu ─→ Connecting ─→ OnlineLobby ─→ OnlinePlaying
MainMenu ─→ Rules / Skills / Settings
```

状态转换由 `GameStateMachine.cs` 管理。

### 游戏内状态栈

`Logic/GameState.cs` 定义了回合内的游戏逻辑状态。这些状态使用**栈结构**管理，而非简单的线性转换。核心状态包括：

| 状态类 | 职责 |
|--------|------|
| `GameStartState` | 游戏初始化，处理 GameStart 触发技能 |
| `TurnStartState` | 回合开始，重置每回合技能，启动数字选择 |
| `ChooseNumberIndexState` | 两步数字选择流程 |
| `ApplyingEffectState` | 检查数字组合并触发效果 |
| `OrderingAttackState` | 处理攻击方增益技能 |
| `TargetSelectionState` | 选择攻击/治疗目标 |
| `DamagingState` | 护盾检查与伤害应用 |
| `PreDamageState` | 防御技能与最终伤害结算 |
| `HealingState` | 治疗结算 |
| `TrueDamageState` | 真实伤害结算（无视护盾和防御） |
| `PlayerTurnEndState` | 回合结束，切换到下一位玩家 |
| `HandleStackState` | 弹出状态栈中的下一个状态 |

## 伤害管线

一次完整攻击的伤害流程（以数字 7 触发攻击为例）：

```
ApplyingEffectState
  └→ 检测到数字 7 → 设置 Damage = 1，重置数字为 1
  └→ 推入 OrderingAttackState
        │
        ├→ 遍历攻击方 OrderingAttack 技能 (PlusAttack → DoubleAttack → Long → Bian)
        │   每个技能：弹出 → 询问玩家是否使用 → 修改上下文中的 Damage
        │
        ├→ 推入攻击方 OrderedAttack 技能 (Xian)
        │
        └→ 过渡到 TargetSelectionState
              │
              └→ 玩家选择目标
              └→ Damage > 0 → 过渡到 DamagingState
                    │
                    ├→ 检查目标护盾 (HasShield)
                    │   ├→ 有护盾: 护盾破碎 (0→1)，Damage 清零
                    │   │   └→ 重新检查数字效果 (链式反应)
                    │   └→ 无护盾: 过渡到 PreDamageState
                    │         │
                    │         ├→ 遍历防御方 Defending 技能 (PlusDefence → DoubleDefence)
                    │         │   每个技能：弹出 → 自动或确认后减少 Damage（最低 1）
                    │         │
                    │         ├→ target.TakeDamage(finalDamage)
                    │         │
                    │         ├→ 攻击方 Damaging 技能 (Xue — 自动回血)
                    │         │
                    │         └→ 防御方 Damaged 技能 (Mo → Shen)
                    │
                    └→ Damage > 0? → DamagingState → PreDamageState → ...
                        否则 → 回合结束
```

### 伤害最小值

所有经过防御技能减伤后的伤害，最低为 **1**。即无法将伤害降至 0（除非护盾完全抵消）。

### 护盾优先级

1. 护盾检查在防御技能之前
2. 护盾破碎后，攻击直接结束（不进入 PreDamageState）
3. 真实伤害跳过护盾检查

## 技能上下文 (SkillContext)

`SkillContext` 是技能管线中的数据载体，在状态间传递：

```
SkillContext:
  SourcePlayerId    — 当前技能来源玩家 ID（可变）
  PipelineOriginId  — 管线发起者 ID（锁定，不可变）
  TargetPlayerId    — 目标玩家 ID
  Damage            — 当前伤害值
  Healing           — 当前治疗值
  Numbers[]         — 技能激活时的数字快照
  SkillEnumerator   — 当前触发条件下的技能迭代器
```

### Derive 机制

`SkillContext.Derive(newSourcePlayerId)` 创建子上下文：
- `SourcePlayerId` 更新为新值
- `PipelineOriginId` 保持原始值不变

这确保了在多步技能管线中始终能追溯到最初的发起者（例如防御技能将 SourcePlayerId 切换为防御方后，管线结束后需要恢复到原始攻击方）。

## 链式反应

数字变化可能触发链式反应。流程如下：

```
数字变化 (SetNumber)
  └→ ApplyingEffectState 检查组合
      └→ 触发效果 (攻击/治疗/护盾)
          └→ 效果结算后数字可能再次变化
              └→ 推入新的 ApplyingEffectState
                  └→ 再次检查组合 → 可能再次触发...
```

防止无限递归：状态栈有一个 **100 次迭代的安全上限**（`HandleStackState` 中的 `StackIterationCount`）。

## 队伍系统 (Group)

`Group` 类支持组队模式（Co-op）：

- 每个 `Group` 包含若干 `Player`
- `Group.IsAlive()` — 只要组内有任意成员存活，队伍即为存活
- 游戏结束判定：只剩 0 或 1 个队伍存活时结束

### 使用场景

- **单人模式**: 每个玩家各自为队（默认行为）
- **联机 Co-op**: 人类玩家组队 vs AI 组队

## 玩家控制器架构

游戏使用控制器模式分离输入来源：

| 控制器 | 类 | 行为 |
|--------|-----|------|
| 本地玩家 | `LocalPlayerController` | `RequestAction` 为空操作，UI 层被动响应 |
| AI 玩家 | `AgentPlayerController` | 调用 `AgentManager.GetAgentAction()` 自动决策 |
| 远程玩家 | `RemotePlayerController` | 等待网络消息驱动 |

`GameManager` 向控制器广播 `PlayerActionRequired` 事件，不关心玩家是人类还是 AI。

## AI 系统

- 使用 ONNX 强化学习模型（`agent_GamePhaseType.CHOOSE_NUMBER_INDEX_*.onnx`）
- 输入状态向量 `[sourceHP, sourceNum0, sourceNum1, userHP, targetNum0, targetNum1]`（6 个 float）
- 输出 Q 值最大的动作索引
- AI 仅在选数字第一步使用神经网络；后续决策使用启发式规则（如攻击血量最低的敌人）

## 网络协议

- **传输层**: TCP Socket（双栈 IPv4/IPv6）
- **消息格式**: `[MessageType: 1 byte][PayloadLength: 2 bytes][Payload: N bytes]`
- **GameAction 序列化**: BinaryWriter/BinaryReader
- **消息类型**: 包含连接、大厅、游戏同步、聊天、断开等 19 种消息类型
