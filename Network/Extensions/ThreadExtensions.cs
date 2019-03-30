using System.Threading;

namespace Network.Extensions
{
    /// <summary>
    /// Provides additional functionality to the <see cref="Thread"/>
    /// class, that isn't available under .NET Standard 2.0.
    /// </summary>
    internal static class ThreadExtensions
    {
        #region Methods

        /// <summary>
        /// Allows for a <see cref="Thread"/> object to be aborted in a program
        /// running under the .NET Standard C# implementation.
        /// </summary>
        /// <param name="thread">
        /// The <see cref="Thread"/> instance this extension method affects.
        /// </param>
        /// <returns>
        /// Whether the <see cref="Thread.Abort(object)"/> method raised an
        /// exception.
        /// </returns>
        public static bool AbortSave(this Thread thread)
        {
            try
            {
                thread.Abort();
                return false;
            }
            catch
            {
                return true;
            }
        }

        #endregion Methods
    }
}