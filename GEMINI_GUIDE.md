# Ascendum Demo - Gemini CLI 操作指南

## 项目概述

这是一个对 Unity 游戏 **Ascendum Demo** 进行 IL 级别修改的项目，实现：
1. 破解 Demo 限制 → 解锁完整游戏
2. 绕过 Steam → 无需 Steam 客户端
3. 远程联机 → 本地双人合作改为远程多人

## 目录结构

```
AscendumReleaseDemo/
├── Ascendum_Data/Managed/
│   ├── Assembly-CSharp.dll          # 目标 DLL（需要补丁）
│   ├── Assembly-CSharp.dll.bak      # 原始备份
│   └── MultiplayerMod.dll           # 联网模块（从构建目录复制）
└── Ascendum.exe                     # 游戏主程序

C:\Users\chuzo\AppData\Local\Temp\ascendum_patcher/
├── Patcher/
│   ├── Program.cs                   # IL 补丁代码（Mono.Cecil）
│   └── Patcher.csproj
├── MultiplayerMod/
│   ├── MultiplayerMod.cs            # 网络模块代码
│   └── MultiplayerMod.csproj
└── README.md                        # 完整项目文档
```

## 如何构建

```bash
# 1. 编译 MultiplayerMod（需要 .NET Framework 4.7.2）
cd C:\Users\chuzo\AppData\Local\Temp\ascendum_patcher\MultiplayerMod
dotnet build -c Release

# 2. 编译 Patcher（需要 .NET 8.0 SDK）
cd C:\Users\chuzo\AppData\Local\Temp\ascendum_patcher\Patcher
dotnet build -c Release
```

## 如何执行补丁

```bash
# 1. 运行补丁工具
cd C:\Users\chuzo\AppData\Local\Temp\ascendum_patcher
dotnet Patcher/bin/Release/net8.0/Patcher.dll \
  "E:/Program Files (x86)/Steam/steamapps/common/Ascendum Demo/AscendumReleaseDemo/Ascendum_Data/Managed/Assembly-CSharp.dll"

# 2. 复制联网模块到游戏目录
cp MultiplayerMod/bin/Release/net472/MultiplayerMod.dll \
   "E:/Program Files (x86)/Steam/steamapps/common/Ascendum Demo/AscendumReleaseDemo/Ascendum_Data/Managed/"
```

## 关键文件说明

### 1. Patcher/Program.cs — IL 补丁逻辑

这是最重要的文件，使用 Mono.Cecil 修改 Assembly-CSharp.dll。

**当前已实现的补丁（18个）：**

| 补丁 | 目标类/方法 | 作用 |
|------|------------|------|
| PatchInTesting | StringConstants.inTesting | 强制返回 true，解锁完整内容 |
| PatchFinishDemoScript | FinishDemoScript.Video_loopPointReached | 跳过 Demo 结束，加载 LevelMenu_Zone1 |
| PatchMenuController | MenuController.StartDemo | 直接进入关卡选择 |
| PatchSteamManager | SteamManager.Awake | 绕过 Steam，初始化 NetworkManager |
| PatchAchivement | AchivementManager (2个方法) | 禁用成就系统 |
| PatchInputManager | InputManager (10个方法) | 网络输入重定向 |
| PatchClientLevelBlock | LoadingManager (2个方法) | 阻止客户端选择关卡 |

**修改补丁的方法：**

```csharp
// 示例：修改 StringConstants.inTesting 的补丁
static int PatchInTesting(ModuleDefinition module)
{
    // 1. 找到目标类型
    var type = module.GetType("StringConstants");
    if (type == null) return 0;

    // 2. 找到目标属性/方法
    var prop = type.Properties.FirstOrDefault(p => p.Name == "inTesting");
    if (prop?.GetMethod == null || !prop.GetMethod.HasBody) return 0;
    var getter = prop.GetMethod;

    // 3. 清除原有代码（注意：Clear() 在此版本有 bug，需要用 InsertBefore）
    getter.Body.Instructions.Clear();
    getter.Body.Variables.Clear();
    getter.Body.ExceptionHandlers.Clear();

    // 4. 写入新的 IL 指令
    var il = getter.Body.GetILProcessor();
    il.Append(il.Create(OpCodes.Ldc_I4_1));  // 加载常量 1 (true)
    il.Append(il.Create(OpCodes.Ret));       // 返回

    return 1;
}
```

**添加新补丁的步骤：**

1. 在 `Program.cs` 中添加新方法：
```csharp
static int PatchNewFeature(ModuleDefinition module)
{
    var type = module.GetType("TargetClassName");
    if (type == null) return 0;
    var method = type.Methods.FirstOrDefault(m => m.Name == "MethodName");
    if (method == null || !method.HasBody) return 0;

    // 使用 InsertBefore 方式（推荐，避免 Clear() bug）
    var il = method.Body.GetILProcessor();
    var first = method.Body.Instructions[0];

    // 插入检查逻辑
    il.InsertBefore(first, il.Create(OpCodes.Nop)); // 示例

    return 1;
}
```

2. 在 `Main` 方法中调用：
```csharp
patched += PatchNewFeature(module);
```

### 2. MultiplayerMod/MultiplayerMod.cs — 网络模块

这是联网功能的核心，继承自 `MonoBehaviour`。

**关键类和字段：**

```csharp
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;      // 单例
    public static bool IsNetworkActive;         // 网络是否激活
    public static bool bothReady;               // 双方是否就绪（IL补丁读取此字段）
    public static NetworkInputState remoteInput; // 远程输入（客户端发来的）

    public bool isHost;                         // 是否是主机
    public int port = 7777;                     // 端口号
}
```

**修改网络协议的方法：**

1. 修改数据结构：
```csharp
public struct NetworkInputState
{
    public float move;
    public bool jump;
    // 添加新字段
    public bool newAction;  // 新增

    public byte[] Serialize()
    {
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms))
        {
            w.Write(move);
            w.Write(jump);
            w.Write(newAction);  // 新增
            return ms.ToArray();
        }
    }
}
```

2. 修改 IL 补丁以使用新字段（在 Patcher/Program.cs 中）

**修改连接方式的方法：**

当前使用 TCP，端口 7777。修改 `StartHost()` 和 `StartClient()` 方法。

**修改按键检测的方法：**

当前使用 `Input.GetKey` + 手动防抖（因为 Rewired 拦截 `Input.GetKeyDown`）：

```csharp
private bool lastF2;
private void Update()
{
    bool f2Now = Input.GetKey(KeyCode.F2);
    if (f2Now && !lastF2)
        showUI = !showUI;
    lastF2 = f2Now;
}
```

## 关键里程碑：双端同步选关系统 (F6 机制)

这是目前最核心的联网改造，解决了多人联机中“选关不同步”和“客户端偷跑”的难题。

### 实现逻辑

1.  **全局调用拦截 (Universal Interceptor)**：
    补丁工具不再 patch 单个类，而是扫描整个 `Assembly-CSharp.dll`，将**所有**对 Unity 原生跳转函数（`LoadScene`, `LoadLevel` 等）的调用，强行替换为我对接的 `NetworkManager.SafeLoadScene`。这保证了 100% 的拦截覆盖率。

2.  **F6 握手协议**：
    *   **拦截阶段**：当主机点击地图时，`SafeLoadScene` 捕获请求，主机进入 `INVITATION PENDING` 状态，画面不跳转，并向全网广播邀请。
    *   **确认阶段**：客户端收到信号，UI 提示 `F6 to agree`。客户端按下 **F6** 后发送同意回执。
    *   **同步进入**：主机收到回执，自动触发真正的场景加载。客户端检测到主机场景变化，自动跟随进入。

3.  **底层兼容性适配 (针对 .NET 3.5)**：
    *   **无锁化 (Lock-Free)**：去除了所有 `lock` 和 `Monitor` 调用，解决了老版本 Mono 环境下的 `MissingMethodException`。
    *   **强转检查**：使用 `(object)x != null` 代替重载运算符检查，确保在极其古老的运行时下不崩溃。
    *   **堆栈平衡**：在拦截非 Void 返回值（如异步操作）时，通过压入 `null` 维持了 CPU 堆栈的绝对平衡。

### 操作快捷键 (更新)

| 按键 | 功能 | 说明 |
|------|------------|------|
| **F2** | 切换菜单 | 显示联机状态、Packet 计数和邀请信息 |
| **F3** | 创建主机 | 开启 17777 端口监听 |
| **F4** | 连接主机 | 连接至 127.0.0.1:17777 (默认) |
| **F5** | 断开连接 | 彻底清理网络资源并重置状态 |
| **F6** | **确认/取消** | 主机按下取消邀请；客户端按下同意邀请 |

### 诊断信息说明

在 F2 菜单中：
*   **Pkt-I / Pkt-S**：若 Pkt-S 在跳动，说明状态同步正常，邀请信号一定能传达。
*   **INVITATION SENT**：主机端已成功拦截选关请求，正在等待队友。
*   **INVITATION RECEIVED**：客户端已收到请求，请按 F6 配合。

---
## 如何构建 (参考之前说明)

### 任务 1：添加新的网络输入字段

**步骤：**
1. 修改 `MultiplayerMod.cs` 中的 `NetworkInputState` 结构
2. 修改 `Patcher/Program.cs` 中对应的 InputManager 补丁
3. 重新编译和补丁

### 任务 2：修改同步的数据

**步骤：**
1. 修改 `MultiplayerMod.cs` 中的 `NetworkStateSync` 结构
2. 修改 `BuildHostState()` 和 `ApplyReceivedState()` 方法
3. 重新编译和补丁

### 任务 3：修改快捷键

**步骤：**
1. 修改 `MultiplayerMod.cs` 中 `Update()` 方法的按键检测
2. 注意：必须使用 `Input.GetKey` + 手动防抖，不能用 `Input.GetKeyDown`

### 任务 4：添加新的 IL 补丁

**步骤：**
1. 使用 `ilspycmd` 反编译目标 DLL 查看方法签名：
```bash
ilspycmd -p "E:/Program Files (x86)/Steam/steamapps/common/Ascendum Demo/AscendumReleaseDemo/Ascendum_Data/Managed/Assembly-CSharp.dll" | grep -A 10 "ClassName.MethodName"
```

2. 在 `Patcher/Program.cs` 中添加新补丁方法
3. 使用 `InsertBefore` 方式（推荐）或 `Clear()` + 重写

### 任务 5：调试补丁

**步骤：**
1. 使用 `ilspycmd` 反编译补丁后的 DLL 验证：
```bash
ilspycmd -p "E:/Program Files (x86)/Steam/steamapps/common/Ascendum Demo/AscendumReleaseDemo/Ascendum_Data/Managed/Assembly-CSharp.dll.patched" | grep -A 20 "ClassName.MethodName"
```

2. 检查 IL 指令是否正确

## 重要注意事项

### 1. Mono.Cecil Clear() Bug

在此版本的 Mono.Cecil 中，`Body.Instructions.Clear()` 不能正确清除指令列表。解决方案：
- 使用 `InsertBefore` 方式在方法开头插入检查逻辑
- 保留原始代码，通过 `goto` 跳转到原始代码

### 2. Unity Mono 线程限制

Unity Mono 运行时对后台线程有严格限制。`Thread.Start()` 启动的线程可能不执行。解决方案：
- 后台线程只做阻塞 I/O（Accept/Connect）
- 通过 `volatile` 标志通知主线程
- 主线程在 `Update()` 中检查标志并处理

### 3. Rewired 输入拦截

游戏使用 Rewired 输入系统，会拦截 `Input.GetKeyDown`。解决方案：
- 使用 `Input.GetKey` + 手动防抖
- 示例：
```csharp
private bool lastKey;
bool keyNow = Input.GetKey(KeyCode.F2);
if (keyNow && !lastKey)
    // 按下逻辑
lastKey = keyNow;
```

### 4. 两者就绪机制

`bothReady` 是 `public static` 字段，IL 补丁直接读取此字段来决定是否拦截输入。
- 主机和客户端都必须在同一场景中
- 两者都加载完成后才能开始同步操作

## 构建和测试流程

```bash
# 1. 编译
cd C:\Users\chuzo\AppData\Local\Temp\ascendum_patcher\MultiplayerMod
dotnet build -c Release

cd C:\Users\chuzo\AppData\Local\Temp\ascendum_patcher\Patcher
dotnet build -c Release

# 2. 执行补丁
cd C:\Users\chuzo\AppData\Local\Temp\ascendum_patcher
dotnet Patcher/bin/Release/net8.0/Patcher.dll \
  "E:/Program Files (x86)/Steam/steamapps/common/Ascendum Demo/AscendumReleaseDemo/Ascendum_Data/Managed/Assembly-CSharp.dll"

# 3. 复制联网模块
cp MultiplayerMod/bin/Release/net472/MultiplayerMod.dll \
   "E:/Program Files (x86)/Steam/steamapps/common/Ascendum Demo/AscendumReleaseDemo/Ascendum_Data/Managed/"

# 4. 测试
# 启动两个 Ascendum.exe 实例
# 实例1: 按 F3 启动主机
# 实例2: 按 F4 连接 localhost
```

## 调试技巧

### 1. 查看补丁日志

补丁工具会输出应用的补丁数量和目标文件路径。

### 2. 反编译验证

使用 `ilspycmd` 反编译补丁后的 DLL，检查方法是否被正确修改。

### 3. 检查网络连接

使用 `netstat` 检查端口：
```bash
netstat -an | grep 7777
```

### 4. 游戏日志

Unity 游戏日志通常在：
- Windows: `%USERPROFILE%\AppData\LocalLow\CompanyName\ProductName\output_log.txt`
- 或者游戏目录下的 `output_log.txt`

## 联系和支持

如有问题，请查看 `C:\Users\chuzo\AppData\Local\Temp\ascendum_patcher\README.md` 获取完整文档。
