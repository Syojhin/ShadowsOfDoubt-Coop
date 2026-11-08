using System;
using System.IO;
using ShadowsOfDoubtCoop.Net;
using ShadowsOfDoubtCoop.Sync;
using UnityEngine;

namespace ShadowsOfDoubtCoop
{
    public class CoopNetworkManager : MonoBehaviour
    {
        public CoopNetworkManager(IntPtr ptr) : base(ptr) { }

        public static CoopNetworkManager? Instance { get; private set; }
        public static bool IsApplyingRemote { get; set; } = false;

        // Pure C# Network transport (isolated from IL2CPP reflection)
        private readonly NetworkTransport _transport = new NetworkTransport();
        private readonly RemotePuppet _puppet = new RemotePuppet();

        private uint _packetSequence = 0;
        private float _lastSendTime = 0f;
        private float _lastTimeSync = 0f;
        private const float SendInterval = 0.033f; // 30Hz position replication
        private const float TimeSyncInterval = 1.0f; // 1Hz clock sync

        public bool IsHost => _transport.IsHost;
        public bool IsConnected => _transport.IsConnected;

        // UI & Input state
        private bool _showUi = true;
        private bool _inputFallbackMode = false;
        private string _targetIp = "26.0.0.1";
        private int _port = NetworkConfig.DefaultPort;
        private string _statusText = "Ready to connect over Radmin VPN (Port 7777)";
        private int _ping = 0;
        private string _lastTransferStatus = "";

        // Telemetry
        public Vector3 RemotePlayerPosition => _puppetIsActive ? _remotePos : Vector3.zero;
        public Quaternion RemotePlayerRotation => _puppetIsActive ? _remoteRot : Quaternion.identity;
        public bool HasRemotePlayer => _puppetIsActive;

        private Vector3 _remotePos;
        private Quaternion _remoteRot;
        private bool _puppetIsActive = false;

        private void Awake()
        {
            Instance = this;

            // Wire up callbacks from pure C# network transport
            _transport.OnPlayerStateReceived = OnRemotePlayerState;
            _transport.OnDoorStateReceived = ApplyRemoteDoor;
            _transport.OnTimeSyncReceived = ApplyRemoteTimeSync;
            _transport.OnCasePinReceived = ApplyRemoteCasePin;
            _transport.OnPlayerCombatStateReceived = OnRemotePlayerCombat;
            _transport.OnSaveFileReceived = OnSaveFileReceived;
            _transport.OnStatusChanged = text => _statusText = text;
            _transport.OnTransferProgress = text => _lastTransferStatus = text;
            _transport.OnPeerConnected = (peerId, isHost) =>
            {
                _statusText = isHost ? $"Partner connected (Peer #{peerId})" : $"Connected to host (Peer #{peerId})";
            };
            _transport.OnPeerDisconnected = reason =>
            {
                _statusText = $"Disconnected: {reason}";
                _puppetIsActive = false;
                _puppet.Destroy();
            };
        }

        private void Start()
        {
            Plugin.Log.LogInfo("[CoopNetworkManager] Component initialized. Press F9 to toggle GUI.");
        }

        private void Update()
        {
            // F9 Toggle with Input safety check
            if (!_inputFallbackMode)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.F9))
                    {
                        _showUi = !_showUi;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[Input] Input.GetKeyDown unavailable ({ex.Message}). Switching to Event.current fallback.");
                    _inputFallbackMode = true;
                }
            }

            // Unfreeze hardware mouse cursor if F9 Co-op HUD is active or if in pause menu
            if (_showUi || (MainMenuController.Instance != null && MainMenuController.Instance.mainMenuActive))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Poll network events on main thread
            _transport.Poll();

            // Interpolate remote player avatar
            _puppet.Interpolate(Time.deltaTime);

            if (_transport.IsConnected)
            {
                _ping = _transport.Ping;

                // Transmit local player coordinates to peer (30Hz)
                if (Time.time - _lastSendTime >= SendInterval)
                {
                    _lastSendTime = Time.time;
                    BroadcastLocalPlayerTransform();
                }

                // Host broadcasts game clock (1Hz)
                if (_transport.IsHost && Time.time - _lastTimeSync >= TimeSyncInterval)
                {
                    _lastTimeSync = Time.time;
                    BroadcastGameTime();
                }
            }
        }

        public void StartHost()
        {
            _transport.StartHost(_port);
        }

        public void StartClient(string hostIp)
        {
            _transport.StartClient(hostIp, _port);
        }

        public void StopNetwork()
        {
            _transport.Stop();
            _puppetIsActive = false;
            _puppet.Destroy();
        }

        public void TriggerHostSaveAndSync()
        {
            _transport.TriggerHostSaveSync(progress =>
            {
                _lastTransferStatus = $"Syncing Save: {progress * 100f:F0}%";
            });
        }

        public void SendDoorState(int wallId, bool open, bool locked)
        {
            _transport.SendDoorState(wallId, open, locked);
        }

        public void SendCasePin(int caseId, string evId, float posX, float posY, bool isPinned)
        {
            _transport.SendCasePin(caseId, evId, posX, posY, isPinned);
        }

        public void SendPlayerDamageOrKO(float amount, bool isKO)
        {
            _transport.SendPlayerCombatState(amount, isKO);
        }

        private void BroadcastLocalPlayerTransform()
        {
            Vector3 pos;
            Quaternion rot;

            if (Player.Instance != null)
            {
                pos = Player.Instance.transform.position;
                rot = Player.Instance.transform.rotation;
            }
            else if (Camera.main != null)
            {
                pos = Camera.main.transform.position;
                rot = Camera.main.transform.rotation;
            }
            else
            {
                return;
            }

            _transport.SendPlayerState(++_packetSequence, pos, rot);
        }

        private void BroadcastGameTime()
        {
            if (SessionData.Instance == null) return;
            _transport.SendGameTime(SessionData.Instance.gameTime);
        }

        private void OnRemotePlayerState(Vector3 pos, Quaternion rot)
        {
            _remotePos = pos;
            _remoteRot = rot;
            _puppetIsActive = true;
            _puppet.UpdateTarget(_remotePos, _remoteRot);
        }

        private void OnRemotePlayerCombat(float dmg, bool isKO)
        {
            if (isKO)
            {
                _puppet.SetDowned(true);
                _statusText = "[!] PARTNER DOWNED / UNCONSCIOUS!";
                Plugin.Log.LogWarning($"[Combat] Remote partner was knocked out at {_remotePos}!");
            }
            else
            {
                if (dmg > 0.5f)
                {
                    _lastTransferStatus = $"[ALERT] Partner took {dmg:F0} damage!";
                    Plugin.Log.LogInfo($"[Combat] Remote partner took {dmg:F1} damage.");
                }
                else
                {
                    _puppet.SetDowned(false);
                    _statusText = "Partner recovered";
                    Plugin.Log.LogInfo("[Combat] Remote partner recovered.");
                }
            }
        }

        private void ApplyRemoteDoor(int wallId, bool isOpen, bool isLocked)
        {
            try
            {
                IsApplyingRemote = true;

                NewDoor[] doors = GameObject.FindObjectsOfType<NewDoor>();
                foreach (NewDoor door in doors)
                {
                    if (door.wall != null && door.wall.id == wallId)
                    {
                        door.SetLocked(isLocked, null, false);
                        door.SetOpen(isOpen ? door.openAngle : 0f, null, true, door.doorOpenSpeed);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Doors] Failed to apply remote door state: {ex.Message}");
            }
            finally
            {
                IsApplyingRemote = false;
            }
        }

        private void ApplyRemoteTimeSync(float hostGameTime)
        {
            if (SessionData.Instance != null)
            {
                float drift = Mathf.Abs(SessionData.Instance.gameTime - hostGameTime);
                if (drift > 2.0f) // Correct clock drift > 2 seconds
                {
                    SessionData.Instance.gameTime = hostGameTime;
                }
            }
        }

        private void ApplyRemoteCasePin(int caseId, string evId, float posX, float posY, bool isPinned)
        {
            try
            {
                IsApplyingRemote = true;

                if (GameplayController.Instance == null || GameplayController.Instance.evidenceDictionary == null)
                {
                    Plugin.Log.LogWarning("[CaseBoard] GameplayController or evidenceDictionary unavailable.");
                    return;
                }

                if (!GameplayController.Instance.evidenceDictionary.ContainsKey(evId))
                {
                    Plugin.Log.LogWarning($"[CaseBoard] Evidence ID '{evId}' not found in local dictionary.");
                    return;
                }

                Evidence ev = GameplayController.Instance.evidenceDictionary[evId];

                Case? targetCase = null;
                if (CasePanelController.Instance != null)
                {
                    if (caseId >= 0 && CasePanelController.Instance.activeCases != null)
                    {
                        for (int i = 0; i < CasePanelController.Instance.activeCases.Count; i++)
                        {
                            Case c = CasePanelController.Instance.activeCases[i];
                            if (c != null && c.id == caseId)
                            {
                                targetCase = c;
                                break;
                            }
                        }
                    }

                    if (targetCase == null)
                    {
                        targetCase = CasePanelController.Instance.activeCase;
                    }

                    if (targetCase != null)
                    {
                        if (isPinned)
                        {
                            CasePanelController.Instance.PinToCasePanel(targetCase, ev, Evidence.DataKey.name, false, new Vector2(posX, posY), false);
                            Plugin.Log.LogInfo($"[CaseBoard] Synced remote pin: {evId} at ({posX:F1}, {posY:F1}) to case '{targetCase.name}'");
                        }
                        else
                        {
                            CasePanelController.Instance.UnPinFromCasePanel(targetCase, ev, null, false, null);
                            Plugin.Log.LogInfo($"[CaseBoard] Synced remote unpin: {evId} from case '{targetCase.name}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[CaseBoard] Failed to apply remote case board operation: {ex.Message}");
            }
            finally
            {
                IsApplyingRemote = false;
            }
        }

        private void OnSaveFileReceived(string assembledPath)
        {
            _statusText = $"Save received! Ready to load {Path.GetFileName(assembledPath)}.";
            Plugin.Log.LogInfo($"[Sync] File transfer complete: {assembledPath}");
        }

        private void OnGUI()
        {
            if (_inputFallbackMode && Event.current != null && Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.F9)
                {
                    _showUi = !_showUi;
                    Event.current.Use();
                }
            }

            if (!_showUi) return;

            GUI.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 0.94f);
            GUI.contentColor = new Color(0.96f, 0.94f, 0.88f);

            GUILayout.BeginArea(new Rect(20, 20, 380, 330), GUI.skin.box);
            GUILayout.Space(6);

            // Header (ASCII-safe)
            GUI.contentColor = new Color(0.83f, 0.69f, 0.22f); // Imperial Gold
            GUILayout.Label("[+] SHADOWS OF DOUBT // CO-OP SUITE", UnityStyles.HeaderStyle);
            GUI.contentColor = new Color(0.70f, 0.70f, 0.72f);
            GUILayout.Label("Engineered by Syojhin & The Sunwoo Sisterhood", UnityStyles.SubtitleStyle);
            GUILayout.Space(8);

            // Status bar
            GUI.contentColor = IsConnected ? new Color(0.35f, 0.85f, 0.45f) : new Color(0.95f, 0.75f, 0.25f);
            GUILayout.Label($"Status: {_statusText}");
            if (!string.IsNullOrEmpty(_lastTransferStatus))
            {
                GUILayout.Label($"Transfer: {_lastTransferStatus}");
            }
            if (IsConnected)
            {
                GUILayout.Label($"Latency: {_ping} ms | Mode: {(IsHost ? "HOST" : "PEER")}");
                if (_puppetIsActive)
                {
                    GUILayout.Label($"Partner Pos: ({_remotePos.x:F1}, {_remotePos.y:F1}, {_remotePos.z:F1})");
                }
            }
            GUILayout.Space(8);

            // Controls
            if (!IsConnected && !_transport.IsConnected)
            {
                if (GUILayout.Button("HOST INVESTIGATION (Port 7777)", GUILayout.Height(32)))
                {
                    StartHost();
                }

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Host IP:", GUILayout.Width(60));
                _targetIp = GUILayout.TextField(_targetIp);
                GUILayout.EndHorizontal();

                if (GUILayout.Button("JOIN INVESTIGATION", GUILayout.Height(32)))
                {
                    StartClient(_targetIp.Trim());
                }
            }
            else
            {
                if (IsHost && GUILayout.Button("SYNC CURRENT SAVE TO PARTNER", GUILayout.Height(28)))
                {
                    TriggerHostSaveAndSync();
                }

                GUILayout.Space(4);
                if (GUILayout.Button("DISCONNECT / STOP", GUILayout.Height(28)))
                {
                    StopNetwork();
                }
            }

            GUILayout.FlexibleSpace();
            GUI.contentColor = new Color(0.5f, 0.5f, 0.5f);
            GUILayout.Label("Press [F9] to hide this HUD  |  Radmin VPN Ready", UnityStyles.FooterStyle);

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            StopNetwork();
        }
    }

    internal static class UnityStyles
    {
        private static GUIStyle? _headerStyle;
        public static GUIStyle HeaderStyle => _headerStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        private static GUIStyle? _subtitleStyle;
        public static GUIStyle SubtitleStyle => _subtitleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter
        };

        private static GUIStyle? _footerStyle;
        public static GUIStyle FooterStyle => _footerStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleCenter
        };
    }
}
