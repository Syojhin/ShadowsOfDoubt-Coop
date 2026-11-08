using System;

namespace ShadowsOfDoubtCoop.Net
{
    public enum PacketType : byte
    {
        Hello = 0,
        PlayerState = 1,
        FileChunk = 2,
        LoadReady = 3,
        TimeSync = 4,
        DoorState = 5,
        InteractableAction = 6,
        CaseBoardOp = 7,
        CrimeEvent = 8,
        PlayerCombatState = 9
    }

    public static class NetworkConfig
    {
        public const int ProtocolVersion = 100;
        public const string GameVersion = "v42.04";
        public const string ModVersion = "1.0.0";
        public const string ConnectionKey = "SyojhinCoopV42";
        public const int DefaultPort = 7777;
    }
}
