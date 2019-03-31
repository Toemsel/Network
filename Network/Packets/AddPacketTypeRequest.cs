using Network.Attributes;
using System;
using System.Reflection;

namespace Network.Packets
{
    /// <summary>
    /// Instructs the paired <see cref="Connection"/> to add all the <see cref="Type"/>s in the given <see cref="Assembly"/>.
    /// </summary>
    [PacketType(6)]
    internal class AddPacketTypeRequest : RequestPacket
    {
        #region Constructors

        /// <summary>
        /// Constructs and returns a new instance of the <see cref="AddPacketTypeRequest"/> class, with the given <see cref="Assembly"/>s name specified.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly to add.</param>
        internal AddPacketTypeRequest(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The name of the <see cref="Assembly"/> that should be added.
        /// </summary>
        public string AssemblyName { get; set; }

        #endregion Properties
    }
}