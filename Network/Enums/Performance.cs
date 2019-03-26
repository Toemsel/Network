namespace Network.Enums
{
    /// <summary>
    /// Enumerates the possible values for sleep intervals.
    /// Fastest Performance >> Slowest Performance :
    /// SoftRealtime        >> EnergySaving
    /// </summary>
    public enum Performance : int
    {
        /// <summary>
        /// Sleep intervals of more than 500ms.
        /// </summary>
        EnergySaving = 500,

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
        SoftRealtime = 1
    }
}