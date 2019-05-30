using System;

namespace Network.Exceptions
{
    /// <summary>
    /// Indicates that a <see cref="Connection"/> isn't alive.
    /// Implements the <see cref="Exception" />
    /// </summary>
    /// <seealso cref="Exception" />
    public class ConnectionNotAliveException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionNotAliveException"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public ConnectionNotAliveException(Connection connection, string message = "", Exception exception = null) : base(message, exception)
        {
            Connection = connection;
        }

        /// <summary>
        /// The affected <see cref="Connection" />
        /// </summary>
        /// <value>The connection.</value>
        public Connection Connection { private set; get; }
    }
}
