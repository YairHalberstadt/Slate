﻿using System;
using ProtoBuf;

namespace Slate.Networking.Shared.Protocol
{
    public record Uuid(
        [property: ProtoMember(1, DataFormat = DataFormat.FixedSize)] ulong A, 
        [property: ProtoMember(2, DataFormat = DataFormat.FixedSize)] ulong B
    )
    {
        public Guid ToGuid()
        {
            Span<byte> guidBytes = stackalloc byte[16];
            BitConverter.TryWriteBytes(guidBytes[..8], A);
            BitConverter.TryWriteBytes(guidBytes[8..16], B);

            return new Guid(guidBytes);
        }
    }

    //[ProtoContract]
    //public class Uuid : IEquatable<Uuid>
    //{
    //    [ProtoMember(1, Name = @"a", DataFormat = DataFormat.FixedSize)]
    //    public ulong A { get; init; }

    //    [ProtoMember(2, Name = @"b", DataFormat = DataFormat.FixedSize)]
    //    public ulong B { get; init; }

    //    public Guid ToGuid()
    //    {
    //        Span<byte> guidBytes = stackalloc byte[16];
    //        BitConverter.TryWriteBytes(guidBytes[..8], (ulong)A);
    //        BitConverter.TryWriteBytes(guidBytes[8..16], (ulong)B);

    //        return new Guid(guidBytes);
    //    }

    //    public override bool Equals(object? obj)
    //    {
    //        if (ReferenceEquals(null, obj)) return false;
    //        if (ReferenceEquals(this, obj)) return true;
    //        if (obj.GetType() != this.GetType()) return false;
    //        return Equals((Uuid)obj);
    //    }

    //    public bool Equals(Uuid other)
    //    {
    //        if (ReferenceEquals(null, other)) return false;
    //        if (ReferenceEquals(this, other)) return true;
    //        return A == other.A && B == other.B;
    //    }

    //    public override int GetHashCode()
    //    {
    //        return HashCode.Combine(A, B);
    //    }

    //    public static bool operator ==(Uuid left, Uuid right)
    //    {
    //        return Equals(left, right);
    //    }

    //    public static bool operator !=(Uuid left, Uuid right)
    //    {
    //        return !Equals(left, right);
    //    }
    //}
}