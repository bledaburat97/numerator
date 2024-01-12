using System;
using Unity.Collections;
using Unity.Netcode;

namespace Scripts
{
    public struct PlayerData : IEquatable<PlayerData>, INetworkSerializable
    {
        public ulong clientId;
        public FixedString64Bytes playerName;
        public FixedString64Bytes playerId;
        
        public bool Equals(PlayerData other)
        {
            return 
                clientId == other.clientId && 
                playerName == other.playerName &&
                playerId == other.playerId;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref playerName);
            serializer.SerializeValue(ref playerId);
        }
    }
}