namespace Network.Enums
{
    /// <summary>
    /// The higher the value, the faster the network library.
    /// A faster connection results in more CPU consumption.
    /// Sofrt_Realtime > Default > Fast > Normal > Slow > Energy_Saving.
    /// </summary>
    public enum Performance : int
    {
        /// <summary>
        /// Sleep intervals of more than 500ms.
        /// </summary>
        Energy_Saving = 500,
        /// <summary>
        /// Sleep intervals of more than 100ms.
        /// </summary>
        Slow = 100,
        /// <summary>
        /// Sleep interval of more than 25ms.
        /// </summary>
        Normal = 25,
        /// <summary>
        /// Sleep interval of more than 10ms.
        /// </summary>
        Fast = 10,
        /// <summary>
        /// Sleep interval of more than 5ms.
        /// This is the default value, recommended for usage.
        /// </summary>
        Default = 5,
        /// <summary>
        /// Sleep interval of more than 1ms.
        /// </summary>
        Soft_Realtime = 1
    }
}
