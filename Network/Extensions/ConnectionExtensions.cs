using System.Threading;

namespace Network.Extensions
{
    /// <summary>
    /// Provides additional functionality to the <see cref="Connection"/> class.
    /// </summary>
    internal static class ConnectionExtensions
    {
        #region Variables

        /// <summary>
        /// A private thread-safe counter for generating unique hash codes.
        /// </summary>
        /// <remarks>
        /// Increments are guaranteed to be atomic on all 32-bit and higher systems, as any single-cpu-instruction operation on a variable is
        /// by definition atomic. Since an <see cref="int"/> is 32 bits long, it can be loaded with 1 instruction into a register on a 32-bit or
        /// higher system. Likewise, addition is also atomic. This guarantees atomic behaviour for increments on an <see cref="int"/>.
        /// </remarks>
        private static int counter;

        #endregion Variables

        #region Methods

        /// <summary>
        /// Generates a new unique hash code for the <see cref="Connection"/> via a thread-safe increment operation.
        /// </summary>
        /// <param name="connection">The <see cref="Connection"/> instance this extension method affects.</param>
        /// <returns>A new, unique hash code.</returns>
        /// <remarks>This method is thread safe, see <see cref="counter"/> for more info.</remarks>
        internal static int GenerateUniqueHashCode(this Connection connection)
        {
            return Interlocked.Increment(ref counter);
        }

        #endregion Methods
    }
}