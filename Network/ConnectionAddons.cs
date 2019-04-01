﻿using Network.Interfaces;
using Network.Logging;
using System.IO;
using System.Runtime.InteropServices;

namespace Network
{
    /// <summary>
    /// Partial class which implements additional features for the <see cref="Connection"/> class.
    /// </summary>
    public abstract partial class Connection : IPacketHandler
    {
        #region Variables

        /// <summary>
        /// Whether the executing assembly is running on Windows.
        /// </summary>
        /// <remarks>
        /// Since an assembly cannot be transferred across an OS during runtime, this is a variable that can be set
        /// upon instantiation and it is valid for the lifetime of this <see cref="Connection"/> instance.
        /// </remarks>
        internal readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Whether the executing assembly is running on OSX.
        /// </summary>
        /// <remarks>
        /// Since an assembly cannot be transferred across an OS during runtime, this is a variable that can be set
        /// upon instantiation and it is valid for the lifetime of this <see cref="Connection"/> instance.
        /// </remarks>
        internal readonly bool IsOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Whether the executing assembly is running on Linux.
        /// </summary>
        /// <remarks>
        /// Since an assembly cannot be transferred across an OS during runtime, this is a variable that can be set
        /// upon instantiation and it is valid for the lifetime of this <see cref="Connection"/> instance.
        /// </remarks>
        internal readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        #endregion Variables

        #region Properties

        /// <summary>
        /// The <see cref="NetworkLog"/> instance to which information should be logged.
        /// </summary>
        internal NetworkLog Logger { get; private set; }

        /// <summary>
        /// Whether the <see cref="Connection"/> instance should automatically log information to the <see cref="Logger"/>s
        /// output <see cref="Stream"/>s.
        /// </summary>
        public bool EnableLogging
        {
            get { return Logger.EnableLogging; }
            set { Logger.EnableLogging = value; }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Initialises this <see cref="Connection"/> instance's addons, setting up all required variables.
        /// </summary>
        partial void InitialiseAddons()
        {
            Logger = new NetworkLog(this);
        }

        /// <summary>
        /// Logs events, exceptions and messages into the given stream. To disable logging into a previous provided stream, call
        /// this method again and provide a null reference as stream. Stream hot swapping is supported.
        /// </summary>
        /// <param name="stream">The stream to log into.</param>
        public void LogIntoStream(Stream stream) => Logger.SetOutputStream(stream);

        #endregion Methods
    }
}