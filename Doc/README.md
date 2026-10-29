# Fida 游戏文档

## 文档目录

| 文档 | 说明 |
|------|------|
| [GameRules.md](./GameRules.md) | 游戏基本规则：回合流程、数字效果、护盾机制、胜利条件 |
| [Skills.md](./Skills.md) | 技能系统详解：全部 17 个技能类型、触发时机、效果说明 |
| [GameMechanics.md](./GameMechanics.md) | 游戏深层机制：状态机流程、伤害管线、技能上下文、链式反应 |

## 项目简介

**Fida** 是一款回合制数字对战卡牌游戏，基于 MonoGame (DesktopGL) 构建，运行于 .NET 8.0。

### 核心玩法

- 多名玩家轮流行动
- 每回合选择自己的一个数字，加上任意玩家的另一个数字（取模 10）
- 特定数字组合触发攻击、治疗、护盾等效果
- 玩家拥有独特技能，在特定时机触发
- 将对手生命值归零即可获胜

### 技术栈

- **游戏引擎**: MonoGame DesktopGL 3.8.5
- **UI 框架**: Gum.MonoGame
- **AI**: ONNX Runtime (强化学习模型)
- **网络**: TCP Socket (支持 PvP 和 Co-op 联机)
- **运行时**: .NET 8.0
