# Fida

一个基于数字组合的回合制策略对战游戏：每个玩家持有两个数字，按顺序行动——选择自己的一个数字，再选择自己或对手的另一个数字将其叠加，数字组合触发攻击/治疗/防御效果，17 种技能改变攻防格局，生命值恰好归零者出局。支持本地（对战 AI）与联机（局域网广播/直填地址，零服务器；relay 仅作 NAT 兜底）。

## 技术栈

| 组件 | 版本 |
|---|---|
| 运行时 | .NET 8.0（`net8.0`，可滚动到更高 SDK） |
| 游戏框架 | MonoGame.Framework.DesktopGL 3.8.4 |
| AI 决策 | 统一 value table 评分 + 规则引擎模拟，可调实力，无推理库依赖 |
| 测试 | xUnit 2.7 |
| 联机 | TCP 直连（局域网广播 + 地址直填，无需服务器）+ 纯字节 relay 兜底 |

## 构建与运行

```bash
# 还原 + 构建
dotnet build

# 运行（首次构建会自动执行 `dotnet tool restore` 安装 MonoGame 内容管线）
dotnet run

# 测试（240 个，xUnit）
dotnet test
```

在受限沙箱里 `dotnet test` 会在启动 testhost 时中止（VSTest 打不开父进程句柄，
`Win32Exception (5)`，与代码无关）。此时改用等价运行器：它加载已编译的测试程序集并
逐个调用 `[Fact]`/`[Theory]`，输出与 `dotnet test` 相同。

```bash
dotnet build tools/TestRunner/TestRunner.csproj -m:1 -p:BuildInParallel=false
dotnet Fida.Tests/bin/Debug/net8.0/TestRunner.dll                 # 全部测试
dotnet Fida.Tests/bin/Debug/net8.0/TestRunner.dll Fida.Tests.dll Fida.Tests.RelayTests
```

`TestRunner.dll` 必须输出到 `Fida.Tests` 的输出目录，因为战役/Gum 数据通过
`AppContext.BaseDirectory` 解析 `Content/`。设 `RUNNER_DETAIL=1` 可打印失败用例的内部异常栈。

启动后应看到轻量 Agent 加载成功：

```
Lightweight agents ready, strength 65, table True
```

修改 `Content/agent-settings.json` 的 `Strength`（0–100）并重启即可调整实力。
0 随机选择合法动作，100 贪心选择最高评分；默认 65。双人无技能、双方生命上限 11 的局面
直接读取当前 C# 规则训练的 Q 值；技能、目标选择和多人局通过规则引擎模拟动作后统一查 value table。
不使用手动战术评分或缺表兜底。参见 [轻量 Agent 说明](Doc/LightweightAgent.md)。

## 玩法

1. 玩家按顺序行动：先选自己的一个数字，再选自己或对手的一个数字，将该数字加到前者之上。
2. 数字（组合）变化后查表结算：

   | 数字 / 组合 | 效果 |
   |---|---|
   | 7 | -1 |
   | 8 | -1 |
   | 9 | +1 |
   | 1–6 | -2 |
   | 5-5 | -3 |
   | 6-6 | -5 |
   | 0 | 防御一次攻击 |
   | 0-0 | +5 |

3. 生命值**恰好归零**时游戏结束。

### 技能（17 种）

| 键 | 名称 | 效果 |
|---|---|---|
| 1 | 双倍攻击 | 你的攻击伤害翻倍 |
| 2 | 双倍防御 | 你受到的伤害减半 |
| 3 | 三倍生命 | 生命值上限增加到三倍 |
| 4 | 神 | 每回合一次，受伤时可改一个数字 |
| 5 | 仙 | 每回合一次，攻击时可改一个数字 |
| 6 | 魔 | 被攻击时，返回一半伤害给任意对手 |
| 7 | 血 | 攻击造成伤害后，恢复等量血量 |
| 8 | 同 | 回合开始时若两数字相同，可同时改变 |
| 9 | 帝 | 回合开始时可选改变一个数字 |
| 10 | 火 | 发动攻击时可同时攻击所有目标 |
| 11 | 石 | 回合第一次收到攻击时不受伤害 |
| 12 | 变 | 攻击时可将数值改为 1–5 任意值 |
| 13 | 龙 | 攻击时可将数值改为两数字之和 |

（枚举另含 `Gui`，未在规则文本中列出。）

### 本地模式

主菜单「开始」→ 树状战役地图 → 在节点预算内配置多个已解锁技能 → 本地小队战。战役包含 18 个节点、两条中期分支、动态增援与普通/完整路线结局；进度保存在本机应用数据目录。

内置章节按文件拆分在 `Campaign/Data/`。额外节点可写入 `Content/campaign/nodes.json`，无需修改地图 UI；相同 ID 会覆盖内置节点。无效父节点、循环依赖、超编敌人和未实现技能会被校验并安全回退。

战役地图按文件夹逐层浏览，而不再一次展开所有节点。节点可在 JSON 中设置 `"mapPath": ["Chapter II", "Bell Tower"]` 指定任意深度的地图层级；未设置时按 `chapter` 中的 ` - ` 自动分层。进入文件夹只改变展示范围，关卡的 `parents` 解锁依赖仍跨文件夹生效。

瑞士风格分支的 UI 字体为 IBM Plex Sans（Regular 正文、SemiBold 标题）。字体文件与 OFL 许可随 `Content/fonts/` 一同打包；桌面字体图集采用无损颜色格式，减少小字锯齿。

## 联机

主菜单「联机模式」→ 服务器列表（`server.csv`）→ 创建房间 / 加入房间 → 大厅 → 选择模式 → 开始。

大厅提供两种模式（`OnlineLobby`）：
- **与好友对战（PvP）**：主机 + 客机，1v1
- **Campaign Co-op（联机战役）**：两名玩家在同一张战役地图上各自独立选关；双方进入同一关卡的配装界面后需同时 Ready 才开始双人合作战斗，也可独自开打；一方已在战斗中时，另一方可以进入同一关卡增援（回合边界快照传送）。

### 服务器列表 `server.csv`

4 列：`地址,端口,名称,类型`。`relay` 标志（不区分大小写）决定**整局**的配对方式，客户端恒取第一有效行：

```csv
# 直连（默认）：不需要任何服务器
::1,8888,Direct (LAN or typed address)
# relay 兜底：取消注释即切换回房间码模式
# 175.27.254.213,8888,游戏服务器,relay
```

> 直连模式下地址列不参与配对，端口列只作为默认游戏端口；relay 模式下地址/端口是 relay 的入口。

### 直连（默认，零服务器）

`Create game` 直接在所有网卡的 8888 端口监听（`::` 双栈，IPv4 与 IPv6 都能进），同时每秒向 8890/UDP 广播一次 `FIDA1|<端口>`：

- **同一局域网**：客机点 `Join game`，地址框**留空** → 监听广播 3 秒，听到主机即自动连上，双方都不需要填地址。
- **跨网络（公网 IPv6）**：主机界面大号显示 `192.168.1.7:8888`，下方列出 `LAN …` 与 `IPV6 [2408:…]:8888`；把它发给对手，客机填进地址框按 `Join game`。
- **地址格式**：`192.168.1.7:8888`、`192.168.1.7`（用默认端口）、`[2408::1]:8888`、`2408::1`，也接受粘贴的 `fida://[…]`；不接受域名（传输层只拨 IP 字面量）。
- 首次监听时 Windows 会弹一次防火墙授权，勾选「专用网络」即可；8890/UDP 只需在局域网内可达。
- 双方都在 CGNAT（手机热点、校园网、部分移动宽带）后面时直连必然不通——这时才需要下面的 relay，或者用免费隧道把主机端口暴露出去。

### Relay 模式（可选兜底，NAT 穿透，房间码）

两个 NAT 后的玩家无法直连时，双方都**出站**连入公网 relay；relay 用 Go 实现（`relay/`），分配 4 位房间码做配对，握手完成后双向字节桥接，不解析游戏协议（零帧协议改动）：

```
主机 GameRelayHost ──'H'────────────────────► relay（分配 4 位房间码）
   ◄──────────────── 0x01 + 房间码            │
   （等待客机，界面显示房间码）                 │
                                             │
客机 GameClient  ──'C' + 0x01 + 房间码 ─────► relay
   ◄──────────────── 0x03（加入成功）          │
主机 ◄────────────── 0x02（对手已加入）        │
                                             ▼
                          双向字节桥接（纯转发，不解析帧）
```

- **主机**走 `GameRelayHost`：出站连 relay → 发 `'H'` → 收 `0x01`+4 位房间码 → 显示房间码并等待客机（`0x02`）→ 把 relay 流当对端收发帧
- **客机**走 `GameClient`：出站连 relay → 输入 4 位房间码 → 发 `'C'+0x01+房间码` → 收 `0x03` 视为连接成功；房间码无效/房间满 → relay 直接断开，客户端收到"连接失败"
- **帧协议**不变：`[type:1][len:2][payload:N]`，relay 只转发不解析
- **房间码**：30 个无歧义字符（`23456789ABCDEFGHJKMNPQRSTUVWXYZ`，无 0/1/I/L/O）× 4 位；主机等待客机上限 5 分钟

#### 部署 relay（VPS）

```bash
# 交叉编译（本机 Linux/Windows/macOS 均可）
cd relay
GOOS=linux GOARCH=amd64 go build -ldflags="-s -w" -o fida-relay .

# 上传
scp fida-relay user@vps:/home/user/fida-relay

# systemd 服务
sudo tee /etc/systemd/system/fida-relay.service <<'EOF'
[Unit]
Description=Fida Relay
After=network.target
[Service]
ExecStart=/home/user/fida-relay -port 8888
Restart=always
User=user
[Install]
WantedBy=multi-user.target
EOF
sudo systemctl daemon-reload && sudo systemctl enable --now fida-relay

# 放行端口
sudo ufw allow 8888/tcp   # 云安全组也需放行
```

单文件静态二进制，无运行时依赖（Go 标准库）。

不想维护 VPS 时有两个免费替代：一是用免费隧道（如 [playit.gg](https://playit.gg/)）把主机的 8888 端口暴露成公网地址，客机把该地址填进直连地址框即可，连 `relay/` 都不用跑；二是把 `relay/` 丢进免费层的容器/虚拟机。两者都只在 CGNAT 场景才有必要。

### 消息类型（`NetworkMessage`）

`[MessageType(1B)][PayloadLength(2B, 小端)][Payload(NB)]`

| 0x.. | 类型 | 0x.. | 类型 |
|---|---|---|---|
| 0x01 | ConnectRequest | 0x30 | LobbyJoin |
| 0x02 | ConnectAccept | 0x31 | LobbyUpdate |
| 0x03 | ConnectReject | 0x32 | LobbyStartGameRequest |
| 0x10 | GameStart | 0x33 | LobbyStartGameResponse |
| 0x11 | TurnStart | 0x34 | LobbyStartGameResult |
| 0x12 | PlayerAction | 0x35 | LobbyLeave |
| 0x13 | StateSync | 0x36 | StartGamePvP |
| 0x14 | NumberChanged | 0x37 | StartGameCoop |
| 0x15 | AttackExecuted | 0x20 | ChatMessage |
| 0x16 | HealExecuted | 0xFF | Disconnect |
| 0x17 | GameEnd | | |
| 0x18 | GameAction | | |

## 项目结构

```
Fida/
├── Program.cs               入口
├── Game1.cs                 MonoGame 主循环（状态机 + 各 Manager 装配）
├── GameStateMachine.cs      菜单状态机（主菜单/联机/大厅/托管/连接/暂停）
├── GameSessionManager.cs    对局会话生命周期
├── ResourceManager.cs       资源管理
├── MenuInputHandler.cs      菜单输入
├── ExitHandler.cs           退出处理
├── server.csv               服务器列表（地址,端口,名称,类型）
├── relay/                   Go 实现的公网 relay（房间码配对 + 字节桥接）
├── gametext.json            规则/技能/文案
├── Agent/
│   ├── AgentManager.cs      轻量 AI 接入、实力配置、事件兼容
│   └── OnnxInference.cs     历史 ONNX 封装（不参与编译）
├── Fida.Core/Agents/        通用决策、合法动作、评分表、实力采样
├── Campaign/
│   ├── Data/                按章节拆分的18个内置节点
│   ├── Battle/              规则/脚本Agent、控制器工厂、动态增援
│   ├── Infrastructure/      JSON节点扩展与可替换存档
│   └── Campaign*.cs         图校验、技能预算、进度与战斗工厂
├── Logic/
│   ├── GameManager.cs       核心：玩家/分组/回合/状态委托
│   ├── GameQuery.cs         只读查询封装
│   ├── GameState.cs         状态接口 + 各状态
│   ├── States/              GameStartState 等具体状态
│   ├── Player.cs            玩家（数字/血量/技能）
│   ├── Skill.cs             17 技能 + 触发时机 + 使用次数
│   ├── Group.cs             分组（队伍）
│   ├── GameEvent.cs         事件载荷
│   └── *PlayerController.cs 本地/AI/远程 控制器
├── Network/
│   ├── NetworkManager.cs    网络门面（直连/relay 统一 API）
│   ├── NetworkMessage.cs    帧编解码 + MessageType
│   ├── GameServer.cs        直连主机端（`::` 双栈监听）
│   ├── GameClient.cs        客机端（直连 + relay）
│   ├── GameRelayHost.cs     relay 主机端（出站连 relay）
│   ├── LanDiscovery.cs      局域网广播发现（主机 beacon / 客机 browser，8890/UDP）
│   ├── EndpointAddress.cs   直连地址解析（IPv4/IPv6 字面量 + 可选端口）
│   └── ServerConfig.cs      server.csv 解析
├── States/                  菜单状态（IMenuState 实现）
├── UI/                      按钮/标签/焦点/动画
├── Scene/PlayingSceneManager.cs  对局场景
├── Content/screens/*.json    界面布局（主菜单/规则/关卡选择/技能选择/联机…）
├── Content/campaign/        可增补或覆盖的战役节点配置
├── Content/scenes/*.csv     旧版场景配置（保留兼容）
├── Content/                 字体 + 轻量 Agent 评分表与实力配置
└── Fida.Tests/              xUnit 测试
```

## 测试

```bash
dotnet test
```

覆盖：数字组合结算、技能触发、玩家/分组、`ChooseNumberIndex` 状态机、`server.csv` 解析、直连地址解析、局域网 beacon/browser、relay 房间码握手（好码/错码）、端到端 relay 桥接。

## 约定

- 中文注释 / 中文提交信息
- 每步门禁：`dotnet build` 0 错 0 警告 → `dotnet test` 全绿 → `Fida.dll` 启动加载轻量 Agent 无异常
- 不改帧协议 / UI / 状态机；主机走 `GameServer` 语义、客机走 `GameClient` 语义
- 绝不把备份 `.cs` 留在项目根（SDK glob 会扫进导致重复类型错误）
