using Network.RSA;

namespace Network.Interfaces
{
    /// <summary>
    /// Describes the properties and methods that a class must implement to be capable of RSA encryption.
    /// </summary>
    internal interface IRSACapability
    {
        #region Properties

        /// <summary>
        /// Stores a RSA private and public key pair.
        /// </summary>
        RSAPair RSAPair { get; set; }

        #endregion Properties
    }
}