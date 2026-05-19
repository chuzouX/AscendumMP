using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct ReceivedPacket
{
    public byte type;
    public byte[] data;
}

public struct NetworkInputState
{
    public float move;
    public bool jump;
    public bool jumpHold;
    public bool jumpRelease;
    public bool shoot;
    public bool shootHold;
    public bool shootRelease;
    public bool confirm;
    public bool holyCarryingObject;
    public bool shadowCarryingObject;
    public string holyCarryObjectName;
    public string shadowCarryObjectName;
    public bool escape;
    public bool agreed;

    public byte[] Serialize()
    {
        NetworkManager._serStream.Position = 0;
        NetworkManager._serStream.SetLength(0);
        var w = NetworkManager._serWriter;
        w.Write(move);
        w.Write(jump);
        w.Write(jumpHold);
        w.Write(jumpRelease);
        w.Write(shoot);
        w.Write(shootHold);
        w.Write(shootRelease);
        w.Write(confirm);
        w.Write(holyCarryingObject);
        w.Write(shadowCarryingObject);
        w.Write(holyCarryObjectName ?? "");
        w.Write(shadowCarryObjectName ?? "");
        w.Write(escape);
        w.Write(agreed);
        int len = (int)NetworkManager._serStream.Position;
        if (NetworkManager._serOutput.Length < len) NetworkManager._serOutput = new byte[len + 64];
        Array.Copy(NetworkManager._serStream.GetBuffer(), 0, NetworkManager._serOutput, 0, len);
        return NetworkManager._serOutput;
    }

    public static NetworkInputState Deserialize(byte[] data)
    {
        var ms = NetworkManager._deserStream;
        ms.Position = 0;
        ms.SetLength(0);
        ms.Write(data, 0, data.Length);
        ms.Position = 0;
        var r = NetworkManager._deserReader;
        if (data.Length < 15)
        {
            return new NetworkInputState
            {
                move = r.ReadSingle(),
                jump = r.ReadBoolean(),
                jumpHold = r.ReadBoolean(),
                jumpRelease = r.ReadBoolean(),
                shoot = r.ReadBoolean(),
                shootHold = r.ReadBoolean(),
                shootRelease = r.ReadBoolean(),
                confirm = r.ReadBoolean(),
                holyCarryingObject = false,
                shadowCarryingObject = false,
                holyCarryObjectName = "",
                shadowCarryObjectName = "",
                escape = r.ReadBoolean(),
                agreed = r.ReadBoolean()
            };
        }
        return new NetworkInputState
        {
            move = r.ReadSingle(),
            jump = r.ReadBoolean(),
            jumpHold = r.ReadBoolean(),
            jumpRelease = r.ReadBoolean(),
            shoot = r.ReadBoolean(),
              shootHold = r.ReadBoolean(),
              shootRelease = r.ReadBoolean(),
              confirm = r.ReadBoolean(),
              holyCarryingObject = r.ReadBoolean(),
              shadowCarryingObject = r.ReadBoolean(),
              holyCarryObjectName = r.ReadString(),
              shadowCarryObjectName = r.ReadString(),
              escape = r.ReadBoolean(),
              agreed = r.ReadBoolean()
          };
    }
}

public struct NetworkStateSync
{
    public float holyX, holyY;
    public float shadowX, shadowY;
    public int sceneIndex;
    public int targetScene;
    public int pendingInviteScene;
    public bool holyFacingRight;
    public bool shadowFacingRight;
    public bool levelCompleted;
    public bool levelReady;
    public bool isInviting;
    public int sceneGeneration;
    public bool holyCarryingObject;
    public bool shadowCarryingObject;
    public string holyCarryObjectName;
    public string shadowCarryObjectName;
    public int holyShootSeq;
    public int shadowShootSeq;

    public byte[] Serialize()
    {
        NetworkManager._serStream.Position = 0;
        NetworkManager._serStream.SetLength(0);
        var w = NetworkManager._serWriter;
        w.Write(holyX); w.Write(holyY);
        w.Write(shadowX); w.Write(shadowY);
        w.Write(sceneIndex);
        w.Write(targetScene);
        w.Write(pendingInviteScene);
        w.Write(holyFacingRight);
        w.Write(shadowFacingRight);
        w.Write(levelCompleted);
        w.Write(levelReady);
        w.Write(isInviting);
        w.Write(sceneGeneration);
        w.Write(holyCarryingObject);
        w.Write(shadowCarryingObject);
        w.Write(holyCarryObjectName ?? "");
        w.Write(shadowCarryObjectName ?? "");
        w.Write(holyShootSeq);
        w.Write(shadowShootSeq);
        int len = (int)NetworkManager._serStream.Position;
        if (NetworkManager._serOutput.Length < len) NetworkManager._serOutput = new byte[len + 64];
        Array.Copy(NetworkManager._serStream.GetBuffer(), 0, NetworkManager._serOutput, 0, len);
        return NetworkManager._serOutput;
    }

    public static NetworkStateSync Deserialize(byte[] data)
    {
        var ms = NetworkManager._deserStream;
        ms.Position = 0;
        ms.SetLength(0);
        ms.Write(data, 0, data.Length);
        ms.Position = 0;
        var r = NetworkManager._deserReader;
        return new NetworkStateSync
        {
            holyX = r.ReadSingle(), holyY = r.ReadSingle(),
            shadowX = r.ReadSingle(), shadowY = r.ReadSingle(),
            sceneIndex = r.ReadInt32(),
            targetScene = r.ReadInt32(),
            pendingInviteScene = r.ReadInt32(),
            holyFacingRight = r.ReadBoolean(),
            shadowFacingRight = r.ReadBoolean(),
              levelCompleted = r.ReadBoolean(),
              levelReady = r.ReadBoolean(),
              isInviting = r.ReadBoolean(),
              sceneGeneration = r.ReadInt32(),
              holyCarryingObject = r.ReadBoolean(),
              shadowCarryingObject = r.ReadBoolean(),
              holyCarryObjectName = r.ReadString(),
              shadowCarryObjectName = r.ReadString(),
              holyShootSeq = r.ReadInt32(),
              shadowShootSeq = r.ReadInt32()
          };
    }
}

public struct ClientStateSync
{
    // Input fields (matching NetworkInputState)
    public float move;
    public bool jump, jumpHold, jumpRelease;
    public bool shoot, shootHold, shootRelease;
    public bool confirm, escape, agreed;
    public bool holyCarryingObject, shadowCarryingObject;
    public string holyCarryObjectName, shadowCarryObjectName;
    // Client-authoritative position
    public float posX, posY;
    public bool facingRight;

    public byte[] Serialize()
    {
        NetworkManager._serStream.Position = 0;
        NetworkManager._serStream.SetLength(0);
        var w = NetworkManager._serWriter;
        w.Write(move);
        w.Write(jump); w.Write(jumpHold); w.Write(jumpRelease);
        w.Write(shoot); w.Write(shootHold); w.Write(shootRelease);
        w.Write(confirm); w.Write(escape); w.Write(agreed);
        w.Write(holyCarryingObject); w.Write(shadowCarryingObject);
        w.Write(holyCarryObjectName ?? "");
        w.Write(shadowCarryObjectName ?? "");
        w.Write(posX); w.Write(posY);
        w.Write(facingRight);
        int len = (int)NetworkManager._serStream.Position;
        if (NetworkManager._serOutput.Length < len) NetworkManager._serOutput = new byte[len + 64];
        Array.Copy(NetworkManager._serStream.GetBuffer(), 0, NetworkManager._serOutput, 0, len);
        return NetworkManager._serOutput;
    }

    public static ClientStateSync Deserialize(byte[] data)
    {
        var ms = NetworkManager._deserStream;
        ms.Position = 0; ms.SetLength(0);
        ms.Write(data, 0, data.Length);
        ms.Position = 0;
        var r = NetworkManager._deserReader;
        return new ClientStateSync
        {
            move = r.ReadSingle(),
            jump = r.ReadBoolean(), jumpHold = r.ReadBoolean(), jumpRelease = r.ReadBoolean(),
            shoot = r.ReadBoolean(), shootHold = r.ReadBoolean(), shootRelease = r.ReadBoolean(),
            confirm = r.ReadBoolean(), escape = r.ReadBoolean(), agreed = r.ReadBoolean(),
            holyCarryingObject = r.ReadBoolean(), shadowCarryingObject = r.ReadBoolean(),
            holyCarryObjectName = r.ReadString(), shadowCarryObjectName = r.ReadString(),
            posX = r.ReadSingle(), posY = r.ReadSingle(),
            facingRight = r.ReadBoolean()
        };
    }
}

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;
    public static bool IsNetworkActive
    {
        get { return instance != null && instance.isNetworkActive; }
    }
    public static bool IsHostPeer
    {
        get { return instance != null && instance.isNetworkActive && instance.isHost; }
    }
    public static bool IsNetworkClient
    {
        get { return instance != null && instance.isNetworkActive && !instance.isHost; }
    }
    public static bool BlockHolyInput
    {
        get { return instance != null && instance.isNetworkActive && !instance.isHost; }
    }
    public static bool OverrideShadowInput
    {
        get { return instance != null && instance.isNetworkActive; }
    }
    public static bool OverrideHolyInput
    {
        get { return instance != null && instance.isNetworkActive && instance.isHost; }
    }

    public bool isNetworkActive;
    public bool isHost;
    public int port = 17777;

    public static NetworkInputState remoteInput;
    public static NetworkStateSync lastStateSync;
    // Client-authoritative Shadow: position received from client
    public static float _remoteShadowX, _remoteShadowY;
    public static bool _remoteShadowFacing;

    private TcpListener server;
    private Socket clientSocket;
    private NetworkStream stream;
    private Socket connectSocket; // non-null while async connect is in progress

    private bool showUI;
    private bool mouseOverrideVisible;
    private bool lastF1;
    private string ipInput = "127.0.0.1";
    private string portInput = "17777";
    private string statusText = "Press F2 for network menu";

    public static float localShadowMove;
    public static bool localShadowJump;
    public static bool localShadowJumpHold;
    public static bool localShadowJumpRelease;
    public static bool localShadowShoot;
    public static bool localShadowShootHold;
    public static bool localShadowShootRelease;
    public static bool localShadowConfirm;
    public static bool localClientAgreed;
    private static bool cachedHolyCarryingObject;
    private static bool cachedShadowCarryingObject;
    private static bool cachedCarryStateValid;
    private static string cachedHolyCarryObjectName = "";
    private static string cachedShadowCarryObjectName = "";
    private static int carrySyncBlockedUntilFrame = -1;
    private static string lastConfirmLogKey;
    private static string lastRxClientInputKey;
    private static string lastRxStateSyncKey;
    private static string lastAppliedHolyCarryKey;
    private static string lastAppliedShadowCarryKey;
    private static bool lastRemoteConfirmDown;
    private static bool lastHostLocalConfirmDown;
    // Frame-preserving edge detection for game InputManager on client.
    // Multiple calls within the same frame see the same edge value.
    private static int _shadowJumpFrame, _shadowShootFrame;
    private static bool _shadowJumpStartEdge, _shadowJumpReleaseEdge;
    private static bool _shadowShootStartEdge, _shadowShootReleaseEdge;
    private static bool _shadowJumpDown, _shadowShootDown;
    // Host-side edge detection: client sends raw key-hold (jumpHold/shootHold).
    // Host detects edges and preserves them for the entire frame so multiple
    // calls (game InputManager + PollAndNotifyShootEvents) see the same value.
    private static int _hostJumpFrame, _hostShootFrame;
    private static bool _hostJumpStartEdge, _hostJumpReleaseEdge;
    private static bool _hostShootStartEdge, _hostShootReleaseEdge;
    private static bool _hostLastJumpHold, _hostLastShootHold;

    public static string pendingSceneName;
    public static bool localIsInviting;
    public static bool isActuallyLoading;
    public static bool bothReady;

    private int debugStatePackets;
    private int debugInputPackets;
    private int lastSceneIndex = -1;
    private int sceneStabilizeFrames;
    private static int sceneGeneration;
    private int lastReceivedSceneGeneration = -1;

    private static Type levelManagerType;
    private static FieldInfo levelManagerCurrentField;
    private static FieldInfo levelManagerHolyField;
    private static FieldInfo levelManagerShadowField;
    private static Type playerControllerType;
    private static FieldInfo playerFacingRightField;
    private static FieldInfo playerProjectileField;
    private static FieldInfo playerArmPosField;
    private static FieldInfo playerIgnoreWhenShootingField;
    private static FieldInfo playerInsideHoldAreaField;
    private static FieldInfo playerReadyToAttackField;
    private static FieldInfo playerKnockedBackField;
    private static MethodInfo playerFaceDirMethod;
    private static MethodInfo playerShootAnimMethod;
    private static MethodInfo playerIsCarryingObjectMethod;
    private static MethodInfo inputShootMethod;
    private static MethodInfo inputShootHolyMethod;
    private static MethodInfo inputShootShadowMethod;
    private static Type inputPlayerTypeEnum;
    private static Type pickUpObjectType;
    private static MethodInfo pickUpObjectOnCarrierChangedMethod;
    private static MethodInfo pickUpObjectOnItemPickedUpMethod;
    private static MethodInfo pickUpObjectObjectDroppedMethod;
    private static MethodInfo pickUpObjectSwitchCarrierMethod;
    private static FieldInfo pickUpObjectPickedUpField;
    private static FieldInfo pickUpObjectPlayerWhoCarriesField;
    private static FieldInfo pickUpObjectCollField;
    private static MethodInfo playerGetCarryObjectMethod;
    private static Type goldObjectType;
    private static MethodInfo goldObjectSpawnFormMethod;
    private static Type goldSphereControllerType;
    private static MethodInfo goldSphereChangeFormMethod;
    private static Type projectileType;
    private static MethodInfo projectileSetIgnoreObjMethod;
    private static int holyShootSeq;
    private static int shadowShootSeq;
    private static int lastAppliedHolyShootSeq;
    private static int lastAppliedShadowShootSeq;
    private static bool lastHolyShootPressed;
    private static bool lastShadowShootPressed;
    private static bool _holyShootFallbackDown;
    private static int lastAppliedCarryVisualType = int.MinValue;
    private static int lastAppliedCarryVisualScene = -1;

    // Cached player references — avoids per-frame reflection (GetLevelManager + FieldInfo.GetValue)
    private static GameObject _cachedHoly;
    private static GameObject _cachedShadow;
    private static int _cachedPlayerScene = -1;

    // Reusable serialization buffers to reduce per-frame GC allocations
    internal static MemoryStream _serStream = new MemoryStream(256);
    internal static BinaryWriter _serWriter = new BinaryWriter(_serStream);
    internal static byte[] _serOutput = new byte[256];
    internal static MemoryStream _deserStream = new MemoryStream(256);
    internal static BinaryReader _deserReader = new BinaryReader(_deserStream);

    public static void Init()
    {
        if (instance != null) return;
        var go = new GameObject("NetworkManager");
        instance = go.AddComponent<NetworkManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void SafeLoadScene(string n) { if (OnHostRequestLevel(n)) { isActuallyLoading = true; SceneManager.LoadScene(n); isActuallyLoading = false; } }
    public static void SafeLoadScene(string n, LoadSceneMode m) { if (OnHostRequestLevel(n)) { isActuallyLoading = true; SceneManager.LoadScene(n, m); isActuallyLoading = false; } }
    public static void SafeLoadScene(int i) { if (OnHostRequestLevel("Idx_" + i)) { isActuallyLoading = true; SceneManager.LoadScene(i); isActuallyLoading = false; } }
    public static void SafeLoadScene(int i, LoadSceneMode m) { if (OnHostRequestLevel("Idx_" + i)) { isActuallyLoading = true; SceneManager.LoadScene(i, m); isActuallyLoading = false; } }
    public static void SafeLoadLevel(string n) { if (OnHostRequestLevel(n)) { isActuallyLoading = true; SceneManager.LoadScene(n); isActuallyLoading = false; } }
    public static void SafeLoadLevel(int i) { if (OnHostRequestLevel("Idx_" + i)) { isActuallyLoading = true; SceneManager.LoadScene(i); isActuallyLoading = false; } }

    public static AsyncOperation SafeLoadSceneAsync(string n) { if (OnHostRequestLevel(n)) { isActuallyLoading = true; var op = SceneManager.LoadSceneAsync(n); isActuallyLoading = false; return op; } return null; }
    public static AsyncOperation SafeLoadSceneAsync(int i) { if (OnHostRequestLevel("Idx_" + i)) { isActuallyLoading = true; var op = SceneManager.LoadSceneAsync(i); isActuallyLoading = false; return op; } return null; }
    public static AsyncOperation SafeLoadLevelAsync(string n) { if (OnHostRequestLevel(n)) { isActuallyLoading = true; var op = SceneManager.LoadSceneAsync(n); isActuallyLoading = false; return op; } return null; }
    public static AsyncOperation SafeLoadLevelAsync(int i) { if (OnHostRequestLevel("Idx_" + i)) { isActuallyLoading = true; var op = SceneManager.LoadSceneAsync(i); isActuallyLoading = false; return op; } return null; }

    public static bool OnHostRequestLevel(string sceneName)
    {
        if (instance == null || !instance.isNetworkActive) return true;
        if (isActuallyLoading) return true;

        if (instance.isHost)
        {
            string sn = (sceneName == null) ? "" : sceneName.ToLower();
            if (sn.Contains("menu") || sn.Contains("logo") || sn.Contains("finish") || sn.Contains("start") || sn.Contains("credits"))
            {
                localIsInviting = false; localClientAgreed = false;
                return true;
            }

            // Respawn: same-scene reload — allow immediately, increment generation for client sync.
            if (sceneName == SceneManager.GetActiveScene().name)
            {
                localIsInviting = false;
                sceneGeneration++;
                return true;
            }

            if (localIsInviting && pendingSceneName == sceneName && remoteInput.agreed)
            {
                localIsInviting = false;
                sceneGeneration++;
                return true;
            }

            pendingSceneName = sceneName;
            localIsInviting = true;
            localClientAgreed = false;
            remoteInput.agreed = false; 
            
            return false;
        }
        return false;
    }

    private void Update()
    {
        HandleMouseVisibilityHotkey();

        // Non-blocking accept (main thread only — no background thread allocation)
        if (server != null && (object)clientSocket == null)
        {
            try { if (server.Pending()) { clientSocket = server.AcceptSocket(); stream = new NetworkStream(clientSocket, true); stream.ReadTimeout = 10; statusText = "Connected!"; } }
            catch (Exception e) { statusText = "Accept error: " + e.Message; }
        }

        // Non-blocking connect poll (main thread only)
        if ((object)connectSocket != null)
        {
            try
            {
                if (connectSocket.Poll(0, SelectMode.SelectWrite))
                {
                    connectSocket.Blocking = true;
                    clientSocket = connectSocket;
                    stream = new NetworkStream(clientSocket, true);
                    connectSocket = null;
                    statusText = "Connected!";
                }
            }
            catch (Exception e) { statusText = "Connect failed: " + e.Message; try { connectSocket.Close(); } catch { } connectSocket = null; isNetworkActive = false; }
        }

        if (!isNetworkActive) return;

        // Detect scene transitions (triggered by LoadingManager coroutines or
        // SafeLoadScene).  Skip several frames after the scene index changes so
        // that Awake/Start have finished on all objects in the new scene before
        // we try to access players through reflection.
        int curScene = SceneManager.GetActiveScene().buildIndex;
        if (curScene != lastSceneIndex)
        {
            lastSceneIndex = curScene;
            sceneStabilizeFrames = 3; // skip 3 frames after scene change
            return;
        }
        if (sceneStabilizeFrames > 0)
        {
            sceneStabilizeFrames--;
            if (sceneStabilizeFrames == 0) { System.GC.Collect(); }
            return;
        }

        // During scene transitions the player GameObjects are destroyed but
        // LevelManager may still hand out stale references.  Skip net logic
        // until the scene is fully loaded.
        if (!SceneManager.GetActiveScene().isLoaded) return;

        if (isHost && localIsInviting && remoteInput.agreed)
        {
            SafeLoadScene(pendingSceneName);
            return; // SceneManager.LoadScene was just called — skip rest of frame,
                    // scene objects are being destroyed and accessing players would crash.
        }

        if (!isHost && !lastStateSync.isInviting)
        {
            localClientAgreed = false;
        }

        if ((object)stream != null && (object)clientSocket != null && clientSocket.Connected)
        {
            ReceivedPacket? rp;
            while (true)
            {
                rp = TryReadPacket();
                if (!rp.HasValue) break;

                var packet = rp.Value;
                if (packet.type == 0x01) {
                    // ClientStateSync: Shadow position + input (new protocol, >= 24 bytes)
                    if (packet.data.Length >= 24) {
                        ClientStateSync clientState = ClientStateSync.Deserialize(packet.data);
                        remoteInput.move = clientState.move;
                        remoteInput.jump = clientState.jump;
                        remoteInput.jumpHold = clientState.jumpHold;
                        remoteInput.jumpRelease = clientState.jumpRelease;
                        remoteInput.shoot = clientState.shoot;
                        remoteInput.shootHold = clientState.shootHold;
                        remoteInput.shootRelease = clientState.shootRelease;
                        remoteInput.confirm = clientState.confirm;
                        remoteInput.holyCarryingObject = clientState.holyCarryingObject;
                        remoteInput.shadowCarryingObject = clientState.shadowCarryingObject;
                        remoteInput.holyCarryObjectName = clientState.holyCarryObjectName;
                        remoteInput.shadowCarryObjectName = clientState.shadowCarryObjectName;
                        remoteInput.escape = clientState.escape;
                        remoteInput.agreed = clientState.agreed;
                        CacheCarryObjectName(false, clientState.shadowCarryObjectName);
                        _remoteShadowX = clientState.posX;
                        _remoteShadowY = clientState.posY;
                        _remoteShadowFacing = clientState.facingRight;
                        debugInputPackets++;
                        LogRxClientInput(clientState);
                        // Host: apply client's Shadow position to local GameObject
                        if (isHost)
                        {
                            ApplyRemoteShadowPosition();
                            GameObject holy;
                            GameObject shadow;
                            if (TryGetPlayers(out holy, out shadow))
                            {
                                TryProcessRemoteCarryTransfer(holy, shadow);
                                if (!ShouldDeferCarrySync())
                                    ApplyCarryObjectState(shadow, remoteInput.shadowCarryObjectName, remoteInput.shadowCarryingObject, "shadow-input");
                            }
                        }
                    }
                    // Backward compat: old NetworkInputState (13 bytes, no position)
                    else if (packet.data.Length >= 13) {
                        remoteInput = NetworkInputState.Deserialize(packet.data);
                        debugInputPackets++;
                    }
                    } else if (packet.type == 0x02) {
                        if (packet.data.Length >= 43) {
                            lastStateSync = NetworkStateSync.Deserialize(packet.data);
                            CacheCarryObjectName(true, lastStateSync.holyCarryObjectName);
                            CacheCarryObjectName(false, lastStateSync.shadowCarryObjectName);
                            debugStatePackets++;
                            LogRxStateSync(lastStateSync);
                            if (!isHost)
                            {
                              CacheCarryState(lastStateSync.holyCarryingObject, lastStateSync.shadowCarryingObject, "state-sync");
                              ApplyReceivedState();
                              TryApplyShootEventsFromLatestState();
                            }
                        }
                }
            }
        }

        // Only access player objects in actual gameplay scenes (not menus).
        // Player GameObjects don't exist in menu/loading scenes, so
        // reflection would return destroyed-object wrappers.
        string sceneName = SceneManager.GetActiveScene().name;
        bool isGameScene = !string.IsNullOrEmpty(sceneName)
            && sceneName.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) < 0
            && sceneName.IndexOf("Logo", StringComparison.OrdinalIgnoreCase) < 0
            && sceneName.IndexOf("Start", StringComparison.OrdinalIgnoreCase) < 0
            && sceneName.IndexOf("Loading", StringComparison.OrdinalIgnoreCase) < 0
            && sceneName.IndexOf("Credits", StringComparison.OrdinalIgnoreCase) < 0
            && sceneName.IndexOf("Finish", StringComparison.OrdinalIgnoreCase) < 0;

        if (isHost && isGameScene) {
            if (ShouldDeferCarrySync())
            {
                return;
            }
            try { PollAndNotifyShootEvents(); } catch { }
            try
            {
                GameObject holy;
                GameObject shadow;
                if (TryGetPlayers(out holy, out shadow))
                    TryProcessHostCarryTransfer(holy, shadow);
            }
            catch { }
            try { BuildHostState(); } catch { return; }
            try { SendPacket(pendingHostState.Serialize(), 0x02); } catch { }
        } else if (isHost) {
            // Debug: host is NOT in game scene
            if (Time.frameCount % 120 == 0) Debug.Log("[NET] Host NOT in game scene. sceneName=" + sceneName + " isGameScene=" + isGameScene);
            // In menus, send a bare state packet (no player data) so client
            // sees scene changes and invitation flags.
            pendingHostState = new NetworkStateSync { sceneIndex = SceneManager.GetActiveScene().buildIndex, targetScene = SceneManager.GetActiveScene().buildIndex, levelReady = bothReady, isInviting = localIsInviting, sceneGeneration = sceneGeneration };
            try { SendPacket(pendingHostState.Serialize(), 0x02); } catch { }
        } else {
            var reply = new NetworkStateSync { sceneIndex = SceneManager.GetActiveScene().buildIndex, targetScene = SceneManager.GetActiveScene().buildIndex, levelReady = true, isInviting = false };
            try { SendPacket(reply.Serialize(), 0x02); } catch { }
            // Build and send client state with own edge detection
            try { SendPacket(BuildClientState().Serialize(), 0x01); } catch { }
            if (lastStateSync.sceneIndex == reply.sceneIndex) { if (!bothReady) { bothReady = true; statusText = "Both ready!"; } }
        }
    }

    private void HandleMouseVisibilityHotkey()
    {
        bool f1Now = Input.GetKey(KeyCode.F1);
        if (f1Now && !lastF1)
        {
            mouseOverrideVisible = !mouseOverrideVisible;
            Debug.Log("[NET] Mouse visibility override: " + (mouseOverrideVisible ? "ON" : "OFF"));
        }
        lastF1 = f1Now;

        if (mouseOverrideVisible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Cursor.visible)
        {
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        if (!mouseOverrideVisible) return;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnGUI()
    {
        if (Event.current != null && Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.F2) showUI = !showUI;
            else if (Event.current.keyCode == KeyCode.F3 && !isNetworkActive) StartHost();
            else if (Event.current.keyCode == KeyCode.F4 && !isNetworkActive) StartClient(ipInput);
            else if (Event.current.keyCode == KeyCode.F5 && isNetworkActive) Disconnect();
            else if (Event.current.keyCode == KeyCode.F6 && isNetworkActive)
            {
                if (isHost) { if (localIsInviting) { localIsInviting = false; statusText = "Cancelled."; } }
                else { if (lastStateSync.isInviting) { localClientAgreed = true; statusText = "Agreed!"; } }
            }
        }

        if (!showUI) return;

        // ── Dark panel ──
        Color oldBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.92f);
        GUI.Box(new Rect(8, 8, 384, 290), "");
        GUI.backgroundColor = oldBg;

        int x = 18, w = 360, y = 14;
        Color dim = new Color(0.55f, 0.55f, 0.6f);
        Color bright = new Color(0.9f, 0.9f, 0.95f);
        Color green = new Color(0.3f, 0.85f, 0.4f);
        Color yellow = new Color(1f, 0.85f, 0.2f);
        Color blue = new Color(0.3f, 0.6f, 1f);
        string role = isNetworkActive ? (isHost ? "房主" : "客户端") : "待机";

        // Title
        GUI.color = bright;
        GUI.Label(new Rect(x, y, w, 20), "◆ Ascendum 联机");
        GUI.color = dim;
        GUI.Label(new Rect(x + 120, y, 240, 20), "Build " + BuildInfo.CompiledAt);
        y += 18;

        // Status
        GUI.color = isNetworkActive ? (isHost ? green : blue) : dim;
        GUI.Label(new Rect(x, y, 16, 20), "●");
        GUI.color = bright;
        GUI.Label(new Rect(x + 18, y, w - 18, 20), role + (isNetworkActive ? "" : " (F3/F4 开始)"));
        y += 20;

        GUI.color = new Color(0.25f, 0.25f, 0.35f);
        GUI.Label(new Rect(x, y, w, 12), "──────────────────────────────────────");
        y += 10;

        if (isNetworkActive)
        {
            GUI.color = dim; GUI.Label(new Rect(x, y, 60, 18), "端口");
            GUI.color = bright; GUI.Label(new Rect(x + 60, y, w - 60, 18), port.ToString());
            y += 16;
            if (!isHost) {
                GUI.color = dim; GUI.Label(new Rect(x, y, 60, 18), "连接至");
                GUI.color = bright; GUI.Label(new Rect(x + 60, y, w - 60, 18), ipInput + ":" + port.ToString());
                y += 16;
            }
            y += 4;
            GUI.color = new Color(0.25f, 0.25f, 0.35f);
            GUI.Label(new Rect(x, y, w, 12), "──────────────────────────────────────");
            y += 12;

            int myScene = SceneManager.GetActiveScene().buildIndex;
            string myName = SceneManager.GetActiveScene().name ?? "";
            GUI.color = dim; GUI.Label(new Rect(x, y, w, 16), "场景同步");
            y += 16;
            GUI.color = green; GUI.Label(new Rect(x, y, 16, 18), "H");
            GUI.color = bright; GUI.Label(new Rect(x + 16, y, w - 16, 18), isHost ? (myName + " [" + myScene + "]") : ("[" + lastStateSync.sceneIndex + "]"));
            y += 16;
            GUI.color = blue; GUI.Label(new Rect(x, y, 16, 18), "S");
            GUI.color = bright; GUI.Label(new Rect(x + 16, y, w - 16, 18), isHost ? ("[" + lastStateSync.sceneIndex + "]") : (myName + " [" + myScene + "]"));
            y += 16;

            GUI.color = dim; GUI.Label(new Rect(x, y, w, 16), "同步 " + (bothReady ? "✓" : "…"));
            y += 16;
            GUI.color = bright;
            GUI.Label(new Rect(x, y, 170, 18), "状态包 " + debugStatePackets);
            GUI.Label(new Rect(x + 170, y, 170, 18), "输入包 " + debugInputPackets);
            y += 16;
            GUI.Label(new Rect(x, y, w, 18), "射击序列  H:" + (isHost ? holyShootSeq : lastStateSync.holyShootSeq) + "  S:" + (isHost ? shadowShootSeq : lastStateSync.shadowShootSeq));
            y += 18;

            GUI.color = new Color(0.25f, 0.25f, 0.35f);
            GUI.Label(new Rect(x, y, w, 12), "──────────────────────────────────────");
            y += 12;

            if (isHost && localIsInviting) {
                GUI.color = yellow;
                GUI.Label(new Rect(x, y, w, 18), "▶ 邀请中: " + (pendingSceneName ?? ""));
                y += 18;
                GUI.color = Color.white;
                if (GUI.Button(new Rect(x, y, 100, 22), "取消 (F6)")) { localIsInviting = false; statusText = "Cancelled."; }
            } else if (!isHost && lastStateSync.isInviting) {
                GUI.color = yellow;
                GUI.Label(new Rect(x, y, w, 18), "▶ 收到邀请!");
                y += 18;
                GUI.color = Color.white;
                if (localClientAgreed) GUI.Label(new Rect(x, y, w, 18), "  等待主机确认…");
                else if (GUI.Button(new Rect(x, y, 100, 22), "同意 (F6)")) { localClientAgreed = true; statusText = "Agreed!"; }
            } else {
                GUI.backgroundColor = new Color(0.7f, 0.2f, 0.2f);
                if (GUI.Button(new Rect(x, y, 140, 26), "断开连接 (F5)")) Disconnect();
                GUI.backgroundColor = Color.white;
            }
        }
        else
        {
            GUI.color = dim; GUI.Label(new Rect(x, y, 50, 20), "对方IP");
            GUI.Label(new Rect(x + 170, y, 40, 20), "端口");
            GUI.color = Color.white;
            GUI.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            ipInput = GUI.TextField(new Rect(x + 52, y, 112, 20), ipInput);
            portInput = GUI.TextField(new Rect(x + 200, y, 50, 20), portInput);
            GUI.backgroundColor = Color.white;
            y += 28;
            GUI.color = bright;
            if (GUI.Button(new Rect(x, y, w - 4, 30), "◆ 创建房间 (F3)")) StartHost();
            y += 34;
            GUI.color = bright;
            if (GUI.Button(new Rect(x, y, w - 4, 30), "◆ 加入房间 (F4)")) StartClient(ipInput);
        }
        GUI.color = Color.white;
    }

    public void StartHost()
    {
        isHost = true; isNetworkActive = true;
        if (!int.TryParse(portInput, out port) || port < 1 || port > 65535) { port = 17777; portInput = "17777"; }
        statusText = "Hosting...";
        holyShootSeq = 0;
        shadowShootSeq = 0;
        lastAppliedHolyShootSeq = 0;
        lastAppliedShadowShootSeq = 0;
        lastReceivedScene = -1;
        lastHolyShootPressed = false;
        lastShadowShootPressed = false;
        remoteInput = default(NetworkInputState);
        lastStateSync = default(NetworkStateSync);
        recvOffset = 0;
        pendingSceneName = null;
        localIsInviting = false;
        localClientAgreed = false;
        bothReady = false;
        _cachedHoly = null; _cachedShadow = null; _cachedPlayerScene = -1;
        _remoteShadowX = 0f; _remoteShadowY = 0f; _remoteShadowFacing = false;
        cachedHolyCarryingObject = false;
        cachedShadowCarryingObject = false;
        cachedCarryStateValid = false;
        cachedHolyCarryObjectName = "";
        cachedShadowCarryObjectName = "";
        carrySyncBlockedUntilFrame = -1;
        lastRemoteConfirmDown = false;
        lastHostLocalConfirmDown = false;
        lastConfirmLogKey = "";
        lastRxClientInputKey = "";
        lastRxStateSyncKey = "";
        lastAppliedHolyCarryKey = "";
        lastAppliedShadowCarryKey = "";
        lastAppliedCarryVisualType = int.MinValue;
        lastAppliedCarryVisualScene = -1;
        _hostJumpFrame = 0; _hostShootFrame = 0;
        _hostJumpStartEdge = false; _hostJumpReleaseEdge = false;
        _hostShootStartEdge = false; _hostShootReleaseEdge = false;
        _hostLastJumpHold = false; _hostLastShootHold = false;
        lastSceneIndex = SceneManager.GetActiveScene().buildIndex;
        sceneStabilizeFrames = 0;
        sceneGeneration = 0;
        try { if ((object)server != null) server.Stop(); } catch { }
        server = null; clientSocket = null; stream = null;
        try { server = new TcpListener(IPAddress.Any, port); server.Start(); }
        catch (Exception e) { statusText = "Error: " + e.Message; isNetworkActive = false; return; }
    }

    public void StartClient(string ip)
    {
        isHost = false; isNetworkActive = true;
        if (!int.TryParse(portInput, out port) || port < 1 || port > 65535) { port = 17777; portInput = "17777"; }
        statusText = "Connecting...";
        remoteInput = default(NetworkInputState);
        lastStateSync = default(NetworkStateSync);
        lastAppliedHolyShootSeq = 0;
        lastAppliedShadowShootSeq = 0;
        lastReceivedScene = -1;
        lastHolyShootPressed = false;
        lastShadowShootPressed = false;
        recvOffset = 0;
        localClientAgreed = false;
        _cachedHoly = null; _cachedShadow = null; _cachedPlayerScene = -1;
        _remoteShadowX = 0f; _remoteShadowY = 0f; _remoteShadowFacing = false;
        cachedHolyCarryingObject = false;
        cachedShadowCarryingObject = false;
        cachedCarryStateValid = false;
        cachedHolyCarryObjectName = "";
        cachedShadowCarryObjectName = "";
        carrySyncBlockedUntilFrame = -1;
        lastRemoteConfirmDown = false;
        lastHostLocalConfirmDown = false;
        lastConfirmLogKey = "";
        lastRxClientInputKey = "";
        lastRxStateSyncKey = "";
        lastAppliedHolyCarryKey = "";
        lastAppliedShadowCarryKey = "";
        lastAppliedCarryVisualType = int.MinValue;
        lastAppliedCarryVisualScene = -1;
        _hostJumpFrame = 0; _hostShootFrame = 0;
        _hostJumpStartEdge = false; _hostJumpReleaseEdge = false;
        _hostShootStartEdge = false; _hostShootReleaseEdge = false;
        _hostLastJumpHold = false; _hostLastShootHold = false;
        lastSceneIndex = SceneManager.GetActiveScene().buildIndex;
        sceneStabilizeFrames = 0;
        lastReceivedSceneGeneration = -1;
        connectSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        connectSocket.Blocking = false;
        try
        {
            connectSocket.Connect(ip, port);
            // Synchronous connect (localhost): use immediately, no polling needed
            connectSocket.Blocking = true;
            clientSocket = connectSocket;
            stream = new NetworkStream(clientSocket, true);
            connectSocket = null;
            statusText = "Connected!";
        }
        catch (SocketException ex)
        {
            if (ex.SocketErrorCode != SocketError.WouldBlock && ex.SocketErrorCode != SocketError.InProgress)
            { statusText = "Connect error: " + ex.Message; isNetworkActive = false; try { connectSocket.Close(); } catch { } connectSocket = null; }
        }
    }

    public void Disconnect()
    {
        isNetworkActive = false; isHost = false; bothReady = false;
        statusText = "Disconnected";
        remoteInput = default(NetworkInputState);
        lastStateSync = default(NetworkStateSync);
        pendingSceneName = null;
        localIsInviting = false;
        localClientAgreed = false;
        lastReceivedScene = -1;
        lastHolyShootPressed = false;
        lastShadowShootPressed = false;
        recvOffset = 0;
        cachedHolyCarryingObject = false;
        cachedShadowCarryingObject = false;
        cachedCarryStateValid = false;
        cachedHolyCarryObjectName = "";
        cachedShadowCarryObjectName = "";
        carrySyncBlockedUntilFrame = -1;
        lastRemoteConfirmDown = false;
        lastConfirmLogKey = "";
        lastRxClientInputKey = "";
        lastRxStateSyncKey = "";
        lastAppliedHolyCarryKey = "";
        lastAppliedShadowCarryKey = "";
        lastAppliedCarryVisualType = int.MinValue;
        lastAppliedCarryVisualScene = -1;
        try { if ((object)stream != null) stream.Close(); } catch { }
        try { if ((object)clientSocket != null) clientSocket.Close(); } catch { }
        try { if ((object)server != null) server.Stop(); } catch { }
        try { if ((object)connectSocket != null) connectSocket.Close(); } catch { }
        stream = null; clientSocket = null; server = null; connectSocket = null;
        System.GC.Collect();
    }

    private NetworkStateSync pendingHostState;
    private void BuildHostState()
    {
        pendingHostState = new NetworkStateSync { sceneIndex = SceneManager.GetActiveScene().buildIndex, targetScene = SceneManager.GetActiveScene().buildIndex, levelReady = bothReady, isInviting = localIsInviting, sceneGeneration = sceneGeneration };
        GameObject holy;
        GameObject shadow;
        if (TryGetPlayers(out holy, out shadow))
        {
            if (holy != null)
            {
                Vector3 p = holy.transform.position;
                pendingHostState.holyX = p.x;
                pendingHostState.holyY = p.y;
                pendingHostState.holyFacingRight = ReadFacingRight(holy, true);
            }
            if (shadow != null)
            {
                Vector3 p = shadow.transform.position;
                pendingHostState.shadowX = p.x;
                pendingHostState.shadowY = p.y;
                pendingHostState.shadowFacingRight = ReadFacingRight(shadow, true);
            }
            pendingHostState.holyCarryingObject = IsPlayerCarryingObject(holy);
            pendingHostState.holyCarryObjectName = GetCarryObjectName(holy);
            CacheCarryObjectName(true, pendingHostState.holyCarryObjectName);
            pendingHostState.shadowCarryingObject = IsPlayerCarryingObject(shadow);
            pendingHostState.shadowCarryObjectName = GetCarryObjectName(shadow);
            CacheCarryObjectName(false, pendingHostState.shadowCarryObjectName);
            CacheCarryState(pendingHostState.holyCarryingObject, pendingHostState.shadowCarryingObject, "host-state");
        }
        pendingHostState.holyShootSeq = holyShootSeq;
        pendingHostState.shadowShootSeq = shadowShootSeq;
        if (holyShootSeq != _debugLastHostHolyShootSeq || shadowShootSeq != _debugLastHostShadowShootSeq)
        {
            _debugLastHostHolyShootSeq = holyShootSeq;
            _debugLastHostShadowShootSeq = shadowShootSeq;
            Debug.Log("[NET] BuildHostState: holyShootSeq=" + holyShootSeq + " shadowShootSeq=" + shadowShootSeq);
        }
    }

    private static int _debugLogFrame;
    private static int _debugLastHostHolyShootSeq;
    private static int _debugLastHostShadowShootSeq;
    private void PollAndNotifyShootEvents()
    {
        GameObject holy;
        GameObject shadow;
        if (!TryGetPlayers(out holy, out shadow))
        {
            if (Time.frameCount != _debugLogFrame) { _debugLogFrame = Time.frameCount; Debug.Log("[NET] PollShoot: TryGetPlayers returned false"); }
            return;
        }
        // ── Holy detection (host) ──
        bool holyRefl = ReadShootPressed(0);
        bool holyKey = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool holyEdge = holyKey && !_holyShootFallbackDown;
        if (holyKey && Time.frameCount != _debugLogFrame)
        {
            _debugLogFrame = Time.frameCount;
            // Log CanShootNow details
            bool canShoot = holy != null ? CanShootNow(holy) : false;
            Debug.Log("[NET] PollShoot HOST: S key=" + holyKey + " refl=" + holyRefl + " edge=" + holyEdge + " canShoot=" + canShoot + " seq=" + holyShootSeq);
        }
        TryNotifyShootFor(holy, 0, ref lastHolyShootPressed);
        // Direct fallback: if S is pressed and edge detected, always increment.
        // The game's own PlayerController handles readyToAttack/knockedBack checks.
        if (isHost && holyEdge)
        {
            NotifyShoot(0);
            if (Time.frameCount != _debugLogFrame) Debug.Log("[NET] PollShoot HOST: FALLBACK NotifyShoot(0) → holyShootSeq=" + holyShootSeq);
        }
        _holyShootFallbackDown = holyKey;

        // ── Shadow detection ──
        TryNotifyShootFor(shadow, 1, ref lastShadowShootPressed);
    }

    private static void TryNotifyShootFor(GameObject player, int playerType, ref bool lastPressed)
    {
        bool pressed = ReadShootPressed(playerType);
        if (pressed && !lastPressed && CanShootNow(player))
        {
            NotifyShoot(playerType);
        }
        lastPressed = pressed;
    }

    private int lastReceivedScene = -1;
    private void ApplyReceivedState()
    {
        if (lastStateSync.isInviting) return;
        int sceneIdx = lastStateSync.targetScene;
        bool sameSceneReload = (sceneIdx == lastReceivedScene && lastStateSync.sceneGeneration > lastReceivedSceneGeneration);
        if (sceneIdx != lastReceivedScene || sameSceneReload) {
            lastReceivedScene = sceneIdx;
            lastReceivedSceneGeneration = lastStateSync.sceneGeneration;
              if (SceneManager.GetActiveScene().buildIndex != sceneIdx || sameSceneReload) {
                  isActuallyLoading = true;
                  SceneManager.LoadScene(sceneIdx);
                  isActuallyLoading = false;
                  lastAppliedCarryVisualType = int.MinValue;
                  lastAppliedCarryVisualScene = -1;
                  return; // Scene is unloading — do not access player objects this frame
              }
          }
        if (SceneManager.GetActiveScene().buildIndex == lastStateSync.sceneIndex)
        {
            ApplyPlayerState();
            GameObject holy;
            GameObject shadow;
            if (TryGetPlayers(out holy, out shadow))
            {
                bool localHolyCarrying;
                bool localShadowCarrying;
                TryGetLocalCarryState(out localHolyCarrying, out localShadowCarrying);

                if (!(localHolyCarrying && !lastStateSync.holyCarryingObject))
                    ApplyCarryObjectState(holy, lastStateSync.holyCarryObjectName, lastStateSync.holyCarryingObject, "holy-state");
                else
                    Debug.Log("[NET] Carry skip holy-state: local=true remote=false frame=" + Time.frameCount);

                if (!(localShadowCarrying && !lastStateSync.shadowCarryingObject))
                    ApplyCarryObjectState(shadow, lastStateSync.shadowCarryObjectName, lastStateSync.shadowCarryingObject, "shadow-state");
                else
                    Debug.Log("[NET] Carry skip shadow-state: local=true remote=false frame=" + Time.frameCount);
            }
        }
    }

    private static void TryApplyShootEventsFromLatestState()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool isGameScene = !string.IsNullOrEmpty(sceneName)
            && sceneName.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) < 0
            && sceneName.IndexOf("Logo", StringComparison.OrdinalIgnoreCase) < 0
            && sceneName.IndexOf("Start", StringComparison.OrdinalIgnoreCase) < 0
            && sceneName.IndexOf("Loading", StringComparison.OrdinalIgnoreCase) < 0
            && sceneName.IndexOf("Credits", StringComparison.OrdinalIgnoreCase) < 0
            && sceneName.IndexOf("Finish", StringComparison.OrdinalIgnoreCase) < 0;
        if (!isGameScene) return;
        ApplyShootEvents();
    }

    public static void NotifyShoot(int playerType)
    {
        if (!IsHostPeer) return;
        if (playerType == 0) holyShootSeq++;
        else if (playerType == 1) shadowShootSeq++;
    }

    private static void ApplyPlayerState()
    {
        GameObject holy;
        GameObject shadow;
        if (!TryGetPlayers(out holy, out shadow)) return;
        // Client only applies Holy position (host-authoritative).
        // Shadow is simulated locally on client — do NOT overwrite from host state.
        if (holy != null && !IsHostPeer)
        {
            ApplyTransformState(holy, lastStateSync.holyX, lastStateSync.holyY, lastStateSync.holyFacingRight);
        }
        // Host applies Shadow position from client state (client-authoritative).
        // Holy is simulated locally on host — do NOT overwrite from remote.
        if (shadow != null && IsHostPeer)
        {
            ApplyTransformState(shadow, _remoteShadowX, _remoteShadowY, _remoteShadowFacing);
        }
    }

    // Apply client-reported Shadow position on host GameObject
    private static void ApplyRemoteShadowPosition()
    {
        GameObject holy;
        GameObject shadow;
        if (!TryGetPlayers(out holy, out shadow)) return;
        if (shadow != null)
        {
            Transform t = shadow.transform;
            t.position = new Vector3(_remoteShadowX, _remoteShadowY, t.position.z);
            WriteFacingRight(shadow, _remoteShadowFacing);
        }
    }

    private static void ApplyTransformState(GameObject go, float x, float y, bool facingRight)
    {
        if (go == null) return; // Unity overloaded == catches destroyed objects; (object) cast does not
        Transform t = go.transform;
        t.position = new Vector3(x, y, t.position.z);
        WriteFacingRight(go, facingRight);
    }

    private static int _debugClientFrame;
    private static void ApplyShootEvents()
    {
        // Always update seq even if players not found — avoids backlog when
        // LevelManager isn't ready yet during scene transitions.
        GameObject holy;
        GameObject shadow;
        TryGetPlayers(out holy, out shadow);
        if (lastStateSync.holyShootSeq > lastAppliedHolyShootSeq)
        {
            if (Time.frameCount != _debugClientFrame) { _debugClientFrame = Time.frameCount; Debug.Log("[NET] Client ApplyShoot: Holy seq " + lastAppliedHolyShootSeq + " → " + lastStateSync.holyShootSeq + " holy=" + (holy != null ? "OK" : "null")); }
            if (holy != null && SpawnProjectileFor(holy))
                lastAppliedHolyShootSeq = lastStateSync.holyShootSeq;
        }
        if (lastStateSync.shadowShootSeq > lastAppliedShadowShootSeq)
        {
            if (Time.frameCount != _debugClientFrame) { _debugClientFrame = Time.frameCount; Debug.Log("[NET] Client ApplyShoot: Shadow seq " + lastAppliedShadowShootSeq + " → " + lastStateSync.shadowShootSeq); }
            if (shadow != null && SpawnProjectileFor(shadow))
                lastAppliedShadowShootSeq = lastStateSync.shadowShootSeq;
        }
    }

    private static int _spawnDebugFrame;
    private static bool SpawnProjectileFor(GameObject player)
    {
        try
        {
            LogSpawnDebug("begin player=" + (player != null ? player.name : "null"));
            if (player == null) { LogSpawnDebug("player=null"); return false; }
            EnsureReflectionCache();
            if ((object)playerControllerType == null) { LogSpawnDebug("playerControllerType=null"); return false; }
            if ((object)playerProjectileField == null) { LogSpawnDebug("playerProjectileField=null"); return false; }
            if ((object)playerArmPosField == null) { LogSpawnDebug("playerArmPosField=null"); return false; }
            Component controller = FindPlayerController(player);
            if (controller == null) { LogSpawnDebug("controller=null player=" + player.name); return false; }
            GameObject prefab = playerProjectileField.GetValue(controller) as GameObject;
            if (prefab == null) { LogSpawnDebug("prefab=null player=" + player.name); return false; }
            Transform arm = playerArmPosField.GetValue(controller) as Transform;
            if (arm == null) { LogSpawnDebug("arm=null player=" + player.name); return false; }
            if ((object)playerShootAnimMethod != null)
            {
                try { playerShootAnimMethod.Invoke(controller, null); } catch { }
            }
            GameObject projectile = UnityEngine.Object.Instantiate(prefab, arm.position, arm.rotation);
            if (projectile == null) { LogSpawnDebug("instantiate=null"); return false; }
            if ((object)projectileSetIgnoreObjMethod != null && (object)playerIgnoreWhenShootingField != null)
            {
                GameObject ignore = playerIgnoreWhenShootingField.GetValue(controller) as GameObject;
                if (ignore != null)
                {
                    Component projectileComponent = projectile.GetComponent(projectileType);
                    if (projectileComponent != null) projectileSetIgnoreObjMethod.Invoke(projectileComponent, new object[] { ignore });
                }
            }
            LogSpawnDebug("spawned player=" + player.name + " prefab=" + prefab.name);
            return true;
        }
        catch (Exception ex)
        {
            LogSpawnDebug("exception=" + ex.GetType().Name + " " + ex.Message);
            return false;
        }
    }

    private static Component FindPlayerController(GameObject player)
    {
        if (player == null || (object)playerControllerType == null) return null;
        Component c = player.GetComponent(playerControllerType);
        if (c != null) return c;
        c = player.GetComponentInChildren(playerControllerType);
        if (c != null) return c;
        return player.GetComponentInParent(playerControllerType);
    }

    private static void LogSpawnDebug(string msg)
    {
        _spawnDebugFrame = Time.frameCount;
        Debug.Log("[NET] SpawnProj: " + msg);
    }

    private static bool TryGetPlayers(out GameObject holy, out GameObject shadow)
    {
        int curScene = SceneManager.GetActiveScene().buildIndex;
        // Return cached references if still valid (same scene, not destroyed)
        if (curScene == _cachedPlayerScene && _cachedHoly != null && _cachedShadow != null)
        {
            holy = _cachedHoly;
            shadow = _cachedShadow;
            return true;
        }
        // Cache miss — do the reflection lookup and cache results
        holy = null;
        shadow = null;
        object manager = GetLevelManager();
        if ((UnityEngine.Object)manager == null) return false;
        EnsureReflectionCache();
        if ((object)levelManagerHolyField != null)
            holy = levelManagerHolyField.GetValue(manager) as GameObject;
        if ((object)levelManagerShadowField != null)
            shadow = levelManagerShadowField.GetValue(manager) as GameObject;
        if (holy != null || shadow != null)
        {
            if (holy != null) _cachedHoly = holy;
            if (shadow != null) _cachedShadow = shadow;
            _cachedPlayerScene = curScene;
        }
        return holy != null || shadow != null;
    }

    private static object GetLevelManager()
    {
        EnsureReflectionCache();
        // 1) Find by tag "GameController" — the GameObject that holds LevelManager
        GameObject go = GameObject.FindGameObjectWithTag("GameController");
        if (go != null)
        {
            if ((object)levelManagerType != null)
            {
                Component lm = go.GetComponent(levelManagerType);
                if (lm != null) return lm;
            }
        }
        // 2) FindObjectOfType fallback
        if ((object)levelManagerType != null)
        {
            object found = UnityEngine.Object.FindObjectOfType(levelManagerType);
            if (found != null) return found;
        }
        // 3) Static field fallback
        if ((object)levelManagerCurrentField != null)
        {
            object cur = levelManagerCurrentField.GetValue(null);
            if ((UnityEngine.Object)cur != null) return cur;
        }
        return null;
    }

    private static void EnsureReflectionCache()
    {
        if ((object)levelManagerType == null)
        {
            levelManagerType = Type.GetType("LevelManager, Assembly-CSharp");
            if ((object)levelManagerType != null)
            {
                levelManagerCurrentField = levelManagerType.GetField("current", BindingFlags.Public | BindingFlags.Static);
                levelManagerHolyField = levelManagerType.GetField("inGamePlayerHoly", BindingFlags.Public | BindingFlags.Instance);
                levelManagerShadowField = levelManagerType.GetField("inGamePlayerShadow", BindingFlags.Public | BindingFlags.Instance);
            }
        }
        if ((object)playerControllerType == null)
        {
            playerControllerType = Type.GetType("PlayerController, Assembly-CSharp");
            if ((object)playerControllerType != null)
            {
                playerFacingRightField = playerControllerType.GetField("facingRight", BindingFlags.NonPublic | BindingFlags.Instance);
                playerProjectileField = playerControllerType.GetField("projectile", BindingFlags.Public | BindingFlags.Instance);
                playerArmPosField = playerControllerType.GetField("armPos", BindingFlags.Public | BindingFlags.Instance);
                playerIgnoreWhenShootingField = playerControllerType.GetField("ignoreWhenShooting", BindingFlags.NonPublic | BindingFlags.Instance);
                playerInsideHoldAreaField = playerControllerType.GetField("insideHoldArea", BindingFlags.NonPublic | BindingFlags.Instance);
                playerReadyToAttackField = playerControllerType.GetField("readyToAttack", BindingFlags.NonPublic | BindingFlags.Instance);
                playerKnockedBackField = playerControllerType.GetField("knockedBack", BindingFlags.NonPublic | BindingFlags.Instance);
                playerFaceDirMethod = playerControllerType.GetMethod("FaceDir", BindingFlags.NonPublic | BindingFlags.Instance);
                playerShootAnimMethod = playerControllerType.GetMethod("DoProperShootAnim", BindingFlags.NonPublic | BindingFlags.Instance);
                playerIsCarryingObjectMethod = playerControllerType.GetMethod("IsPlayerCarryingObject", BindingFlags.Public | BindingFlags.Instance);
            }
        }
        if ((object)inputShootHolyMethod == null)
        {
            Type inputType = Type.GetType("InputManager, Assembly-CSharp");
            if ((object)inputType != null)
            {
                inputShootMethod = inputType.GetMethod("Shoot", BindingFlags.Public | BindingFlags.Static);
                inputShootHolyMethod = inputType.GetMethod("Shoot_Holy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                inputShootShadowMethod = inputType.GetMethod("Shoot_Shadow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            }
        }
        if ((object)inputPlayerTypeEnum == null)
        {
            inputPlayerTypeEnum = Type.GetType("PlayerType, Assembly-CSharp");
        }
        if ((object)pickUpObjectType == null)
        {
            pickUpObjectType = Type.GetType("PickUpObject, Assembly-CSharp");
            if ((object)pickUpObjectType != null)
            {
                pickUpObjectOnCarrierChangedMethod = pickUpObjectType.GetMethod("OnCarrierChanged", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                pickUpObjectOnItemPickedUpMethod = pickUpObjectType.GetMethod("OnItemPickedUp", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                pickUpObjectObjectDroppedMethod = pickUpObjectType.GetMethod("ObjectDropped", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                pickUpObjectSwitchCarrierMethod = pickUpObjectType.GetMethod("SwitchCarrier", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                pickUpObjectPickedUpField = pickUpObjectType.GetField("pickedUp", BindingFlags.NonPublic | BindingFlags.Instance);
                pickUpObjectPlayerWhoCarriesField = pickUpObjectType.GetField("playerWhoCarries", BindingFlags.NonPublic | BindingFlags.Instance);
                pickUpObjectCollField = pickUpObjectType.GetField("coll", BindingFlags.Public | BindingFlags.Instance);
            }
        }
        if ((object)playerControllerType != null && (object)playerGetCarryObjectMethod == null)
        {
            playerGetCarryObjectMethod = playerControllerType.GetMethod("GetCarryObject", BindingFlags.Public | BindingFlags.Instance);
        }
        if ((object)goldObjectType == null)
        {
            goldObjectType = Type.GetType("GoldObject, Assembly-CSharp");
            if ((object)goldObjectType != null)
            {
                goldObjectSpawnFormMethod = goldObjectType.GetMethod("SpawnForm", BindingFlags.Public | BindingFlags.Instance);
            }
        }
        if ((object)goldSphereControllerType == null)
        {
            goldSphereControllerType = Type.GetType("GoldSpehereController, Assembly-CSharp");
            if ((object)goldSphereControllerType != null)
            {
                goldSphereChangeFormMethod = goldSphereControllerType.GetMethod("ChangeForm", BindingFlags.Public | BindingFlags.Instance);
            }
        }
        if ((object)projectileType == null)
        {
            projectileType = Type.GetType("Projectile, Assembly-CSharp");
            if ((object)projectileType != null)
            {
                projectileSetIgnoreObjMethod = projectileType.GetMethod("SetIgnoreObj", BindingFlags.Public | BindingFlags.Instance);
            }
        }
    }

    private static bool CanShootNow(GameObject player)
    {
        if (player == null) return false;
        EnsureReflectionCache();
        if ((object)playerControllerType == null) return false;
        Component c = player.GetComponent(playerControllerType);
        if (c == null) return false;
        if ((object)playerInsideHoldAreaField != null && !(bool)playerInsideHoldAreaField.GetValue(c)) return false;
        if ((object)playerReadyToAttackField != null && !(bool)playerReadyToAttackField.GetValue(c)) return false;
        if ((object)playerKnockedBackField != null && (bool)playerKnockedBackField.GetValue(c)) return false;
        return true;
    }

    private static bool ReadShootPressed(int playerType)
    {
        EnsureReflectionCache();
        // 1) Call IL-patched Shoot_Holy / Shoot_Shadow directly (static, no args)
        MethodInfo m = (playerType == 0) ? inputShootHolyMethod : inputShootShadowMethod;
        if ((object)m != null)
        {
            try { object r = m.Invoke(null, null); if (r is bool && (bool)r) return true; if (r is int && (int)r != 0) return true; } catch { }
        }
        // 2) Fallback: InputManager.Shoot(PlayerType) dispatcher
        if ((object)inputShootMethod != null)
        {
            object arg = (object)inputPlayerTypeEnum != null ? Enum.ToObject(inputPlayerTypeEnum, playerType) : (object)playerType;
            try { object r = inputShootMethod.Invoke(null, new object[] { arg }); if (r is bool && (bool)r) return true; if (r is int && (int)r != 0) return true; } catch { }
        }
        // 3) Direct Rewired call bypassing all IL patches — last resort
        try
        {
            Type inputType = Type.GetType("InputManager, Assembly-CSharp");
            if ((object)inputType != null)
            {
                FieldInfo mainField = inputType.GetField("main", BindingFlags.Public | BindingFlags.Static);
                if ((object)mainField != null)
                {
                    object main = mainField.GetValue(null);
                    if (main != null)
                    {
                        MethodInfo getPlayer = inputType.GetMethod("GetPlayerInput");
                        if ((object)getPlayer != null)
                        {
                            object player = getPlayer.Invoke(main, new object[] { playerType == 0 });
                            if (player != null)
                            {
                                string action = (playerType == 0) ? "ShootHoly" : "ShootShadow";
                                MethodInfo getButtonDown = player.GetType().GetMethod("GetButtonDown");
                                if ((object)getButtonDown != null)
                                {
                                    object r = getButtonDown.Invoke(player, new object[] { action });
                                    if (r is bool && (bool)r) return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch { }
        return false;
    }

    private static bool ReadFacingRight(GameObject go, bool fallback)
    {
        if (go == null) return fallback;
        EnsureReflectionCache();
        if ((object)playerControllerType == null || (object)playerFacingRightField == null) return fallback;
        Component c = go.GetComponent(playerControllerType);
        if (c == null) return fallback;
        object value = playerFacingRightField.GetValue(c);
        return value is bool ? (bool)value : fallback;
    }

    private static void WriteFacingRight(GameObject go, bool facingRight)
    {
        if (go == null) return;
        EnsureReflectionCache();
        if ((object)playerControllerType == null || (object)playerFacingRightField == null) return;
        Component c = go.GetComponent(playerControllerType);
        if (c == null) return;
        bool current = ReadFacingRight(go, facingRight);
        if (current != facingRight && (object)playerFaceDirMethod != null)
        {
            playerFaceDirMethod.Invoke(c, new object[] { facingRight });
        }
        else
        {
            playerFacingRightField.SetValue(c, facingRight);
        }
    }

    private void SendPacket(byte[] data, byte type)
    {
        if ((object)stream == null || (object)clientSocket == null || !clientSocket.Connected) return;
        int totalLen = data.Length + 3;
        if (sendBuffer == null || sendBuffer.Length < totalLen) sendBuffer = new byte[totalLen + 64];
        sendBuffer[0] = type;
        int len = data.Length;
        sendBuffer[1] = (byte)(len & 0xFF);
        sendBuffer[2] = (byte)((len >> 8) & 0xFF);
        Array.Copy(data, 0, sendBuffer, 3, data.Length);
        stream.Write(sendBuffer, 0, totalLen);
    }

    private byte[] recvBuffer = new byte[4096];
    private int recvOffset = 0;
    private byte[] sendBuffer;
    private byte[] packetDataBuffer;
    private ReceivedPacket? TryReadPacket()
    {
        if ((object)stream == null || (object)clientSocket == null || !clientSocket.Connected) return null;
        try {
            if (stream.DataAvailable) { int available = stream.Read(recvBuffer, recvOffset, recvBuffer.Length - recvOffset); if (available > 0) recvOffset += available; }
            if (recvOffset < 3) return null;
            byte type = recvBuffer[0];
            int len = recvBuffer[1] | (recvBuffer[2] << 8);
            if (len <= 0 || len > 1024) { recvOffset = 0; return null; }
            if (recvOffset < 3 + len) return null;
            if (packetDataBuffer == null || packetDataBuffer.Length < len) packetDataBuffer = new byte[len + 64];
            Array.Copy(recvBuffer, 3, packetDataBuffer, 0, len);
            int remaining = recvOffset - (3 + len);
            if (remaining > 0) Array.Copy(recvBuffer, 3 + len, recvBuffer, 0, remaining);
            recvOffset = remaining;
            return new ReceivedPacket { type = type, data = packetDataBuffer };
        } catch { return null; }
    }

    public static void CaptureLocalInput() {
        localShadowMove = ReadShadowMove();
        localShadowJump = ReadShadowJumpStart();
        localShadowJumpHold = ReadShadowJumpHold();
        localShadowJumpRelease = ReadShadowJumpRelease();
        localShadowShoot = ReadShadowShoot();
        localShadowShootHold = ReadShadowShootHold();
        localShadowShootRelease = ReadShadowShootRelease();
        localShadowConfirm = ReadShadowConfirm();
    }

    private static float ReadShadowMove()
    {
        float move = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += 1f;
        return move;
    }

    // Shadow input: frame-preserving edge detection.
    // Edge is detected once and held for the entire frame so multiple
    // calls from the game's InputManager see the same value.
    private static bool ReadShadowJumpStart()
    {
        SampleShadowJump();
        return _shadowJumpStartEdge;
    }
    private static bool ReadShadowJumpHold()  { SampleShadowJump(); return _shadowJumpDown; }
    private static bool ReadShadowJumpRelease()
    {
        SampleShadowJump();
        return _shadowJumpReleaseEdge;
    }

    private static void SampleShadowJump()
    {
        if (Time.frameCount == _shadowJumpFrame) return;
        bool prev = _shadowJumpDown;
        bool now = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        _shadowJumpFrame = Time.frameCount;
        _shadowJumpStartEdge = now && !prev;
        _shadowJumpReleaseEdge = !now && prev;
        _shadowJumpDown = now;
    }

    private static bool ReadShadowShoot()
    {
        if (Time.frameCount != _shadowShootFrame) { _shadowShootFrame = Time.frameCount; _shadowShootStartEdge = false; _shadowShootReleaseEdge = false; }
        bool now = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        if (now && !_shadowShootDown) _shadowShootStartEdge = true;
        _shadowShootDown = now;
        return _shadowShootStartEdge;
    }
    private static bool ReadShadowShootHold()  { return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow); }
    private static bool ReadShadowShootRelease()
    {
        if (Time.frameCount != _shadowShootFrame) { _shadowShootFrame = Time.frameCount; _shadowShootStartEdge = false; _shadowShootReleaseEdge = false; }
        bool now = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        if (!now && _shadowShootDown) _shadowShootReleaseEdge = true;
        _shadowShootDown = now;
        return _shadowShootReleaseEdge;
    }

    // Shadow confirm / interact: Space hold.
    private static bool ReadShadowConfirm()
    {
        return Input.GetKey(KeyCode.Space);
    }

    public static float GetShadowMove() { return IsHostPeer ? remoteInput.move : ReadShadowMove(); }
    // Host-side: frame-preserving edge detection from client's raw hold state.
    // Edge is detected once and held for the entire frame so multiple calls
    // (game InputManager + PollAndNotifyShootEvents) see the same value.
    public static bool GetShadowJumpStart() {
        if (IsHostPeer) {
            SampleHostRemoteJump();
            return _hostJumpStartEdge;
        }
        return ReadShadowJumpStart();
    }
    public static bool GetShadowJumpHold() { if (IsHostPeer) { SampleHostRemoteJump(); return _hostLastJumpHold; } return ReadShadowJumpHold(); }
    public static bool GetShadowJumpRelease() {
        if (IsHostPeer) {
            SampleHostRemoteJump();
            return _hostJumpReleaseEdge;
        }
        return ReadShadowJumpRelease();
    }

    private static void SampleHostRemoteJump()
    {
        if (Time.frameCount == _hostJumpFrame) return;
        bool prev = _hostLastJumpHold;
        bool now = remoteInput.jumpHold;
        _hostJumpFrame = Time.frameCount;
        _hostJumpStartEdge = now && !prev;
        _hostJumpReleaseEdge = !now && prev;
        _hostLastJumpHold = now;
    }
    public static bool GetShadowShoot() {
        if (IsHostPeer) {
            if (Time.frameCount != _hostShootFrame) { _hostShootFrame = Time.frameCount; _hostShootStartEdge = false; _hostShootReleaseEdge = false; }
            if (remoteInput.shootHold && !_hostLastShootHold) _hostShootStartEdge = true;
            _hostLastShootHold = remoteInput.shootHold;
            return _hostShootStartEdge;
        }
        return ReadShadowShoot();
    }
    public static bool GetShadowShootHold() { return IsHostPeer ? remoteInput.shootHold : ReadShadowShootHold(); }
    public static bool GetShadowShootRelease() {
        if (IsHostPeer) {
            if (Time.frameCount != _hostShootFrame) { _hostShootFrame = Time.frameCount; _hostShootStartEdge = false; _hostShootReleaseEdge = false; }
            if (!remoteInput.shootHold && _hostLastShootHold) _hostShootReleaseEdge = true;
            _hostLastShootHold = remoteInput.shootHold;
            return _hostShootReleaseEdge;
        }
        return ReadShadowShootRelease();
    }
    public static bool GetShadowConfirm()
    {
        bool confirm = IsHostPeer ? remoteInput.confirm : ReadShadowConfirm();
        if (!confirm) return false;
        bool allowed = CanConfirmForCarrier(false);
        LogConfirmDebug("input", false, confirm, allowed);
        return allowed;
    }

    // ── Holy WASD readers (mirror Shadow, used by IL patches on host) ──
    // Edge-preserving pattern: edge is detected once and held for the entire frame,
    // so multiple calls (game InputManager + PollAndNotifyShootEvents) see the same value.
    private static bool _holyJumpDown, _holyShootDown;
    private static int _holyJumpFrame, _holyShootFrame;
    private static bool _holyJumpStartEdge, _holyJumpReleaseEdge;
    private static bool _holyShootStartEdge, _holyShootReleaseEdge;
    private static float _holyMoveCached;
    private static int _holyMoveFrame;

    private static float ReadHolyMove()
    {
        if (Time.frameCount == _holyMoveFrame) return _holyMoveCached;
        _holyMoveFrame = Time.frameCount;
        _holyMoveCached = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) _holyMoveCached -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) _holyMoveCached += 1f;
        return _holyMoveCached;
    }
    private static bool ReadHolyJumpStart()
    {
        SampleHolyJump();
        return _holyJumpStartEdge;
    }
    private static bool ReadHolyJumpHold()  { SampleHolyJump(); return _holyJumpDown; }
    private static bool ReadHolyJumpRelease()
    {
        SampleHolyJump();
        return _holyJumpReleaseEdge;
    }

    private static void SampleHolyJump()
    {
        if (Time.frameCount == _holyJumpFrame) return;
        bool prev = _holyJumpDown;
        bool now = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        _holyJumpFrame = Time.frameCount;
        _holyJumpStartEdge = now && !prev;
        _holyJumpReleaseEdge = !now && prev;
        _holyJumpDown = now;
    }
    private static bool ReadHolyShoot()
    {
        if (Time.frameCount != _holyShootFrame) { _holyShootFrame = Time.frameCount; _holyShootStartEdge = false; _holyShootReleaseEdge = false; }
        bool now = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        if (now && !_holyShootDown) _holyShootStartEdge = true;
        _holyShootDown = now;
        return _holyShootStartEdge;
    }
    private static bool ReadHolyShootHold()  { return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow); }
    private static bool ReadHolyShootRelease()
    {
        if (Time.frameCount != _holyShootFrame) { _holyShootFrame = Time.frameCount; _holyShootStartEdge = false; _holyShootReleaseEdge = false; }
        bool now = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        if (!now && _holyShootDown) _holyShootReleaseEdge = true;
        _holyShootDown = now;
        return _holyShootReleaseEdge;
    }
    private static bool ReadHolyConfirm() { return Input.GetKey(KeyCode.Space); }

    public static float GetHolyMove()         { return IsHostPeer ? ReadHolyMove() : 0f; }
    public static bool GetHolyJumpStart()     { return IsHostPeer ? ReadHolyJumpStart() : false; }
    public static bool GetHolyJumpHold()      { return IsHostPeer ? ReadHolyJumpHold() : false; }
    public static bool GetHolyJumpRelease()   { return IsHostPeer ? ReadHolyJumpRelease() : false; }
    public static bool GetHolyShoot()         { return IsHostPeer ? ReadHolyShoot() : false; }
    public static bool GetHolyShootHold()     { return IsHostPeer ? ReadHolyShootHold() : false; }
    public static bool GetHolyShootRelease()  { return IsHostPeer ? ReadHolyShootRelease() : false; }
    public static bool GetHolyConfirm()
    {
        bool confirm = IsHostPeer && ReadHolyConfirm();
        if (!confirm) return false;
        bool allowed = CanConfirmForCarrier(true);
        LogConfirmDebug("input", true, confirm, allowed);
        return allowed;
    }

    private static bool CanConfirmForCarrier(bool holy)
    {
        bool holyCarrying;
        bool shadowCarrying;
        if (!TryGetConfirmCarryState(out holyCarrying, out shadowCarrying)) return true;
        if (!holyCarrying && !shadowCarrying) return true;
        return holy ? holyCarrying : shadowCarrying;
    }

    private static bool TryGetConfirmCarryState(out bool holyCarrying, out bool shadowCarrying)
    {
        bool localHolyCarrying;
        bool localShadowCarrying;
        bool haveSynced = TryGetSyncedCarryState(out holyCarrying, out shadowCarrying);
        bool haveLocal = TryGetLocalCarryState(out localHolyCarrying, out localShadowCarrying);

        if (!IsHostPeer)
        {
            if (haveLocal)
            {
                holyCarrying = localHolyCarrying;
                shadowCarrying = localShadowCarrying;
                return true;
            }
            if (haveSynced)
            {
                return true;
            }
            holyCarrying = false;
            shadowCarrying = false;
            return false;
        }

        if (haveSynced)
        {
            return true;
        }

        if (haveLocal)
        {
            holyCarrying = localHolyCarrying;
            shadowCarrying = localShadowCarrying;
            return true;
        }
        holyCarrying = false;
        shadowCarrying = false;
        return false;
    }

    private static bool TryGetLocalCarryState(out bool holyCarrying, out bool shadowCarrying)
    {
        GameObject holyPlayer;
        GameObject shadowPlayer;
        if (!TryGetPlayers(out holyPlayer, out shadowPlayer))
        {
            holyCarrying = false;
            shadowCarrying = false;
            return false;
        }
        holyCarrying = IsPlayerCarryingObject(holyPlayer);
        shadowCarrying = IsPlayerCarryingObject(shadowPlayer);
        return true;
    }

    private static bool TryGetSyncedCarryState(out bool holyCarrying, out bool shadowCarrying)
    {
        if (cachedCarryStateValid)
        {
            holyCarrying = cachedHolyCarryingObject;
            shadowCarrying = cachedShadowCarryingObject;
            return true;
        }
        holyCarrying = false;
        shadowCarrying = false;
        return false;
    }

    private static void CacheCarryState(bool holyCarrying, bool shadowCarrying, string source)
    {
        bool changed = !cachedCarryStateValid || cachedHolyCarryingObject != holyCarrying || cachedShadowCarryingObject != shadowCarrying;
        cachedHolyCarryingObject = holyCarrying;
        cachedShadowCarryingObject = shadowCarrying;
        cachedCarryStateValid = true;
        if (changed) LogCarryDebug(source, holyCarrying, shadowCarrying);
    }

    private static void LogCarryDebug(string source, bool holyCarrying, bool shadowCarrying)
    {
        Debug.Log("[NET] Carry " + source + ": H=" + holyCarrying + " S=" + shadowCarrying + " frame=" + Time.frameCount);
    }

    private static void LogConfirmDebug(string source, bool holyContext, bool rawConfirm, bool allowed)
    {
        string key = source + "|" + holyContext + "|" + rawConfirm + "|" + allowed + "|" + cachedHolyCarryingObject + "|" + cachedShadowCarryingObject;
        if (key == lastConfirmLogKey) return;
        lastConfirmLogKey = key;
        string message = "[NET] Space " + source + " role=" + (holyContext ? "Holy" : "Shadow") + " raw=" + rawConfirm + " allowed=" + allowed + " carry[H=" + cachedHolyCarryingObject + ",S=" + cachedShadowCarryingObject + "] frame=" + Time.frameCount;
        Debug.Log(message);
    }

    private static void CacheCarryObjectName(bool holySide, string objectName)
    {
        if (holySide)
        {
            cachedHolyCarryObjectName = objectName ?? "";
        }
        else
        {
            cachedShadowCarryObjectName = objectName ?? "";
        }
    }

    private static string GetCarryObjectName(GameObject player)
    {
        if (player == null) return "";
        EnsureReflectionCache();
        if ((object)playerGetCarryObjectMethod == null) return "";
        Component controller = FindPlayerController(player);
        if (controller == null) return "";
        try
        {
            object result = playerGetCarryObjectMethod.Invoke(controller, null);
            Component carry = result as Component;
            if ((object)carry != null) return carry.name;
        }
        catch { }
        return "";
    }

    private static bool TryResolvePickUpObject(string objectName, out Component pickUpObject)
    {
        pickUpObject = null;
        if (string.IsNullOrEmpty(objectName) || (object)pickUpObjectType == null) return false;
        UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(pickUpObjectType);
        if (objects == null) return false;
        for (int i = 0; i < objects.Length; i++)
        {
            Component candidate = objects[i] as Component;
            if ((object)candidate == null) continue;
            if (!candidate.gameObject.scene.IsValid()) continue;
            if (candidate.name == objectName)
            {
                pickUpObject = candidate;
                return true;
            }
        }
        return false;
    }

    private static bool TryGetCurrentCarryObject(GameObject player, out Component pickUpObject)
    {
        pickUpObject = null;
        if (player == null) return false;
        EnsureReflectionCache();
        if ((object)playerGetCarryObjectMethod == null) return false;
        Component controller = FindPlayerController(player);
        if (controller == null) return false;
        try
        {
            object result = playerGetCarryObjectMethod.Invoke(controller, null);
            pickUpObject = result as Component;
            return (object)pickUpObject != null;
        }
        catch { return false; }
    }

    private static void TryProcessRemoteCarryTransfer(GameObject holy, GameObject shadow)
    {
        if (!IsHostPeer) return;
        bool confirmDown = remoteInput.confirm;
        if (!confirmDown)
        {
            lastRemoteConfirmDown = false;
            return;
        }

        bool hasCarryPayload = remoteInput.holyCarryingObject || remoteInput.shadowCarryingObject || !string.IsNullOrEmpty(remoteInput.holyCarryObjectName) || !string.IsNullOrEmpty(remoteInput.shadowCarryObjectName);
        if (!hasCarryPayload) return;

        bool confirmEdge = !lastRemoteConfirmDown;
        if (!confirmEdge) return;

        if ((object)pickUpObjectSwitchCarrierMethod == null) return;

        Component holyController = FindPlayerController(holy);
        Component shadowController = FindPlayerController(shadow);
        if ((object)holyController == null || (object)shadowController == null) return;

        Component carry;
        if (remoteInput.holyCarryingObject && TryResolveCarryTarget(remoteInput.holyCarryObjectName, holy, out carry))
        {
            try
            {
                pickUpObjectSwitchCarrierMethod.Invoke(carry, new object[] { shadowController });
                carrySyncBlockedUntilFrame = Time.frameCount + 2;
                lastRemoteConfirmDown = true;
                Debug.Log("[NET] Carry transfer remote-confirm: Holy -> Shadow object=" + carry.name + " frame=" + Time.frameCount);
            }
            catch (Exception ex)
            {
                Debug.Log("[NET] Carry transfer remote-confirm exception=" + ex.GetType().Name + " " + ex.Message);
            }
            return;
        }

        if (remoteInput.shadowCarryingObject && TryResolveCarryTarget(remoteInput.shadowCarryObjectName, shadow, out carry))
        {
            try
            {
                pickUpObjectSwitchCarrierMethod.Invoke(carry, new object[] { holyController });
                carrySyncBlockedUntilFrame = Time.frameCount + 2;
                lastRemoteConfirmDown = true;
                Debug.Log("[NET] Carry transfer remote-confirm: Shadow -> Holy object=" + carry.name + " frame=" + Time.frameCount);
            }
            catch (Exception ex)
            {
                Debug.Log("[NET] Carry transfer remote-confirm exception=" + ex.GetType().Name + " " + ex.Message);
            }
            return;
        }

        if (TryGetCurrentCarryObject(holy, out carry))
        {
            try
            {
                pickUpObjectSwitchCarrierMethod.Invoke(carry, new object[] { shadowController });
                carrySyncBlockedUntilFrame = Time.frameCount + 2;
                lastRemoteConfirmDown = true;
                Debug.Log("[NET] Carry transfer remote-confirm: Holy -> Shadow object=" + carry.name + " frame=" + Time.frameCount);
            }
            catch (Exception ex)
            {
                Debug.Log("[NET] Carry transfer remote-confirm exception=" + ex.GetType().Name + " " + ex.Message);
            }
            return;
        }

        if (TryGetCurrentCarryObject(shadow, out carry))
        {
            try
            {
                pickUpObjectSwitchCarrierMethod.Invoke(carry, new object[] { holyController });
                carrySyncBlockedUntilFrame = Time.frameCount + 2;
                lastRemoteConfirmDown = true;
                Debug.Log("[NET] Carry transfer remote-confirm: Shadow -> Holy object=" + carry.name + " frame=" + Time.frameCount);
            }
            catch (Exception ex)
            {
                Debug.Log("[NET] Carry transfer remote-confirm exception=" + ex.GetType().Name + " " + ex.Message);
            }
        }
    }

    private static bool TryResolveCarryTarget(string objectName, GameObject fallbackPlayer, out Component carry)
    {
        carry = null;
        if (TryResolvePickUpObject(objectName, out carry))
            return true;
        return TryGetCurrentCarryObject(fallbackPlayer, out carry);
    }

    private static void TryProcessHostCarryTransfer(GameObject holy, GameObject shadow)
    {
        if (!IsHostPeer) return;
        bool confirmDown = ReadHolyConfirm();
        bool confirmEdge = confirmDown && !lastHostLocalConfirmDown;
        lastHostLocalConfirmDown = confirmDown;
        if (!confirmEdge) return;

        if ((object)pickUpObjectSwitchCarrierMethod == null) return;

        Component holyController = FindPlayerController(holy);
        Component shadowController = FindPlayerController(shadow);
        if ((object)holyController == null || (object)shadowController == null) return;

        Component carry;
        if (TryGetCurrentCarryObject(holy, out carry))
        {
            try
            {
                pickUpObjectSwitchCarrierMethod.Invoke(carry, new object[] { shadowController });
                carrySyncBlockedUntilFrame = Time.frameCount + 2;
                Debug.Log("[NET] Carry transfer host-confirm: Holy -> Shadow object=" + carry.name + " frame=" + Time.frameCount);
            }
            catch (Exception ex)
            {
                Debug.Log("[NET] Carry transfer host-confirm exception=" + ex.GetType().Name + " " + ex.Message);
            }
            return;
        }

        if (TryGetCurrentCarryObject(shadow, out carry))
        {
            try
            {
                pickUpObjectSwitchCarrierMethod.Invoke(carry, new object[] { holyController });
                carrySyncBlockedUntilFrame = Time.frameCount + 2;
                Debug.Log("[NET] Carry transfer host-confirm: Shadow -> Holy object=" + carry.name + " frame=" + Time.frameCount);
            }
            catch (Exception ex)
            {
                Debug.Log("[NET] Carry transfer host-confirm exception=" + ex.GetType().Name + " " + ex.Message);
            }
        }
    }

    private static void ApplyCarryObjectState(GameObject player, string objectName, bool carrying, string source)
    {
        if (player == null) return;
        EnsureReflectionCache();
        Component controller = FindPlayerController(player);
        if (controller == null) return;
        if (ShouldDeferCarrySync()) return;
        Component pickUpObject;
        bool found = false;
        if (carrying)
        {
            found = TryResolvePickUpObject(objectName, out pickUpObject);
            if (!found)
                found = TryGetCurrentCarryObject(player, out pickUpObject);
        }
        else
        {
            if (!TryResolvePickUpObject(objectName, out pickUpObject))
                TryGetCurrentCarryObject(player, out pickUpObject);
            found = (object)pickUpObject != null;
        }
        string applyKey = player.name + "|" + carrying + "|" + (objectName ?? "") + "|" + found;
        if (player == _cachedHoly)
        {
            if (applyKey == lastAppliedHolyCarryKey) return;
            lastAppliedHolyCarryKey = applyKey;
        }
        else if (player == _cachedShadow)
        {
            if (applyKey == lastAppliedShadowCarryKey) return;
            lastAppliedShadowCarryKey = applyKey;
        }
        Debug.Log("[NET] Carry apply " + source + ": player=" + player.name + " carrying=" + carrying + " object=" + (objectName ?? "") + " found=" + found + " frame=" + Time.frameCount);
        try
        {
            if (carrying)
            {
                if (!found) return;
                object currentOwner = null;
                if ((object)pickUpObjectPlayerWhoCarriesField != null)
                {
                    currentOwner = pickUpObjectPlayerWhoCarriesField.GetValue(pickUpObject);
                    if ((object)currentOwner != null && (object)currentOwner != controller && (object)pickUpObjectObjectDroppedMethod != null)
                    {
                        pickUpObjectObjectDroppedMethod.Invoke(pickUpObject, null);
                    }
                }
                if ((object)pickUpObjectPickedUpField != null) pickUpObjectPickedUpField.SetValue(pickUpObject, true);
                if ((object)pickUpObjectPlayerWhoCarriesField != null) pickUpObjectPlayerWhoCarriesField.SetValue(pickUpObject, controller);
                if ((object)pickUpObjectOnCarrierChangedMethod != null) pickUpObjectOnCarrierChangedMethod.Invoke(pickUpObject, new object[] { controller });
                if ((object)pickUpObjectOnItemPickedUpMethod != null) pickUpObjectOnItemPickedUpMethod.Invoke(pickUpObject, new object[] { controller });
                if ((object)pickUpObjectCollField != null)
                {
                    Behaviour coll = pickUpObjectCollField.GetValue(pickUpObject) as Behaviour;
                    if (coll != null) coll.enabled = false;
                }
                MethodInfo objectPickedUp = playerControllerType.GetMethod("ObjectPickedUp", BindingFlags.Public | BindingFlags.Instance);
                if ((object)objectPickedUp != null) objectPickedUp.Invoke(controller, new object[] { pickUpObject });
            }
            else
            {
                if ((object)pickUpObject != null)
                {
                    if ((object)pickUpObjectObjectDroppedMethod != null)
                    {
                        pickUpObjectObjectDroppedMethod.Invoke(pickUpObject, null);
                    }
                    if ((object)pickUpObjectOnCarrierChangedMethod != null) pickUpObjectOnCarrierChangedMethod.Invoke(pickUpObject, new object[] { null });
                    if ((object)pickUpObjectPickedUpField != null) pickUpObjectPickedUpField.SetValue(pickUpObject, false);
                    if ((object)pickUpObjectPlayerWhoCarriesField != null) pickUpObjectPlayerWhoCarriesField.SetValue(pickUpObject, null);
                    if ((object)pickUpObjectCollField != null)
                    {
                        Behaviour coll = pickUpObjectCollField.GetValue(pickUpObject) as Behaviour;
                        if (coll != null) coll.enabled = true;
                    }
                }
                MethodInfo objectDropped = playerControllerType.GetMethod("ObjectDropped", BindingFlags.Public | BindingFlags.Instance);
                if ((object)objectDropped != null) objectDropped.Invoke(controller, null);
            }
        }
        catch (Exception ex)
        {
            Debug.Log("[NET] Carry apply " + source + " exception=" + ex.GetType().Name + " " + ex.Message);
        }
    }

    private static bool ShouldDeferCarrySync()
    {
        return carrySyncBlockedUntilFrame > Time.frameCount;
    }

    private static void LogRxClientInput(ClientStateSync clientState)
    {
        string key = clientState.confirm + "|" + clientState.holyCarryingObject + "|" + clientState.shadowCarryingObject + "|" + (clientState.holyCarryObjectName ?? "") + "|" + (clientState.shadowCarryObjectName ?? "");
        if (key == lastRxClientInputKey) return;
        lastRxClientInputKey = key;
        Debug.Log("[NET] RX client-input: confirm=" + clientState.confirm + " Hcarry=" + clientState.holyCarryingObject + " Scarry=" + clientState.shadowCarryingObject + " Hobj=" + (clientState.holyCarryObjectName ?? "") + " Sobj=" + (clientState.shadowCarryObjectName ?? "") + " frame=" + Time.frameCount);
    }

    private static void LogRxStateSync(NetworkStateSync state)
    {
        string key = state.holyCarryingObject + "|" + state.shadowCarryingObject + "|" + (state.holyCarryObjectName ?? "") + "|" + (state.shadowCarryObjectName ?? "") + "|" + state.sceneIndex + "|" + state.targetScene;
        if (key == lastRxStateSyncKey) return;
        lastRxStateSyncKey = key;
        Debug.Log("[NET] RX state-sync: Hcarry=" + state.holyCarryingObject + " Scarry=" + state.shadowCarryingObject + " Hobj=" + (state.holyCarryObjectName ?? "") + " Sobj=" + (state.shadowCarryObjectName ?? "") + " scene=" + state.sceneIndex + " target=" + state.targetScene + " frame=" + Time.frameCount);
    }

    private static void ApplyCarryVisualState(bool holyCarrying, bool shadowCarrying, string source)
    {
        int desiredType = 2;
        if (holyCarrying && !shadowCarrying) desiredType = 0;
        else if (shadowCarrying && !holyCarrying) desiredType = 1;
        else if (holyCarrying && shadowCarrying) desiredType = 0;

        int sceneIdx = SceneManager.GetActiveScene().buildIndex;
        if (desiredType == lastAppliedCarryVisualType && sceneIdx == lastAppliedCarryVisualScene) return;

        EnsureReflectionCache();
        object playerTypeArg = desiredType;
        if ((object)inputPlayerTypeEnum != null)
        {
            playerTypeArg = Enum.ToObject(inputPlayerTypeEnum, desiredType);
        }

        int appliedObjects = 0;
        if ((object)goldObjectType != null && (object)goldObjectSpawnFormMethod != null)
        {
            UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(goldObjectType);
            if (objects != null)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    object obj = objects[i];
                    if (obj == null) continue;
                    try
                    {
                        goldObjectSpawnFormMethod.Invoke(obj, new object[] { playerTypeArg });
                        appliedObjects++;
                    }
                    catch { }
                }
            }
        }
        if ((object)goldSphereControllerType != null && (object)goldSphereChangeFormMethod != null)
        {
            UnityEngine.Object[] controllers = Resources.FindObjectsOfTypeAll(goldSphereControllerType);
            if (controllers != null)
            {
                for (int i = 0; i < controllers.Length; i++)
                {
                    object obj = controllers[i];
                    if (obj == null) continue;
                    try
                    {
                        goldSphereChangeFormMethod.Invoke(obj, new object[] { playerTypeArg });
                        appliedObjects++;
                    }
                    catch { }
                }
            }
        }

        if (appliedObjects <= 0) return;
        lastAppliedCarryVisualType = desiredType;
        lastAppliedCarryVisualScene = sceneIdx;
        Debug.Log("[NET] Carry apply " + source + ": holy=" + holyCarrying + " shadow=" + shadowCarrying + " desired=" + desiredType + " applied=" + appliedObjects + " frame=" + Time.frameCount);
    }

    private static bool IsPlayerCarryingObject(GameObject player)
    {
        if (player == null) return false;
        EnsureReflectionCache();
        if ((object)playerIsCarryingObjectMethod == null) return false;
        Component controller = FindPlayerController(player);
        if (controller == null) return false;
        try
        {
            object result = playerIsCarryingObjectMethod.Invoke(controller, null);
            return result is bool && (bool)result;
        }
        catch { return false; }
    }

    // Build client state: raw key-hold for jump/shoot (host detects edges).
    // Only confirm uses client-side edge detection to avoid racing InputManager.
    private static ClientStateSync BuildClientState()
    {
        var cs = new ClientStateSync();
        bool localHolyCarrying;
        bool localShadowCarrying;
        GameObject localHolyPlayer;
        GameObject localShadowPlayer;
        Component localCarry;
        cs.move = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) cs.move -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) cs.move += 1f;
        localShadowMove = cs.move;
        // Jump + Shoot: raw key-hold — host detects edges with frame-preserving pattern
        cs.jumpHold = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        cs.shootHold = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        cs.jump = false; cs.jumpRelease = false;
        cs.shoot = false; cs.shootRelease = false;
        localShadowJumpHold = cs.jumpHold;
        localShadowShootHold = cs.shootHold;
        // Confirm/interact is a hold input in the game's InputManager.
        cs.confirm = Input.GetKey(KeyCode.Space);
        localShadowConfirm = cs.confirm;
        cs.holyCarryingObject = false;
        cs.shadowCarryingObject = false;
        cs.holyCarryObjectName = "";
        cs.shadowCarryObjectName = "";
        if (TryGetLocalCarryState(out localHolyCarrying, out localShadowCarrying) && TryGetPlayers(out localHolyPlayer, out localShadowPlayer))
        {
            cs.holyCarryingObject = localHolyCarrying;
            cs.shadowCarryingObject = localShadowCarrying;
            CacheCarryState(localHolyCarrying, localShadowCarrying, "client-local");

            if (TryGetCurrentCarryObject(localShadowPlayer, out localCarry) && (object)localCarry != null)
            {
                cs.shadowCarryObjectName = localCarry.name;
            }
            else
            {
                if (localShadowCarrying)
                    cs.shadowCarryObjectName = GetCarryObjectName(localShadowPlayer);
            }
            if (localHolyCarrying)
                cs.holyCarryObjectName = GetCarryObjectName(localHolyPlayer);
        }
        CacheCarryObjectName(true, cs.holyCarryObjectName);
        CacheCarryObjectName(false, cs.shadowCarryObjectName);
        cs.escape = false;
        cs.agreed = localClientAgreed;
        // Shadow position from local GameObject
        cs.posX = 0f; cs.posY = 0f; cs.facingRight = false;
        if (_cachedShadow != null)
        {
            Vector3 p = _cachedShadow.transform.position;
            cs.posX = p.x; cs.posY = p.y;
            cs.facingRight = ReadFacingRight(_cachedShadow, false);
        }
        return cs;
    }

    public static NetworkInputState GetLocalInput() { return new NetworkInputState { move = localShadowMove, jump = localShadowJump, jumpHold = localShadowJumpHold, jumpRelease = localShadowJumpRelease, shoot = localShadowShoot, shootHold = localShadowShootHold, shootRelease = localShadowShootRelease, confirm = localShadowConfirm, holyCarryingObject = cachedHolyCarryingObject, shadowCarryingObject = cachedShadowCarryingObject, holyCarryObjectName = cachedHolyCarryObjectName, shadowCarryObjectName = cachedShadowCarryObjectName, agreed = localClientAgreed }; }
    private void OnDestroy() { Disconnect(); }
}

public class DevConsoleOverlay : MonoBehaviour
{
    private static DevConsoleOverlay current;

    private struct ConsoleEntry
    {
        public string time;
        public string message;
        public string stack;
        public LogType type;
    }

    private const int MaxEntries = 400;
    private readonly List<ConsoleEntry> entries = new List<ConsoleEntry>(MaxEntries);
    private Vector2 scroll;
    private bool visible;
    private bool keyDown;
    private bool showInfo = true;
    private bool showWarning = true;
    private bool showError = true;
    private bool showStack;
    private bool autoScroll = true;
    private GUIStyle lineStyle;
    private GUIStyle headerStyle;
    private static readonly GUILayoutOption[] NoOptions = new GUILayoutOption[0];

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private void OnEnable()
    {
        current = this;
        Application.logMessageReceived += HandleLog;
        Debug.Log("[DEVCON] Developer console ready. Toggle with ~ or Network menu button.");
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
        if (current == this) current = null;
    }

    private void Update()
    {
        bool now = Input.GetKey(KeyCode.BackQuote) || IsWinToggleDown();
        if (now && !keyDown) visible = !visible;
        keyDown = now;
    }

    public static void Toggle()
    {
        if (current != null) current.visible = !current.visible;
    }

    public static bool IsToggleEvent(Event e)
    {
        if (e == null) return false;
        if (e.keyCode == KeyCode.BackQuote) return true;
        return e.character == (char)96 || e.character == (char)126;
    }

    private static bool IsWinToggleDown()
    {
        try
        {
            return (GetAsyncKeyState(0xC0) & 0x8000) != 0;
        }
        catch { return false; }
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (entries.Count >= MaxEntries) entries.RemoveAt(0);
        entries.Add(new ConsoleEntry
        {
            time = DateTime.Now.ToString("HH:mm:ss"),
            message = condition == null ? "" : condition,
            stack = stackTrace == null ? "" : stackTrace,
            type = type
        });
        if (autoScroll) scroll.y = 999999f;
    }

    private void OnGUI()
    {
        Event e = Event.current;
        if (e != null && e.type == EventType.KeyDown && IsToggleEvent(e))
        {
            visible = !visible;
            e.Use();
        }

        if (!visible) return;
        EnsureStyles();

        float width = Mathf.Min(Screen.width - 40f, 980f);
        float height = Mathf.Min(Screen.height - 40f, 620f);
        Rect panel = new Rect(20f, 20f, width, height);

        GUI.Box(panel, "");
        GUILayout.BeginArea(new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, panel.height - 16f));

        GUILayout.BeginHorizontal(NoOptions);
        GUILayout.Label("Developer Console", headerStyle, GUILayout.Width(180f));
        GUILayout.Label("Build: " + BuildInfo.CompiledAt, GUILayout.Width(190f));
        showInfo = GUILayout.Toggle(showInfo, "Info", "Button", GUILayout.Width(64f));
        showWarning = GUILayout.Toggle(showWarning, "Warning", "Button", GUILayout.Width(82f));
        showError = GUILayout.Toggle(showError, "Error", "Button", GUILayout.Width(70f));
        showStack = GUILayout.Toggle(showStack, "Stack", "Button", GUILayout.Width(70f));
        autoScroll = GUILayout.Toggle(autoScroll, "Auto", "Button", GUILayout.Width(64f));
        if (GUILayout.Button("Clear", GUILayout.Width(64f))) entries.Clear();
        GUILayout.FlexibleSpace();
        GUILayout.Label("~", GUILayout.Width(42f));
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        for (int i = 0; i < entries.Count; i++)
        {
            ConsoleEntry entry = entries[i];
            if (!ShouldShow(entry.type)) continue;

            Color old = GUI.color;
            GUI.color = ColorFor(entry.type);
            GUILayout.Label("[" + entry.time + "] [" + entry.type + "] " + entry.message, lineStyle, NoOptions);
            if (showStack && entry.stack.Length > 0 && entry.type != LogType.Log)
            {
                GUILayout.Label(entry.stack, lineStyle, NoOptions);
            }
            GUI.color = old;
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private bool ShouldShow(LogType type)
    {
        if (type == LogType.Log) return showInfo;
        if (type == LogType.Warning) return showWarning;
        return showError;
    }

    private Color ColorFor(LogType type)
    {
        if (type == LogType.Warning) return new Color(1f, 0.85f, 0.25f, 1f);
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) return new Color(1f, 0.35f, 0.3f, 1f);
        return Color.white;
    }

    private void EnsureStyles()
    {
        if (lineStyle != null) return;
        lineStyle = new GUIStyle(GUI.skin.label);
        lineStyle.wordWrap = true;
        lineStyle.fontSize = 12;
        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 14;
    }
}
