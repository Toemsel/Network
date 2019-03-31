using Network.Converter;
using System;

namespace Network.Attributes
{
    /// <summary>
    /// Marks a property to be ignored by a <see cref="IPacketConverter"/>. Its value will not be serialised before being sent, so will be the default
    /// for its type upon deserialisation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PacketIgnorePropertyAttribute : Attribute { }
}