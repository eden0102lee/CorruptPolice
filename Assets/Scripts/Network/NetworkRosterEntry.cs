using System;
using Unity.Collections;
using Unity.Netcode;

public struct NetworkRosterEntry : INetworkSerializable, IEquatable<NetworkRosterEntry>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public int Role;
    public int TeamIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref Role);
        serializer.SerializeValue(ref TeamIndex);
    }

    public bool Equals(NetworkRosterEntry other)
    {
        return ClientId == other.ClientId &&
               PlayerName.Equals(other.PlayerName) &&
               Role == other.Role &&
               TeamIndex == other.TeamIndex;
    }
}
