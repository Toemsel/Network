namespace Network.Enums
{
    /// <summary>
    /// Enumerates the possible reasons for a <see cref="Connection"/> closing.
    /// </summary>
    public enum CloseReason
    {
        /// <summary>
        /// An unknown exception occurred in the network library.
        /// </summary>
        NetworkError = 0,

        /// <summary>
        /// The server closed the connection.
        /// </summary>
        ServerClosed = 1,

        /// <summary>
        /// The client closed the connection.
        /// </summary>
        ClientClosed = 2,

        /// <summary>
        /// The endpoint sent an unknown packet which cant be processed.
        /// </summary>
        UnknownPacket = 3,

        /// <summary>
        /// Connection timeout reached.
        /// </summary>
        Timeout = 4,

        /// <summary>
        /// The endpoints version is different.
        /// </summary>
        DifferentVersion = 5,

        /// <summary>
        /// UDP connection requested in an improper situation.
        /// </summary>
        InvalidUdpRequest = 6,

        /// <summary>
        /// The client requested too many UDP connections.
        /// </summary>
        UdpLimitExceeded = 7,

        /// <summary>
        /// An exception in the writePacketThread occured.
        /// Indicates, that the endpoint couldn't be reached and is offline.
        /// </summary>
        WritePacketThreadException = 9,

        /// <summary>
        /// An exception in the readPacketThread occured.
        /// </summary>
        ReadPacketThreadException = 10,

        /// <summary>
        /// An exception in the invokePacketThread occured.
        /// Indicates, that an exception has been thrown in the packet-handling subscriber.
        /// </summary>
        InvokePacketThreadException = 11,

        /// <summary>
        /// The assembly for the incoming packet is not available. Make sure that every project is including that assembly.
        /// </summary>
        AssemblyDoesNotExist = 12
    }
}