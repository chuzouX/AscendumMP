using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Patcher
{
    static void Main(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("Usage: Patcher <target-dll> <multiplayer-mod-dll> [dev-console-mod-dll]"); return; }
        string dllPath = args[0];
        string modDllPath = args[1];
        string? devConsoleDllPath = args.Length >= 3 ? args[2] : null;
        string managedDir = Path.GetDirectoryName(dllPath);

        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(managedDir);
        var rp = new ReaderParameters { ReadWrite = false, AssemblyResolver = resolver };
        var asm = AssemblyDefinition.ReadAssembly(dllPath, rp);
        var modAsm = AssemblyDefinition.ReadAssembly(modDllPath, rp);
        AssemblyDefinition? devConsoleAsm = null;
        if (!string.IsNullOrEmpty(devConsoleDllPath))
            devConsoleAsm = AssemblyDefinition.ReadAssembly(devConsoleDllPath, rp);
        var module = asm.MainModule;
        int patched = 0;

        File.Copy(modDllPath, Path.Combine(managedDir, "MultiplayerMod.dll"), true);
        Console.WriteLine("[+] Copied MultiplayerMod.dll to Managed folder");
        if (!string.IsNullOrEmpty(devConsoleDllPath))
        {
            File.Copy(devConsoleDllPath, Path.Combine(managedDir, "DevConsoleMod.dll"), true);
            Console.WriteLine("[+] Copied DevConsoleMod.dll to Managed folder");
        }

        patched += PatchInTesting(module);
        patched += PatchFinishDemoScript(module);
        patched += PatchMenuController(module);
        patched += PatchSteamManager(module, modAsm.MainModule, devConsoleAsm?.MainModule);
        patched += PatchAchivement(module);
        patched += PatchInputManager(module, modAsm.MainModule);
        patched += PatchGlobalLoadingInterceptor(module, modAsm.MainModule);
        patched += PatchLoadingManagerInvitation(module, modAsm.MainModule);

        string tmpPath = dllPath + ".patched";
        asm.Write(tmpPath);
        Console.WriteLine("\n[+] " + patched + " patches applied => " + tmpPath);
    }

    static int PatchGlobalLoadingInterceptor(ModuleDefinition module, ModuleDefinition modAsm)
    {
        var nmType = modAsm.GetType("NetworkManager");
        if (nmType == null) return 0;
        var safeLoads = nmType.Methods.Where(m => m.Name.StartsWith("SafeLoad")).ToList();
        
        int count = 0;
        foreach (var type in module.Types)
        {
            foreach (var method in type.Methods)
            {
                if (!method.HasBody) continue;
                string tn = type.FullName;
                if (tn == "NetworkManager" || tn == "Patcher") continue;
                // Exclude scene-transition pipeline classes — intercepting their LoadScene calls
                // during scene unload/load causes mono.dll ACCESS_VIOLATION crashes.
                // Broad exclusion: any coroutine helper (*Coor), any loading/scene manager.
                if (tn == "LoadingManager" || tn == "MainLoadCoor" || tn == "CutsceneCoor" ||
                    tn == "NextLevelCoor" || tn == "MenuCreditsCoor" || tn == "LevelMenuController" ||
                    tn == "LevelMenuConnector" || tn.StartsWith("LevelMenu") ||
                    tn.EndsWith("Coor") || tn.Contains("Loading") || tn.Contains("LoadCoor")) continue;
                var instrs = method.Body.Instructions;
                for (int i = 0; i < instrs.Count; i++)
                {
                    var instr = instrs[i];
                    if (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt)
                    {
                        var target = instr.Operand as MethodReference;
                        if (target == null) continue;
                        string tName = target.Name;
                        string tType = target.DeclaringType.FullName;
                        bool isUnityLoad = (tType == "UnityEngine.SceneManagement.SceneManager" || tType == "UnityEngine.Application")
                                           && (tName.StartsWith("LoadScene") || tName.StartsWith("LoadLevel"))
                                           && !tName.Contains("Async"); // skip async variants — SafeLoad*Async returns null when blocked, causing NPE→ACCESS_VIOLATION
                        if (isUnityLoad)
                        {
                            var match = safeLoads.FirstOrDefault(m =>
                                m.Name.Replace("Scene", "").Replace("Level", "") == target.Name.Replace("Scene", "").Replace("Level", "") &&
                                m.Parameters.Count == target.Parameters.Count &&
                                m.Parameters[0].ParameterType.Name == target.Parameters[0].ParameterType.Name &&
                                m.ReturnType.Name == target.ReturnType.Name);
                            if (match != null) { instr.Operand = module.ImportReference(match); count++; }
                        }
                    }
                }
            }
        }
        Console.WriteLine("[+] Global Load Interceptor => redirected " + count + " total calls");
        return count;
    }

    static int PatchInTesting(ModuleDefinition module)
    {
        // Set inTesting=true to unlock full game content, but also fix
        // TestingShowAllButtons() which crashes on the release scene because
        // bonus LevelButton slots are not populated.
        int count = 0;

        var strConstType = module.GetType("StringConstants");
        if (strConstType != null)
        {
            var prop = strConstType.Properties.FirstOrDefault(p => p.Name == "inTesting");
            if (prop?.GetMethod != null && prop.GetMethod.HasBody)
            {
                var getter = prop.GetMethod;
                getter.Body.Instructions.Clear();
                getter.Body.Variables.Clear();
                getter.Body.ExceptionHandlers.Clear();
                var il = getter.Body.GetILProcessor();
                il.Append(il.Create(OpCodes.Ldc_I4_1));
                il.Append(il.Create(OpCodes.Ret));
                count++;
            }
        }

        // Fix TestingShowAllButtons: replace with safe normal flow (HandleLevelButtons)
        var lmmType = module.GetType("LevelMenuMap");
        if (lmmType != null)
        {
            var testShow = lmmType.Methods.FirstOrDefault(m => m.Name == "TestingShowAllButtons");
            if (testShow != null && testShow.HasBody)
            {
                var handleNormal = lmmType.Methods.FirstOrDefault(m => m.Name == "HandleLevelButtons");
                var handleConn = lmmType.Methods.FirstOrDefault(m => m.Name == "HandleConnections");
                if (handleNormal != null && handleConn != null)
                {
                    testShow.Body.Instructions.Clear();
                    testShow.Body.Variables.Clear();
                    testShow.Body.ExceptionHandlers.Clear();
                    var il = testShow.Body.GetILProcessor();
                    il.Append(il.Create(OpCodes.Ldarg_0));
                    il.Append(il.Create(OpCodes.Call, handleNormal));
                    il.Append(il.Create(OpCodes.Ldarg_0));
                    il.Append(il.Create(OpCodes.Call, handleConn));
                    il.Append(il.Create(OpCodes.Ret));
                    count++;
                }
            }
        }

        Console.WriteLine("[+] inTesting => true (TestingShowAllButtons fixed)");
        return count;
    }

    static int PatchFinishDemoScript(ModuleDefinition module)
    {
        // The original Video_loopPointReached already calls LoadingManager.LoadNextScene("LevelMenu_Zone1").
        // No patch needed.
        return 0;
    }

    static int PatchMenuController(ModuleDefinition module)
    {
        // The original StartDemo flow correctly selects save state and routes to
        // LevelMenu_Zone1 if the player has played before. No patch needed.
        return 0;
    }

    static int PatchSteamManager(ModuleDefinition module, ModuleDefinition modAsm, ModuleDefinition? devConsoleAsm)
    {
        var type = module.GetType("SteamManager");
        if (type == null) return 0;
        var awake = type.Methods.First(m => m.Name == "Awake");
        var networkInit = module.ImportReference(modAsm.GetType("NetworkManager").Methods.First(m => m.Name == "Init"));
        MethodReference? devConsoleInit = null;
        if (devConsoleAsm != null)
        {
            var devConsoleType = devConsoleAsm.GetType("DevConsole");
            if (devConsoleType != null)
                devConsoleInit = module.ImportReference(devConsoleType.Methods.First(m => m.Name == "Init"));
        }
        var s_instance = type.Fields.First(f => f.Name == "s_instance");
        var m_bInitialized = type.Fields.First(f => f.Name == "m_bInitialized");
        var s_EverInitialized = type.Fields.First(f => f.Name == "s_EverInitialized");
        
        awake.Body.Instructions.Clear();
        awake.Body.Variables.Clear();
        awake.Body.ExceptionHandlers.Clear();
        var il = awake.Body.GetILProcessor();
        il.Append(il.Create(OpCodes.Ldarg_0));
        il.Append(il.Create(OpCodes.Stsfld, s_instance));
        il.Append(il.Create(OpCodes.Ldc_I4_1));
        il.Append(il.Create(OpCodes.Stsfld, s_EverInitialized));
        il.Append(il.Create(OpCodes.Ldarg_0));
        il.Append(il.Create(OpCodes.Ldc_I4_1));
        il.Append(il.Create(OpCodes.Stfld, m_bInitialized));
        il.Append(il.Create(OpCodes.Call, networkInit));
        if (devConsoleInit != null)
            il.Append(il.Create(OpCodes.Call, devConsoleInit));
        il.Append(il.Create(OpCodes.Ret));

        // Also disable OnEnable, OnDestroy, Update — they call Steam APIs
        // which will crash since Steam is not initialized.
        foreach (var name in new[] { "OnEnable", "OnDestroy", "Update" })
        {
            var m = type.Methods.FirstOrDefault(x => x.Name == name);
            if (m != null && m.HasBody)
            {
                m.Body.Instructions.Clear();
                m.Body.Variables.Clear();
                m.Body.ExceptionHandlers.Clear();
                m.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
        }

        Console.WriteLine("[+] SteamManager => Mocked + Network Init" + (devConsoleInit != null ? " + DevConsole Init" : ""));
        return 1;
    }

    static int PatchAchivement(ModuleDefinition module)
    {
        var type = module.GetType("AchivementManager");
        if (type == null) return 0;
        int count = 0;
        foreach (var m in type.Methods) { if ((m.Name == "Start" || m.Name == "LateUpdate") && m.HasBody) { m.Body.Instructions.Clear(); m.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); count++; } }
        return count;
    }

    static int PatchInputManager(ModuleDefinition module, ModuleDefinition modAsm)
    {
        var type = module.GetType("InputManager");
        if (type == null) return 0;
        var nmType = modAsm.GetType("NetworkManager");
        var blockHolyRef = module.ImportReference(nmType.Properties.First(p => p.Name == "BlockHolyInput").GetMethod);
        var overrideHolyRef = module.ImportReference(nmType.Properties.First(p => p.Name == "OverrideHolyInput").GetMethod);
        var overrideShadowRef = module.ImportReference(nmType.Properties.First(p => p.Name == "OverrideShadowInput").GetMethod);
        // Holy WASD getters (host uses WASD for Holy)
        var holyMoveRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetHolyMove"));
        var holyJumpStartRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetHolyJumpStart"));
        var holyJumpHoldRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetHolyJumpHold"));
        var holyJumpReleaseRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetHolyJumpRelease"));
        var holyShootRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetHolyShoot"));
        var holyShootHoldRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetHolyShootHold"));
        var holyShootReleaseRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetHolyShootRelease"));
        var holyConfirmRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetHolyConfirm"));
        // Shadow network getters (unchanged)
        var shadowMoveRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetShadowMove"));
        var shadowJumpStartRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetShadowJumpStart"));
        var shadowJumpHoldRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetShadowJumpHold"));
        var shadowJumpReleaseRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetShadowJumpRelease"));
        var shadowShootRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetShadowShoot"));
        var shadowShootHoldRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetShadowShootHold"));
        var shadowShootReleaseRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetShadowShootRelease"));
        var shadowConfirmRef = module.ImportReference(nmType.Methods.First(m => m.Name == "GetShadowConfirm"));
        int count = 0;

        // Holy: override to WASD on host, block on client
        count += OverrideHoly(type.Methods.FirstOrDefault(m => m.Name == "HolyMovementValue"), blockHolyRef, overrideHolyRef, holyMoveRef, true);
        count += OverrideHoly(type.Methods.FirstOrDefault(m => m.Name == "Shoot_Holy"), blockHolyRef, overrideHolyRef, holyShootRef, false);
        count += OverrideHoly(type.Methods.FirstOrDefault(m => m.Name == "JumpStart_Holy"), blockHolyRef, overrideHolyRef, holyJumpStartRef, false);
        count += OverrideHoly(type.Methods.FirstOrDefault(m => m.Name == "JumpHold_Holy"), blockHolyRef, overrideHolyRef, holyJumpHoldRef, false);
        count += OverrideHoly(type.Methods.FirstOrDefault(m => m.Name == "JumpRelease_Holy"), blockHolyRef, overrideHolyRef, holyJumpReleaseRef, false);
        count += OverrideHoly(type.Methods.FirstOrDefault(m => m.Name == "ConfirmHold_Holy"), blockHolyRef, overrideHolyRef, holyConfirmRef, false);
        count += OverrideHoly(type.Methods.FirstOrDefault(m => m.Name == "CancelHold_Holy"), blockHolyRef, overrideHolyRef, holyShootReleaseRef, false);

        count += OverrideShadow(type.Methods.FirstOrDefault(m => m.Name == "ShadowMovementValue"), overrideShadowRef, shadowMoveRef);
        count += OverrideShadow(type.Methods.FirstOrDefault(m => m.Name == "Shoot_Shadow"), overrideShadowRef, shadowShootRef);
        count += OverrideShadow(type.Methods.FirstOrDefault(m => m.Name == "JumpStart_Shadow"), overrideShadowRef, shadowJumpStartRef);
        count += OverrideShadow(type.Methods.FirstOrDefault(m => m.Name == "JumpHold_Shadow"), overrideShadowRef, shadowJumpHoldRef);
        count += OverrideShadow(type.Methods.FirstOrDefault(m => m.Name == "JumpRelease_Shadow"), overrideShadowRef, shadowJumpReleaseRef);
        count += OverrideShadow(type.Methods.FirstOrDefault(m => m.Name == "ConfirmHold_Shadow"), overrideShadowRef, shadowConfirmRef);
        count += OverrideShadow(type.Methods.FirstOrDefault(m => m.Name == "CancelHold_Shadow"), overrideShadowRef, shadowShootReleaseRef);

        Console.WriteLine("[+] InputManager => patched split character control methods: " + count);
        return count;
    }

    // Patch LoadingManager.LoadNextScene / LoadNextSceneNoLoading to go through
    // OnHostRequestLevel so the host can send an invitation before entering a level.
    static int PatchLoadingManagerInvitation(ModuleDefinition module, ModuleDefinition modAsm)
    {
        var nmType = modAsm.GetType("NetworkManager");
        if (nmType == null) return 0;
        var onHostReqLevel = module.ImportReference(nmType.Methods.First(m => m.Name == "OnHostRequestLevel"));
        int count = 0;

        var loadingManager = module.GetType("LoadingManager");
        if (loadingManager != null)
        {
            foreach (var method in loadingManager.Methods)
            {
                if (!method.HasBody) continue;
                if ((method.Name == "LoadNextScene" || method.Name == "LoadNextSceneNoLoading") &&
                    method.Parameters.Count > 0 &&
                    method.Parameters[0].ParameterType.FullName == "System.String")
                {
                    // Prepend: if (!OnHostRequestLevel(sceneName)) return;
                    var il = method.Body.GetILProcessor();
                    var first = method.Body.Instructions[0];
                    var skip = il.Create(OpCodes.Nop);
                    il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));          // load sceneName (first string param)
                    il.InsertBefore(first, il.Create(OpCodes.Call, onHostReqLevel)); // OnHostRequestLevel(sceneName)
                    il.InsertBefore(first, il.Create(OpCodes.Brtrue, skip));    // if true, continue
                    il.InsertBefore(first, il.Create(OpCodes.Ret));              // else return (block load)
                    il.InsertBefore(first, skip);
                    count++;
                }
            }
        }

        Console.WriteLine("[+] LoadingManager => host invitation gate: " + count);
        return count;
    }

    static int PatchClientLoadingRequests(ModuleDefinition module, ModuleDefinition modAsm)
    {
        var nmType = modAsm.GetType("NetworkManager");
        if (nmType == null) return 0;
        var isClientRef = module.ImportReference(nmType.Properties.First(p => p.Name == "IsNetworkClient").GetMethod);
        int count = 0;

        var loadingManager = module.GetType("LoadingManager");
        if (loadingManager != null)
        {
            foreach (var method in loadingManager.Methods)
            {
                if ((method.Name == "LoadNextScene" || method.Name == "LoadNextSceneNoLoading") &&
                    method.Parameters.Count > 0 &&
                    method.Parameters[0].ParameterType.FullName == "System.String")
                {
                    count += ReturnIfClient(method, isClientRef);
                }
            }
        }

        Console.WriteLine("[+] LoadingManager => client local load requests blocked: " + count);
        return count;
    }

    static int ReturnIfClient(MethodDefinition method, MethodReference isClientRef)
    {
        if (method == null || !method.HasBody || method.ReturnType.FullName != "System.Void") return 0;
        var il = method.Body.GetILProcessor();
        var first = method.Body.Instructions[0];
        var original = il.Create(OpCodes.Nop);
        il.InsertBefore(first, il.Create(OpCodes.Call, isClientRef));
        il.InsertBefore(first, il.Create(OpCodes.Brfalse, original));
        il.InsertBefore(first, il.Create(OpCodes.Ret));
        il.InsertBefore(first, original);
        return 1;
    }

    static int BlockHoly(MethodDefinition method, MethodReference shouldBlock, bool returnsFloat)
    {
        if (method == null || !method.HasBody) return 0;
        var il = method.Body.GetILProcessor();
        var first = method.Body.Instructions[0];
        var original = il.Create(OpCodes.Nop);
        il.InsertBefore(first, il.Create(OpCodes.Call, shouldBlock));
        il.InsertBefore(first, il.Create(OpCodes.Brfalse, original));
        il.InsertBefore(first, returnsFloat ? il.Create(OpCodes.Ldc_R4, 0f) : il.Create(OpCodes.Ldc_I4_0));
        il.InsertBefore(first, il.Create(OpCodes.Ret));
        il.InsertBefore(first, original);
        return 1;
    }

    static int OverrideShadow(MethodDefinition method, MethodReference shouldOverride, MethodReference valueGetter)
    {
        if (method == null || !method.HasBody) return 0;
        var il = method.Body.GetILProcessor();
        var first = method.Body.Instructions[0];
        var original = il.Create(OpCodes.Nop);
        il.InsertBefore(first, il.Create(OpCodes.Call, shouldOverride));
        il.InsertBefore(first, il.Create(OpCodes.Brfalse, original));
        il.InsertBefore(first, il.Create(OpCodes.Call, valueGetter));
        il.InsertBefore(first, il.Create(OpCodes.Ret));
        il.InsertBefore(first, original);
        return 1;
    }

    // OverrideHoly: on host → WASD getter, on client → blocked (return 0)
    static int OverrideHoly(MethodDefinition method, MethodReference blockHolyRef, MethodReference overrideHolyRef, MethodReference valueGetter, bool returnsFloat)
    {
        if (method == null || !method.HasBody) return 0;
        var il = method.Body.GetILProcessor();
        var first = method.Body.Instructions[0];
        var original = il.Create(OpCodes.Nop);
        var blocked = il.Create(OpCodes.Nop);
        // if (BlockHolyInput) return 0;  -- client blocks
        il.InsertBefore(first, il.Create(OpCodes.Call, blockHolyRef));
        il.InsertBefore(first, il.Create(OpCodes.Brtrue, blocked));
        // if (!OverrideHolyInput) goto original;  -- non-host uses Rewired
        il.InsertBefore(first, il.Create(OpCodes.Call, overrideHolyRef));
        il.InsertBefore(first, il.Create(OpCodes.Brfalse, original));
        // return WASD value  -- host uses WASD
        il.InsertBefore(first, il.Create(OpCodes.Call, valueGetter));
        il.InsertBefore(first, il.Create(OpCodes.Ret));
        // blocked: return 0
        il.InsertBefore(first, blocked);
        il.InsertBefore(first, returnsFloat ? il.Create(OpCodes.Ldc_R4, 0f) : il.Create(OpCodes.Ldc_I4_0));
        il.InsertBefore(first, il.Create(OpCodes.Ret));
        // original:
        il.InsertBefore(first, original);
        return 1;
    }
}
