using System;
using System.Collections.Generic;
using System.IO;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ShadowsOfDoubtCoop.Net
{
    public class FileTransferManager
    {
        public const int ChunkSize = 60 * 1024; // 60 KB chunks for MTU-safe reliable transport

        private readonly Dictionary<string, InboundFile> _inboundFiles = new Dictionary<string, InboundFile>();

        private class InboundFile
        {
            public string FileName { get; set; } = "";
            public byte FileType { get; set; } // 0 = Save (.sodb), 1 = City (.citb)
            public int TotalChunks { get; set; }
            public int ReceivedChunks { get; set; }
            public byte[][] Chunks { get; set; } = null!;
        }

        public static string SaveDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", "ColePowered Games", "Shadows of Doubt", "Save");

        public static string CityDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", "ColePowered Games", "Shadows of Doubt", "Cities");

        public void SendFile(NetPeer peer, string filePath, byte fileType, Action<float>? onProgress = null)
        {
            if (!File.Exists(filePath))
            {
                Plugin.Log.LogError($"[FileTransfer] File not found: {filePath}");
                return;
            }

            byte[] fileBytes = File.ReadAllBytes(filePath);
            string fileId = Path.GetFileName(filePath);
            int totalChunks = (int)Math.Ceiling((double)fileBytes.Length / ChunkSize);

            Plugin.Log.LogInfo($"[FileTransfer] Streaming {fileId} ({fileBytes.Length} bytes) in {totalChunks} chunks...");

            NetDataWriter writer = new NetDataWriter();

            for (int i = 0; i < totalChunks; i++)
            {
                int offset = i * ChunkSize;
                int count = Math.Min(ChunkSize, fileBytes.Length - offset);

                writer.Reset();
                writer.Put((byte)PacketType.FileChunk);
                writer.Put(fileType);
                writer.Put(fileId);
                writer.Put(i);
                writer.Put(totalChunks);
                writer.Put(count);
                writer.Put(fileBytes, offset, count);

                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                onProgress?.Invoke((float)(i + 1) / totalChunks);
            }

            Plugin.Log.LogInfo($"[FileTransfer] Finished queueing {fileId} chunks.");
        }

        public string? ReceiveChunk(NetPacketReader reader, out float progress)
        {
            progress = 0f;
            try
            {
                byte fileType = reader.GetByte();
                string fileId = reader.GetString();
                int chunkIndex = reader.GetInt();
                int totalChunks = reader.GetInt();
                int count = reader.GetInt();
                byte[] chunkData = new byte[count];
                reader.GetBytes(chunkData, count);

                if (!_inboundFiles.TryGetValue(fileId, out InboundFile inbound))
                {
                    inbound = new InboundFile
                    {
                        FileName = fileId,
                        FileType = fileType,
                        TotalChunks = totalChunks,
                        ReceivedChunks = 0,
                        Chunks = new byte[totalChunks][]
                    };
                    _inboundFiles[fileId] = inbound;
                }

                if (inbound.Chunks[chunkIndex] == null)
                {
                    inbound.Chunks[chunkIndex] = chunkData;
                    inbound.ReceivedChunks++;
                }

                progress = (float)inbound.ReceivedChunks / inbound.TotalChunks;

                if (inbound.ReceivedChunks >= inbound.TotalChunks)
                {
                    string targetFolder = (fileType == 0) ? SaveDirectory : CityDirectory;
                    Directory.CreateDirectory(targetFolder);

                    string targetPath = Path.Combine(targetFolder, "coop_" + fileId);
                    using (FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                    {
                        for (int i = 0; i < inbound.TotalChunks; i++)
                        {
                            fs.Write(inbound.Chunks[i], 0, inbound.Chunks[i].Length);
                        }
                    }

                    _inboundFiles.Remove(fileId);
                    Plugin.Log.LogInfo($"[FileTransfer] Successfully assembled {fileId} -> {targetPath}");
                    return targetPath;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[FileTransfer] Error handling chunk: {ex.Message}");
            }

            return null;
        }
    }
}
