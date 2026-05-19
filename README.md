# Ascendum Demo - Multiplayer Patch

## 项目概述

本项目对 Unity 游戏 **Ascendum Demo** 进行 IL 级别的修改，实现以下目标：

1. **破解 Demo 限制** — 解锁完整游戏内容
2. **绕过 Steam** — 无需 Steam 客户端即可运行
3. **远程联机** — 将本地双人合作改为远程多人游戏（主机控制 Holy，客户端控制 Shadow）

## 游戏基本信息

| 项目 | 值 |
|------|-----|
| 游戏名 | Ascendum Demo |
| 引擎 | Unity 2018.2.13f1 (Mono/.NET 3.5) |
| 架构 | 32-bit x86 |
| 目标 DLL | `Ascendum_Data\Managed\Assembly-CSharp.dll` |
| 原始大小 | 696,832 bytes |
| 补丁后大小 | ~672,768 bytes |
| 输入系统 | Rewired（双人本地合作：Holy + Shadow） |
| 玩家角色 | Holy（圣光，玩家1）+ Shadow（暗影，玩家2） |

## 项目结构

```
ascendum_patcher/
├── Patcher/                        # IL 补丁工具 (.NET 8.0 控制台应用)
│   ├── Program.cs                  # Mono.Cecil 补丁逻辑
│   └── Patcher.csproj
├── MultiplayerMod/                 # 多人联网模块 (.NET 4.7.2 类库)
│   ├── MultiplayerMod.cs           # NetworkManager + 网络协议
│   └── MultiplayerMod.csproj
└── README.md                       # 本文档
```

## 工具依赖

- **Mono.Cecil 0.11.6** — IL 操作库，用于修改编译后的 .NET 程序集
- **ilspycmd 8.2.0** — 反编译工具，用于验证补丁结果
- **.NET 8.0 SDK** — 编译 Patcher
- **.NET Framework 4.7.2 Targeting Pack** — 编译 MultiplayerMod（与 Unity Mono 兼容）

## 补丁详解 (Patcher/Program.cs)

### 1. StringConstants.inTesting → true

```csharp
// 原始: 可能返回 false（Demo 模式）
// 补丁: 强制返回 true
getter.Body.Instructions.Clear();
il.Append(il.Create(OpCodes.Ldc_I4_1));  // return true
il.Append(il.Create(OpCodes.Ret));
```

**作用**: 游戏检测 `inTesting` 来判断是否为 Demo 版本。设为 true 可解锁完整内容。

### 2. FinishDemoScript.Video_loopPointReached

```csharp
// 原始: 播放 Demo 结束视频后退出
// 补丁: 直接加载 LevelMenu_Zone1
il.Append(il.Create(OpCodes.Ldstr, "LevelMenu_Zone1"));
il.Append(il.Create(OpCodes.Call, loadNext));
```

**作用**: Demo 版本播放完结束动画后会退出。补丁使其跳转到关卡选择菜单。

### 3. MenuController.StartDemo

```csharp
// 原始: 启动 Demo 流程
// 补丁: 直接加载 LevelMenu_Zone1
il.Append(il.Create(OpCodes.Ldstr, "LevelMenu_Zone1"));
il.Append(il.Create(OpCodes.Call, loadNextNoFade));
```

**作用**: 点击"开始游戏"直接进入关卡选择页面。

### 4. SteamManager.Awake → Bypass Steam + Init Network

```csharp
// 完全重写 Awake 方法
// 1. 跳过所有 Steam 初始化 (SteamAPI.Init)
// 2. 设置 SteamManager 单例
// 3. 调用 NetworkManager.Init() 启动联网模块
```

**原始逻辑**: 
- 检查单例 → 初始化 Steamworks → 设置回调 → SteamAPI.Init()

**补丁后逻辑**:
- 检查单例 → 设置 s_instance → DontDestroyOnLoad → m_bInitialized = false → NetworkManager.Init()

**作用**: 
1. 绕过 Steam 依赖（不需要 Steam 客户端运行）
2. 在 SteamManager 初始化时注入 NetworkManager

### 5. AchivementManager (2 methods no-op'd)

```csharp
// UnlockAchivement → 空方法 (直接 return)
// HasUnlockedAchivement → return false
```

**作用**: 禁用成就系统，防止因 Steam 未运行导致的成就相关错误。

### 6. InputManager 网络输入补丁 (10 methods)

#### 架构设计

```
┌─────────────────────────────────────────────────┐
│                    HOST (主机)                    │
│  Holy: 本地键盘输入 (原始代码)                      │
│  Shadow: 从客户端接收的网络输入 (remoteInput)       │
└─────────────────────────────────────────────────┘
                        ↕ TCP 网络
┌─────────────────────────────────────────────────┐
│                   CLIENT (客户端)                  │
│  Holy: 被阻止 (返回 0/false)                       │
│  Shadow: 本地键盘输入 (原始代码) → 发送给主机        │
└─────────────────────────────────────────────────┘
```

#### Host Shadow 补丁 (5 methods)

使用 **InsertBefore** 方式（不破坏原始代码）：

```csharp
// ShadowMovementValue, JumpStart_Shadow, JumpHold_Shadow, 
// JumpRelease_Shadow, Shoot_Shadow
//
// IL 逻辑:
// if (!IsNetworkActive) goto original;        // 非网络模式用原始代码
// if (!isHost) goto original;                 // 客户端用原始代码
// if (!bothReady) goto original;              // 未就绪用原始代码
// return remoteInput.xxx;                     // 主机返回客户端输入
// original: (原始代码继续)
```

#### Client Holy 补丁 (5 methods)

```csharp
// HolyMovementValue, JumpStart_Holy, JumpHold_Holy,
// JumpRelease_Holy, Shoot_Holy
//
// IL 逻辑:
// if (!IsNetworkActive) goto original;        // 非网络模式用原始代码
// if (isHost) goto original;                  // 主机用原始代码
// if (!bothReady) goto original;              // 未就绪用原始代码
// return 0/false;                             // 客户端阻止 Holy 输入
// original: (原始代码继续)
```

**关键设计**: 使用 `InsertBefore` 而非 `Body.Instructions.Clear()`，因为 Mono.Cecil 0.11.6 的 Clear() 在此 Unity 版本中存在 bug（清除无效，导致重复代码）。

### 7. LoadingManager 客户端关卡加载阻止

```csharp
// LoadNextScene(string, float, bool) — 只阻止 string 重载
// LoadNextSceneNoLoading(string, bool, float) — 只阻止 string 重载
//
// IL 逻辑:
// if (!IsNetworkActive || isHost) goto original;  // 主机或非网络正常加载
// return;                                         // 客户端阻止加载
// original: (原始代码继续)
```

**作用**: 客户端不能通过游戏 UI 选择关卡，只能通过网络同步自动加载主机选择的关卡。

## 多人联网模块 (MultiplayerMod/MultiplayerMod.cs)

### 网络协议

```
TCP 连接，端口 7777
数据包格式: [type:1字节][length_lo:1字节][length_hi:1字节][data:N字节]

type = 0x01: 客户端→主机 (InputState)
type = 0x02: 主机→客户端 (StateSync)
```

### 数据结构

#### NetworkInputState (8 bytes) — 客户端发送给主机

| 字段 | 类型 | 大小 | 说明 |
|------|------|------|------|
| move | float | 4 | 水平移动 (-1 ~ 1) |
| jump | bool | 1 | 跳跃按下 |
| jumpRelease | bool | 1 | 跳跃释放 |
| shoot | bool | 1 | 射击 |
| escape | bool | 1 | 逃跑 |

#### NetworkStateSync (28 bytes) — 主机发送给客户端

| 字段 | 类型 | 大小 | 说明 |
|------|------|------|------|
| holyX, holyY | float×2 | 8 | Holy 玩家位置 |
| shadowX, shadowY | float×2 | 8 | Shadow 玩家位置 |
| sceneIndex | int | 4 | 当前场景 buildIndex |
| holyFacingRight | bool | 1 | Holy 朝向 |
| shadowFacingRight | bool | 1 | Shadow 朝向 |
| levelCompleted | bool | 1 | 关卡是否完成 |
| levelReady | bool | 1 | 关卡是否就绪 |
| targetScene | int | 4 | 目标场景 buildIndex |

### NetworkManager 类

继承 `MonoBehaviour`，通过 `DontDestroyOnLoad` 跨场景持久化。

#### 核心流程

```
┌─ Update() ─────────────────────────────────────┐
│  1. 处理 F2/F3/F4/F5 按键                       │
│  2. 检查后台线程连接标志                          │
│  3. Host: 读取客户端输入 → 发送游戏状态           │
│  4. Client: 发送输入 → 读取状态 → 自动加载场景    │
└─────────────────────────────────────────────────┘
```

#### 连接方式

由于 Unity Mono 运行时的线程限制（后台线程无法执行），使用以下方案：

- **主机**: `TcpListener` 在主线程创建和启动，`AcceptTcpClient` 在后台线程阻塞等待，通过 `volatile bool` 标志通知主线程
- **客户端**: `TcpClient.Connect` 在后台线程阻塞连接，通过 `volatile bool` 标志通知主线程
- **数据收发**: 主线程每帧通过 `stream.DataAvailable` 非阻塞读取，直接写入发送

#### BothReady 握手机制

```
主机启动 → 监听端口
         ↓
客户端连接 → 双方建立 TCP 连接
         ↓
主机选择关卡 → 进入游戏场景
         ↓
主机发送 targetScene → 客户端自动 LoadScene
         ↓
客户端加载完成 → clientLevelReady = true
         ↓
客户端回复 sceneIndex → 主机检测到同一场景
         ↓
bothReady = true → 开始同步操作
```

**关键**: `bothReady` 是 `public static` 字段，IL 补丁直接读取此字段来决定是否拦截输入。

### 场景同步 (ApplyReceivedState)

```csharp
// 客户端每帧调用
// 1. 检查 targetScene 是否变化 → 变化则 LoadScene
// 2. 等待 clientLevelReady
// 3. 同步 Holy 和 Shadow 的位置
```

使用反射访问 `LevelManager` 的 `inGamePlayerHoly` 和 `inGamePlayerShadow` 字段，避免循环依赖。

### 按键检测

使用 `Input.GetKey` + 手动防抖（`lastF2`/`lastF3`/`lastF4`/`lastF5`），因为 Rewired 输入系统可能拦截 `Input.GetKeyDown`。

## 构建和运行

### 编译

```bash
# 编译 MultiplayerMod (需要 .NET Framework 4.7.2 targeting pack)
cd MultiplayerMod
dotnet build -c Release

# 编译 Patcher (需要 .NET 8.0 SDK)
cd Patcher
dotnet build -c Release
```

### 执行补丁

```bash
# 1. 备份原始 DLL（仅首次）
cp "游戏目录/Ascendum_Data/Managed/Assembly-CSharp.dll" \
   "游戏目录/Ascendum_Data/Managed/Assembly-CSharp.dll.bak"

# 2. 运行补丁
dotnet Patcher/bin/Release/net8.0/Patcher.dll \
  "游戏目录/Ascendum_Data/Managed/Assembly-CSharp.dll" \
  "MultiplayerMod/bin/Release/net472/MultiplayerMod.dll"

# 3. 覆盖原始 DLL
cp "游戏目录/Ascendum_Data/Managed/Assembly-CSharp.dll.patched" \
   "游戏目录/Ascendum_Data/Managed/Assembly-CSharp.dll"
```

### 运行游戏

1. 启动 `Ascendum.exe`（两次，分别作为主机和客户端）
2. 主机按 F2 打开网络菜单 → F3 启动主机
3. 客户端按 F2 打开网络菜单 → F4 连接 (默认 127.0.0.1)
4. 主机选择关卡进入 → 客户端自动加载
5. 双方就绪后开始游戏

### 快捷键

| 按键 | 功能 |
|------|------|
| F2 | 打开/关闭网络菜单 |
| F3 | 快速启动主机 |
| F4 | 快速连接 localhost |
| F5 | 断开连接 |

## 已知问题和限制

### 1. Mono 线程限制

Unity Mono 运行时对后台线程有严格限制。`Thread.Start()` 启动的线程可能不执行。解决方案：后台线程只做阻塞 I/O（Accept/Connect），通过 `volatile` 标志通知主线程。

### 2. Mono.Cecil Clear() Bug

`Body.Instructions.Clear()` 在此版本的 Mono.Cecil 中不能正确清除指令列表。解决方案：使用 `InsertBefore` 方式在方法开头插入检查逻辑，保留原始代码。

### 3. Rewired 输入拦截

游戏使用 Rewired 输入系统，可能拦截 `Input.GetKeyDown`。解决方案：使用 `Input.GetKey` + 手动防抖。

### 4. 端口占用

主机启动失败时端口可能未释放。解决方案：`StartHost()` 中先调用 `server.Stop()` 清理旧连接。

### 5. 客户端场景加载

客户端通过 `SceneManager.LoadScene()` 直接加载场景（绕过 LoadingManager），因为 LoadingManager 的加载方法已被 IL 补丁阻止。

### 6. 位置同步精度

当前使用直接赋值方式同步位置，可能造成客户端抖动。未来可考虑插值平滑。

## 未来改进方向

1. **客户端预测** — 客户端本地模拟 Shadow 运动，主机校正
2. **输入缓冲** — 客户端发送输入队列，主机按帧处理
3. **场景加载同步** — 更完善的加载状态同步（加载进度条等）
4. **断线重连** — 支持连接断开后重新加入
5. **多客户端** — 支持多个客户端连接（目前仅 1v1）
6. **UDP 通信** — 使用 UDP 替代 TCP 提高实时性
