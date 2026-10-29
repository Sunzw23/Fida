# 轻量、可调实力 Agent

## 使用

编辑 `Content/agent-settings.json`，重新构建/启动游戏：

```json
{ "Strength": 65, "Seed": null, "SimulationNodeBudget": 256, "SimulationDepth": 8 }
```

`Strength` 必须在 0–100。0 均匀随机选择合法动作，100 贪心最高评分。
建议体验档位：25 入门、50 普通、65 困难、80 专家、100 最高。
这些档位是选择质量参数，不是胜率百分比；技能局的同一档位不保证与普通双人局等强。
`SimulationNodeBudget` 和 `SimulationDepth` 限制技能连锁中的候选展开量，默认 256/8；
所有根候选都会比较，剩余展开预算均分，预算耗尽后仍然只查 value table。
`Seed` 可选，用于重现决策；每个 Agent 拥有独立的随机数和组合计划。

代码可调用 `AgentManager.SetStrength(50)` 更新该 manager 的 Agent；战役创建控制器时使用
`AgentDefaults.Profile`，已创建的战役控制器保持创建时的配置，也可直接用
`new LightweightAgentController(playerId, new AgentProfile { Strength = 50 })` 指定单个敌人实力。
历史节点的 `Rule` 与 `Scripted` 类型都由同一个 table 控制器执行，保留枚举仅兼容已有节点 JSON。
旧规则/脚本控制器代码保留供历史测试使用，运行时工厂不再选择它们。

## 决策与覆盖范围

`LightweightAgent` 枚举合法动作，按评分采样，输出现有 `GameAction`。
普通回合将两个数字选择作为一个组合决策，并将第二步计划保存在每位 Agent 内；
技能选择索引和改数字单独处理，不复用普通回合计划。

**所有自动 AI 评分仅使用同一张 value table，没有规则评分分支。**
恰好两名玩家、无技能、双方最大生命值 11 的正常选数决策可直接读取六个 Q 值。
状态包含真实血量 1–11、双方完整数字；从行动方视角编码，并将每方数字排序。
六个动作按「自己左/右 × 对手左/右/自己另一个」编码，运行时映射回原玩家 ID 和数字位置。
基础表训练时使用攻击对手、治疗效果源自身的目标约定，包含护盾连锁；
所以六个基础 Q 值对应**该目标约定和折扣奖励**，不是全技能完整博弈解。
运行时的目标选择也模拟全部合法候选、查表比较，不再硬编码目标。

技能、多人和不同生命上限的决策遵循统一流程：

1. 从当前步骤枚举合法动作，不根据技能名字写 AI 策略。
2. 复制规则状态，包括状态栈、技能次数、步骤结果和技能流水线枚举器，剥离外部事件。
3. 在副本执行候选动作；继续展开尚未结算的技能/目标决策，友方取最大值、敌方取最小值。
4. 到下一个正常选数阶段、终局或预算边界，读取 `V(s) = max Q(s,a)`。
5. 按原有实力温度和动作损失参数采样。

多人局把友方与敌方两两投影到基础表，取平均作为队伍局面值；敌方回合从敌方视角查值后取负。
不同生命上限使用实际血量查表，超过 11 的血量投影到最高档；实际治疗上限和技能效果由引擎执行。
终局值是胜 +1、负 -1，后续回合值使用与基础训练一致的 0.99 折扣。

这是**基础 value table 的投影估值**：当前技能效果在模拟中按真实规则结算，但表自身没有持久技能、
技能次数或多人联合状态维度，不能声称是这些模式的精确 VI 解。它没有攻击/治疗/技能特判分数，
新增技能不需要写专用评分器。缺失或损坏模型显式报错，不切换到手动策略。

状态副本通过缓存的反射字段复制当前规则对象图与集合，LINQ 技能筛选的纯函数共享；
测试覆盖全部已实现技能以及事件隔离。以后新增含外部资源、可变委托闭包的规则对象时，
应补充副本隔离测试。这是模拟器的工程约束，不是维护技能策略表。

## 实力采样

评分统一到约 [-1,1]，令 `s = Strength / 100`：

```text
温度 T = 2 × (1 − s)²
最大允许评分损失 δ = 2 × (1 − s⁸)
Δ(a) = max(Q) − Q(a)
保留 Δ(a) ≤ δ 的动作
P(a) ∝ exp(−Δ(a) / T)
```

0 和 100 单独处理。强度越高越偏向好动作，但实战胜率受对手、先后手与局面影响，
不保证每个整数档位都严格单调。相同评分的动作在中间档位随机选择，100 按固定顺序打破平局。

## 压缩与资源

状态从 1,210,000 个有序数字状态减少到 366,025 个数字对称状态。
VI 结果先量化成 int16，最大量化误差约 0.000015；当前结果只有 64 种分值，
再存储分值字典和每项 6 位的索引。后一步对量化后的评分是无损压缩。

模型 gzip 文件约 1 MiB，常驻评分数组与字典约 1.57 MiB。
评分表只解压一次，所有 Agent 共用；每次普通双人决策只读取六个评分。
**表内大于 6 的血量没有被合并**，7/8/9 等血量仍然精确区分。
模型作为 `Fida.Core` 的嵌入资源供桌面和 Android 共用，运行时无需 Python、Jupyter、Torch 或 ONNX。

## 训练与验证

相邻仓库 `fida_agent` 的 `tools/FidaTransitions` 直接引用当前 `Fida.Core`，
遍历状态与六个动作，使用真实状态机生成转移，不使用历史 Python 规则。

```powershell
# 在 fida_agent 根目录
dotnet run --project tools/FidaTransitions -c Release -- models/vi/fida-csharp-transitions.gz 11
python scripts/train_lightweight.py models/vi/fida-csharp-transitions.gz ../Fida/Content/agents/base.q.gz
```

第二步只依赖 NumPy，也可在 Jupyter 执行。`scripts/jupyter_train.mjs` 自动上传转移和训练脚本、
创建独立训练目录和 Python kernel、保存 notebook、下载模型与评估报告，最后关闭所创建的 kernel。
Node 22+，token 从标准输入读取，不写进 notebook 或仓库文件：

```powershell
$jupyterToken = Read-Host 'Jupyter token'
$jupyterToken | node scripts/jupyter_train.mjs http://localhost:18888 models/vi/fida-csharp-transitions.gz scripts/train_lightweight.py ../Fida/Content/agents
```

修改规则后须重新导出转移、训练并重建游戏；修改模型格式或目标约定时须同步规则版本与读取端。
默认评估 6 个档位，每档分别对随机与最高档运行 400 局，共 4,800 局；
一半标准开局，一半固定的随机决策局面，交换先后手。400 回合未结束计为评估平局，
这不是给实际游戏添加回合上限。报告见 `Content/agents/base.q.report.json`。

该报告针对基础表六动作的训练约定；技能/多人投影及新的目标搜索未使用该报告做胜率校准。
回归测试覆盖实力端点、随机种子、评分损失、数字对称映射、真实引擎完整对局、
全部已实现技能的合法动作与副本隔离、技能流水线枚举器隔离、33 血量投影和控制器路由。
