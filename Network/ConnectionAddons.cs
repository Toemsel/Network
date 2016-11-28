using System;
using System.IO;
using System.Text;
using Network.Interfaces;
using Network.Logging;
using Network.Packets;

namespace Network
{
    /// <summary>
    /// Partial class for the connection to offer some additional features.
    /// </summary>
    public abstract partial class Connection : IPacketHandler
    {
        private NetworkLog logger;

        /// <summary>
        /// Logger for the connection and all protected classes.
        /// </summary>
        internal NetworkLog Logger { get { return logger; } }

        /// <summary>
        /// Initializes all the addons.
        /// </summary>
        private void InitAddons()
        {
            logger = new NetworkLog(this);
        }

        /// <summary>
        /// Logs events, exceptions and messages into the given stream.
        /// To disable logging into a previous provided stream, call this method again
        /// and provide a null reference as stream. Stream hot swapping is supported.
        /// </summary>
        /// <param name="stream">The stream to log into.</param>
        public void LogIntoStream(Stream stream)
        {
            logger.LogIntoStream(stream);
        }        

        /// <summary>
        /// Indicates if the connection should automatically log.
        /// Logging in DEBUG mode by default ON.
        /// </summary>
        public bool EnableLogging
        {
            get { return logger.EnableLogging; }
            set { logger.EnableLogging = value; }
        }

        /// <summary>
        /// Sends raw data.
        /// </summary>
        /// <param name="key">The identifying key.</param>
        /// <param name="data">The data to send.</param>
        public void SendRawData(string key, byte[] data)
        {
            if(data == null)
            {
                logger.Log("Can't send a null reference data byte array", new ArgumentException(), Enums.LogLevel.Information);
                return;
            }

            Send(new RawData(key, data));
        }

        /// <summary>
        /// Sends a raw data packet.
        /// </summary>
        /// <param name="rawData">The packet to send.</param>
        public void SendRawData(RawData rawData)
        {
            Send(rawData);
        }
    }
}
