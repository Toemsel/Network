using Network.Interfaces;
using Network.RSA;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Network
{
    public abstract class ConnectionContainer : IRSACapability
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionContainer"/> class.
        /// </summary>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        public ConnectionContainer(string ipAddress, int port)
        {
            IPAddress = ipAddress;
            Port = port;
        }

        /// <summary>
        /// Gets the ip address this container is connected to.
        /// </summary>
        /// <value>The ip address.</value>
        public string IPAddress { get; protected set; }

        /// <summary>
        /// Gets the port this container is connected to.
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; protected set; }

        /// <summary>
        /// The PublicKey of this instance.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public string PublicKey => RSAPair?.Public;

        /// <summary>
        /// The PrivateKey of this instance.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public string PrivateKey => RSAPair?.Private;

        /// <summary>
        /// The used KeySize of this instance.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public int KeySize => RSAPair?.KeySize ?? -1;

        /// <summary>
        /// Gets or sets the RSA-Pair.
        /// </summary>
        /// <value>The RSA pair.</value>
        public RSAPair RSAPair { get; set; }

        protected List<Assembly> KnownTypes { get; private set; } = new List<Assembly>();

        /// <summary>
        /// Adds known types to the TCP and UDP connection as soon
        /// as a connection has been established. This is not essential, but will speed up the initial time.
        /// Be aware that this method has te be called from the server and the clientConnectionContainer with the same parameter.
        /// Else the server or the client will crash, because of unknown types.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        public void AddKownType(Assembly assembly)
        {
            if (KnownTypes.Contains(assembly)) return;
            KnownTypes.Add(assembly);
        }

        /// <summary>
        /// Removes the known type from the init process.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        public void RemoveKnownType(Assembly assembly)
        {
            if (!KnownTypes.Contains(assembly)) return;
            KnownTypes.Remove(assembly);
        }
    }
}