using System;
using System.IO;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace ShadowsOfDoubtCoop.Net
{
    public class NetworkTransport
    {
        private EventBasedNetListener? _listener;
        private NetManager? _netManager;
        private NetPeer? _connectedPeer;

        private readonly NetDataWriter _writer = new NetDataWriter();
        private readonly FileTransferManager _fileTransfer = new FileTransferManager();

        public bool IsHost { get; private set; }
        public bool IsConnected => _connectedPeer != null && _connectedPeer.ConnectionState == ConnectionState.Connected;
        public int Ping => _connectedPeer?.Ping ?? 0;

        // Callbacks to main thread Unity coordinator
        public Action<Vector3, Quaternion>? OnPlayerStateReceived { get; set; }
        public Action<int, bool, bool>? OnDoorStateReceived { get; set; }
        public Action<float>? OnTimeSyncReceived { get; set; }
        public Action<int, string, float, float, bool>? OnCasePinReceived { get; set; }
        public Action<string>? OnSaveFileReceived { get; set; }
        public Action<string>? OnStatusChanged { get; set; }
        public Action<string>? OnTransferProgress { get; set; }
        public Action<int, bool>? OnPeerConnected { get; set; }
        public Action<string>? OnPeerDisconnected { get; set; }

        public void Poll()
        {
            _netManager?.PollEvents();
        }

        public void StartHost(int port)
        {
            Stop();

            _listener = new EventBasedNetListener();
            _netManager = new NetManager(_listener)
            {
                AutoRecycle = true,
                IPv6Enabled = false
            };

            _listener.ConnectionRequestEvent += request =>
            {
                Plugin.Log.LogInfo($"[Host] Connection request from {request.RemoteEndPoint}. Validating key...");
                request.AcceptIfKey(NetworkConfig.ConnectionKey);
            };

            _listener.PeerConnectedEvent += peer =>
            {
                _connectedPeer = peer;
                IsHost = true;
                OnStatusChanged?.Invoke($"Partner connected (Peer #{peer.Id})");
                Plugin.Log.LogInfo($"[Host] Partner connected (Peer #{peer.Id}). Sending Hello...");
                SendHello(peer);
                OnPeerConnected?.Invoke(peer.Id, true);
            };

            _listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                _connectedPeer = null;
                string reason = disconnectInfo.Reason.ToString();
                OnStatusChanged?.Invoke($"Partner disconnected: {reason}");
                Plugin.Log.LogInfo($"[Host] Partner disconnected: {reason}");
                OnPeerDisconnected?.Invoke(reason);
            };

            _listener.NetworkReceiveEvent += HandleNetworkReceive;

            if (_netManager.Start(port))
            {
                IsHost = true;
                OnStatusChanged?.Invoke($"Hosting on port {port}. Awaiting partner...");
                Plugin.Log.LogInfo($"[Host] Server listening on UDP {port}");
            }
            else
            {
                OnStatusChanged?.Invoke($"Failed to bind UDP port {port}!");
                Plugin.Log.LogError($"[Host] Could not bind port {port}");
            }
        }

        public void StartClient(string hostIp, int port)
        {
            Stop();

            _listener = new EventBasedNetListener();
            _netManager = new NetManager(_listener)
            {
                AutoRecycle = true,
                IPv6Enabled = false
            };

            _listener.PeerConnectedEvent += peer =>
            {
                _connectedPeer = peer;
                IsHost = false;
                OnStatusChanged?.Invoke($"Connected to Host! (Peer #{peer.Id})");
                Plugin.Log.LogInfo($"[Client] Connected to host (Peer #{peer.Id}). Sending Hello...");
                SendHello(peer);
                OnPeerConnected?.Invoke(peer.Id, false);
            };

            _listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                _connectedPeer = null;
                string reason = disconnectInfo.Reason.ToString();
                OnStatusChanged?.Invoke($"Disconnected from host: {reason}");
                Plugin.Log.LogInfo($"[Client] Disconnected: {reason}");
                OnPeerDisconnected?.Invoke(reason);
            };

            _listener.NetworkReceiveEvent += HandleNetworkReceive;

            if (_netManager.Start())
            {
                OnStatusChanged?.Invoke($"Connecting to {hostIp}:{port}...");
                _netManager.Connect(hostIp, port, NetworkConfig.ConnectionKey);
                Plugin.Log.LogInfo($"[Client] Initiating connection to {hostIp}:{port}");
            }
            else
            {
                OnStatusChanged?.Invoke("Failed to start client network manager!");
                Plugin.Log.LogError("[Client] Failed to start client network manager!");
            }
        }

        public void Stop()
        {
            if (_netManager != null)
            {
                _netManager.Stop();
                _netManager = null;
            }
            _connectedPeer = null;
            IsHost = false;
            OnStatusChanged?.Invoke("Network stopped. Ready.");
        }

        public void SendPlayerState(uint sequence, Vector3 pos, Quaternion rot)
        {
            if (!IsConnected || _connectedPeer == null) return;

            _writer.Reset();
            _writer.Put((byte)PacketType.PlayerState);
            _writer.Put(sequence);
            _writer.Put(pos.x);
            _writer.Put(pos.y);
            _writer.Put(pos.z);
            _writer.Put(rot.x);
            _writer.Put(rot.y);
            _writer.Put(rot.z);
            _writer.Put(rot.w);
            _connectedPeer.Send(_writer, DeliveryMethod.Sequenced);
        }

        public void SendGameTime(float gameTime)
        {
            if (!IsConnected || _connectedPeer == null) return;

            _writer.Reset();
            _writer.Put((byte)PacketType.TimeSync);
            _writer.Put(gameTime);
            _connectedPeer.Send(_writer, DeliveryMethod.Sequenced);
        }

        public void SendDoorState(int wallId, bool open, bool locked)
        {
            if (!IsConnected || _connectedPeer == null) return;

            _writer.Reset();
            _writer.Put((byte)PacketType.DoorState);
            _writer.Put(wallId);
            _writer.Put(open);
            _writer.Put(locked);
            _connectedPeer.Send(_writer, DeliveryMethod.ReliableOrdered);
        }

        public void SendCasePin(int caseId, string evId, float posX, float posY, bool isPinned)
        {
            if (!IsConnected || _connectedPeer == null) return;

            _writer.Reset();
            _writer.Put((byte)PacketType.CaseBoardOp);
            _writer.Put(caseId);
            _writer.Put(evId);
            _writer.Put(posX);
            _writer.Put(posY);
            _writer.Put(isPinned);
            _connectedPeer.Send(_writer, DeliveryMethod.ReliableOrdered);
        }

        public void TriggerHostSaveSync(Action<float> onProgress)
        {
            if (!IsHost || _connectedPeer == null)
            {
                Plugin.Log.LogWarning("[Sync] Only host with connected partner can trigger save sync.");
                return;
            }

            try
            {
                string saveDir = FileTransferManager.SaveDirectory;
                if (!Directory.Exists(saveDir)) return;

                DirectoryInfo di = new DirectoryInfo(saveDir);
                FileInfo[] files = di.GetFiles("*.sodb");
                if (files.Length == 0) return;

                Array.Sort(files, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
                FileInfo latestSave = files[0];

                OnStatusChanged?.Invoke($"Syncing save {latestSave.Name} to partner...");
                _fileTransfer.SendFile(_connectedPeer, latestSave.FullName, 0, onProgress);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Sync] Failed to stream save to peer: {ex.Message}");
            }
        }

        private void SendHello(NetPeer peer)
        {
            _writer.Reset();
            _writer.Put((byte)PacketType.Hello);
            _writer.Put(NetworkConfig.ProtocolVersion);
            _writer.Put(NetworkConfig.GameVersion);
            _writer.Put(NetworkConfig.ModVersion);
            peer.Send(_writer, DeliveryMethod.ReliableOrdered);
        }

        private void HandleNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
                if (reader.AvailableBytes < 1) return;

                byte rawType = reader.GetByte();
                PacketType packetType = (PacketType)rawType;

                switch (packetType)
                {
                    case PacketType.Hello:
                        if (reader.AvailableBytes >= 4)
                        {
                            int remoteProto = reader.GetInt();
                            string remoteGame = reader.GetString();
                            string remoteMod = reader.GetString();
                            if (remoteProto != NetworkConfig.ProtocolVersion || remoteGame != NetworkConfig.GameVersion)
                            {
                                Plugin.Log.LogError($"[Auth] Protocol/Game version mismatch! Local: (Proto {NetworkConfig.ProtocolVersion}, Game {NetworkConfig.GameVersion}), Remote: (Proto {remoteProto}, Game {remoteGame}). Disconnecting.");
                                peer.Disconnect();
                            }
                            else
                            {
                                Plugin.Log.LogInfo($"[Auth] Handshake verified with partner (Proto: v{remoteProto}, Game: {remoteGame}, Mod: {remoteMod})");
                            }
                        }
                        break;

                    case PacketType.PlayerState:
                        if (reader.AvailableBytes >= 32)
                        {
                            uint seq = reader.GetUInt();
                            float px = reader.GetFloat();
                            float py = reader.GetFloat();
                            float pz = reader.GetFloat();
                            float rx = reader.GetFloat();
                            float ry = reader.GetFloat();
                            float rz = reader.GetFloat();
                            float rw = reader.GetFloat();

                            OnPlayerStateReceived?.Invoke(new Vector3(px, py, pz), new Quaternion(rx, ry, rz, rw));
                        }
                        break;

                    case PacketType.DoorState:
                        if (reader.AvailableBytes >= 6)
                        {
                            int wallId = reader.GetInt();
                            bool isOpen = reader.GetBool();
                            bool isLocked = reader.GetBool();

                            OnDoorStateReceived?.Invoke(wallId, isOpen, isLocked);
                        }
                        break;

                    case PacketType.CaseBoardOp:
                        if (reader.AvailableBytes >= 10)
                        {
                            int caseId = reader.GetInt();
                            string evId = reader.GetString();
                            float posX = reader.GetFloat();
                            float posY = reader.GetFloat();
                            bool isPinned = reader.GetBool();

                            OnCasePinReceived?.Invoke(caseId, evId, posX, posY, isPinned);
                        }
                        break;

                    case PacketType.TimeSync:
                        if (!IsHost && reader.AvailableBytes >= 4)
                        {
                            float hostGameTime = reader.GetFloat();
                            OnTimeSyncReceived?.Invoke(hostGameTime);
                        }
                        break;

                    case PacketType.FileChunk:
                        string? assembledPath = _fileTransfer.ReceiveChunk(reader, out float progress);
                        OnTransferProgress?.Invoke($"Receiving File: {progress * 100f:F0}%");
                        if (assembledPath != null)
                        {
                            OnSaveFileReceived?.Invoke(assembledPath);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Network] Exception processing packet from {peer.Id}: {ex.Message}");
            }
        }
    }
}
