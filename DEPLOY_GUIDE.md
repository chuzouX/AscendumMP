# 新电脑部署指南

## 你的项目结构

```
AscendumReleaseDemo/
├── 原版/                 ← 未修改的原版游戏（仅供参考）
│   ├── Ascendum.exe
│   └── Ascendum_Data/Managed/Assembly-CSharp.dll  (696KB 原版)
├── Ascendum_Data/Managed/
│   ├── Assembly-CSharp.dll     ← 已打补丁
│   ├── Assembly-CSharp.dll.bak ← 原版备份
│   ├── MultiplayerMod.dll      ← 网络模块
│   └── DevConsoleMod.dll       ← 控制台
└── Ascendum.exe
```

## 你需要复制的文件

从最新备份（如 `Backups\loading_invitation_gate_20260519\`）取出：

| 复制到新电脑 | 目标路径 |
|---|---|
| `源代码\MultiplayerMod.cs` | 构建用，放源码目录 |
| `源代码\Patcher_Program.cs` | 构建用，放源码目录 |
| `源代码\DevConsole.cs` | 构建用，放源码目录 |
| `DeployedManaged\*` | 可直接部署到新游戏 |
| 或自己构建 → | `Patcher.dll`, `MultiplayerMod.dll`, `DevConsoleMod.dll` |

## 方案 A：直接部署（无需构建）

如果你的新电脑游戏版本与当前**完全相同**：

```powershell
# 1. 备份
cd "新电脑\Ascendum_Data\Managed"
copy Assembly-CSharp.dll Assembly-CSharp.dll.bak

# 2. 从备份的 DeployedManaged 复制
copy "备份\Assembly-CSharp.dll" .
copy "备份\MultiplayerMod.dll" .
copy "备份\DevConsoleMod.dll" .
```

**前置条件**：新电脑的 `Assembly-CSharp.dll` 原版哈希必须一致。验证：
```powershell
certutil -hashfile "原版\Assembly-CSharp.dll" MD5
certutil -hashfile "新电脑\Assembly-CSharp.dll" MD5
```

## 方案 B：重新构建 + 打补丁

如果游戏版本不同，或想从干净的原版开始：

### 1. 安装构建工具

```powershell
# 安装 .NET 8.0 SDK (用于 Patcher)
winget install Microsoft.DotNet.SDK.8

# 安装 .NET Framework 4.7.2 目标包 (用于 Mod DLL)
# 通常 Visual Studio Build Tools 自带，或从：
# https://dotnet.microsoft.com/download/dotnet-framework/net472
```

### 2. 复制构建源码

```powershell
mkdir C:\ascendum_patcher
# 复制整个 Patcher/, MultiplayerMod/, DevConsoleMod/ 目录
```

### 3. 构建

```powershell
cd C:\ascendum_patcher\Patcher
dotnet build -c Release

cd C:\ascendum_patcher\MultiplayerMod
dotnet build -c Release

cd C:\ascendum_patcher\DevConsoleMod
dotnet build -c Release
```

### 4. 备份原版 DLL

```powershell
cd "游戏目录\Ascendum_Data\Managed"
copy Assembly-CSharp.dll Assembly-CSharp.dll.bak
```

### 5. 打 IL 补丁

```powershell
dotnet Patcher\bin\Release\net8.0\Patcher.dll `
    "游戏目录\Ascendum_Data\Managed\Assembly-CSharp.dll" `
    "MultiplayerMod\bin\Release\net472\MultiplayerMod.dll" `
    "DevConsoleMod\bin\Release\net472\DevConsoleMod.dll"
```

这会输出 `Assembly-CSharp.dll.patched`。

### 6. 部署

```powershell
# 替换游戏 DLL
copy "游戏目录\Ascendum_Data\Managed\Assembly-CSharp.dll.patched" `
     "游戏目录\Ascendum_Data\Managed\Assembly-CSharp.dll"

# 复制 Mod DLL（Patcher 已自动复制 MultiplayerMod.dll 和 DevConsoleMod.dll）
```

## IL 补丁做了哪些修改

### 1. SteamManager 模拟
- `Awake()` → 初始化 NetworkManager + DevConsole
- `OnEnable/OnDestroy/Update` → 清空（防止调用未初始化的 Steam API）

### 2. InputManager 输入重定向（14 个方法）
- **Holy（7个）**：主机用 WASD (`GetHoly*`)，客户端阻止
- **Shadow（7个）**：双方用网络输入/本地 WASD (`GetShadow*`)
- 包括移动、射击、跳跃、确认、取消

### 3. 全局场景加载拦截
- 所有 `SceneManager.LoadScene` 调用 → `NetworkManager.SafeLoadScene`
- 排除 `LoadingManager`、`LevelMenu*`、`*Coor` 等（避免场景转换崩溃）

### 4. LoadingManager 邀请门（新增）
- `LoadNextScene` / `LoadNextSceneNoLoading` → 先调用 `OnHostRequestLevel`
- 主机选关 → 触发邀请流程 → 阻止立即加载
- 客户端同意后 → 双方一起进关

### 5. inTesting 解锁
- `StringConstants.inTesting` → 强制返回 `true`
- `TestingShowAllButtons` → 替换为安全实现

### 6. AchivementManager 禁用
- `Start()` / `LateUpdate()` → 清空

## 验证

1. 启动游戏 → 无 Steam 报错
2. 按 `~` → 开发者控制台出现
3. 按 F2 → 联机菜单出现
4. F3 开房，另一实例 F4 连接
5. 主机选关 → 客户端看到邀请 → F6 同意
6. 跳跃、射击、交互正常同步
