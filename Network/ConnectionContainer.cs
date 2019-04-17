using Network.Interfaces;
using Network.RSA;
using System;
using System.Collections.Generic;
using System.Reflection;
using Network.Packets;

namespace Network
{
    /// <summary>
    /// Holds a <see cref="Connection"/> instance and provides additional functionality. Holds <see cref="TcpConnection"/>s
    /// and <see cref="UdpConnection"/>s. Base class for all other connection containers. Provides the basic methods that all
    /// <see cref="ConnectionContainer"/> inheritors must implement.
    /// </summary>
    public abstract class ConnectionContainer : IRSACapability
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionContainer"/> class.
        /// </summary>
        /// <param name="ipAddress">The remote ip address.</param>
        /// <param name="port">The remote port.</param>
        protected ConnectionContainer(string ipAddress, int port)
        {
            IPAddress = ipAddress;
            Port = port;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The IP address of the remote <see cref="Connection"/> that this container is connected to.
        /// </summary>
        public string IPAddress { get; protected set; }

        /// <summary>
        /// The port of the remote <see cref="Connection"/> that this container is connected to.
        /// </summary>
        public int Port { get; protected set; }

        /// <summary>
        /// The public RSA key for this <see cref="ConnectionContainer"/> instance.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public string PublicKey => RSAPair?.Public;

        /// <summary>
        /// The private RSA key for this <see cref="ConnectionContainer"/> instance.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public string PrivateKey => RSAPair?.Private;

        /// <summary>
        /// The size of the RSA keys for this <see cref="ConnectionContainer"/> instance.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public int KeySize => RSAPair?.KeySize ?? -1;

        /// <summary>
        /// The RSA key-pair that is used for encryption and decryption of encrypted messages.
        /// </summary>
        public RSAPair RSAPair { get; set; }

        /// <summary>
        /// Holds all the <see cref="Assembly"/>s that are known by this <see cref="ConnectionContainer"/>.
        /// </summary>
        protected List<Assembly> KnownTypes { get; private set; } = new List<Assembly>();

        #endregion Properties

        #region Methods

        //TODO Fix spelling mistake?

        /// <summary>
        /// Adds the given <see cref="Assembly"/> to the list of assemblies whose <see cref="Packet"/>s to register upon
        /// establishing a connection. This is not essential, but can speed up performance if a lot of <see cref="Packet"/>s
        /// must be registered on each connection (these are found using reflection). NOTE: To avoid incompatible states between
        /// the server (<see cref="ServerConnectionContainer"/>) and client (<see cref="ClientConnectionContainer"/>), this method
        /// must be called on both sides before a connection is established.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> whose <see cref="Packet"/>s to add.</param>
        public void AddKownType(Assembly assembly)
        {
            if (KnownTypes.Contains(assembly)) return;
            KnownTypes.Add(assembly);
        }

        /// <summary>
        /// Removes the given <see cref="Assembly"/> from the list of assemblies whose <see cref="Packet"/>s to register upon
        /// establishing a connection.NOTE: To avoid incompatible states between the server (<see cref="ServerConnectionContainer"/>)
        /// and client (<see cref="ClientConnectionContainer"/>), this method must be called on both sides before a connection is established.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> whose <see cref="Packet"/>s to remove.</param>
        public void RemoveKnownType(Assembly assembly)
        {
            if (!KnownTypes.Contains(assembly)) return;
            KnownTypes.Remove(assembly);
        }

        #endregion Methods
    }
}