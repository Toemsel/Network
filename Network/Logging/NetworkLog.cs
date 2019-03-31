using ConsoleTables;
using Network.Enums;
using Network.Packets;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Network.Logging
{
    /// <summary>
    /// Enumerates the directions that a <see cref="Packet"/> can be traveling on the network.
    /// </summary>
    internal enum PacketDirection
    {
        /// <summary>
        /// The packet is incoming from the network; it is being received by the monitored <see cref="Connection"/>.
        /// </summary>
        Incoming,

        /// <summary>
        /// The packet is outgoing to the network; it is being transmitted by the monitored <see cref="Connection"/>.
        /// </summary>
        Outgoing
    }

    /// <summary>
    /// Logs network traffic, events and connection states into a given <see cref="Stream"/>, be it a <see cref="FileStream"/> or the 'Output' window of Visual Studio.
    /// </summary>
    internal class NetworkLog
    {
        #region Variables

        /// <summary>
        /// The <see cref="Connection"/> that the <see cref="NetworkLog"/> instance will monitor for network traffic, etc.
        /// </summary>
        private readonly Connection monitoredConnection;

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Constructs and returns a new instance of the <see cref="NetworkLog"/> class, that monitors the given <see cref="Connection"/>.
        /// </summary>
        /// <param name="connection">The <see cref="Connection"/> that the <see cref="NetworkLog"/> should monitor for traffic, events and states.</param>
        internal NetworkLog(Connection connection)
        {
            monitoredConnection = connection;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Whether logging is enabled.
        /// </summary>
        internal bool EnableLogging { get; set; } = false;

        /// <summary>
        /// The current timestamp, in the format 'hh:mm:ss:fff'.
        /// </summary>
        internal string TimeStamp
        {
            get { return DateTime.Now.ToString("HH:mm:ss:fff"); }
        }

        /// <summary>
        /// The <see cref="StreamWriter"/> that writes all logged to the current output <see cref="Stream"/>.
        /// </summary>
        private StreamWriter StreamLogger { get; set; }

        #endregion Properties

        #region Methods

        #region Logging A Message

        /// <summary>
        /// WRites the given message to the current <see cref="StreamLogger"/>, and to the 'Output' window.
        /// </summary>
        /// <param name="message">The message that should be logged.</param>
        private void LogToAllOutputs(string message)
        {
            Trace.WriteLine(message);
            StreamLogger?.WriteLine(message);
            StreamLogger?.Flush();
        }

        /// <summary>
        /// Logs the given <see cref="string"/> message and <see cref="Exception"/>, with the given <see cref="Enums.LogLevel"/> to all output <see cref="Stream"/>s.
        /// </summary>
        /// <param name="message">The message to log to the output <see cref="Stream"/>s.</param>
        /// <param name="exception">The <see cref="Exception"/> to log to the output <see cref="Stream"/>s.</param>
        /// <param name="logLevel">The <see cref="Enums.LogLevel"/> of the log message.</param>
        /// <remarks>
        /// If <see cref="EnableLogging"/> if set to <c>false</c> or the <see cref="StreamLogger"/> is <c>null</c>, then no message is logged.
        /// </remarks>
        internal void Log(string message, Exception exception, LogLevel logLevel = LogLevel.Information)
        {
            if (!EnableLogging || StreamLogger == null)
                return;

            string finalLogMessage = BuildLogHeader(exception, logLevel);

            string[] tableColumnHeaders =
            {
                "Type", "Local", "Message", "(Exception)"
            };

            object[] tableRowContent =
            {
                monitoredConnection.GetType().Name,
                message,
                monitoredConnection.IPLocalEndPoint?.ToString(),
                BuildException(exception)
            };

            if (exception == null)
            {
                tableColumnHeaders = tableColumnHeaders
                    .Take(tableColumnHeaders.Length - 1)
                    .ToArray();

                tableRowContent = tableRowContent
                    .Take(tableRowContent.Length - 1)
                    .ToArray();
            }

            ConsoleTable tableOutput = new ConsoleTable(tableColumnHeaders);
            tableOutput.AddRow(tableRowContent);
            finalLogMessage += tableOutput.ToMarkDownString();
            LogToAllOutputs(finalLogMessage);
        }

        /// <summary>
        /// Logs the given <see cref="Exception"/> with the given <see cref="Enums.LogLevel"/> to the output <see cref="Stream"/>s.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to log to the output <see cref="Stream"/>s.</param>
        /// <param name="logLevel">The <see cref="Network.Enums.LogLevel"/> of the log message.</param>
        internal void Log(Exception exception, LogLevel logLevel = LogLevel.Information) => Log(string.Empty, exception, logLevel);

        /// <summary>
        /// Logs the given <see cref="string"/> message with the given <see cref="Enums.LogLevel"/> to the output <see cref="Stream"/>s.
        /// </summary>
        /// <param name="message">The message to log to the output <see cref="Stream"/>s.</param>
        /// <param name="logLevel">The <see cref="Network.Enums.LogLevel"/> of the log message.</param>
        internal void Log(string message, LogLevel logLevel = LogLevel.Information) => Log(message, null, logLevel);

        #endregion Logging A Message

        /// <summary>
        /// Sets the output <see cref="Stream"/> (<see cref="StreamLogger"/>) to the given <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to log messages into.</param>
        internal void SetOutputStream(Stream stream) => StreamLogger = new StreamWriter(stream);

        /// <summary>
        /// Logs an incoming packet to the output <see cref="Stream"/>s.
        /// </summary>
        /// <param name="packet">The serialised incoming packet.</param>
        /// <param name="packetObj">The incoming <see cref="Packet"/> object.</param>
        internal void LogInComingPacket(byte[] packet, Packet packetObj) => LogPacket(packet, packetObj, PacketDirection.Incoming);

        /// <summary>
        /// Logs an outgoing packet to the output <see cref="Stream"/>s.
        /// </summary>
        /// <param name="packet">The serialised outgoing packet.</param>
        /// <param name="packetObj">The outgoing <see cref="Packet"/> object.</param>
        internal void LogOutgoingPacket(byte[] packet, Packet packetObj) => LogPacket(packet, packetObj, PacketDirection.Outgoing);

        /// <summary>
        /// Logs the given packet to the output <see cref="Stream"/>s, along with its direction.
        /// </summary>
        /// <param name="packet">The serialised packet.</param>
        /// <param name="packetObj">The <see cref="Packet"/> object.</param>
        /// <param name="direction">The direction that the packet is traveling across the network.</param>
        private void LogPacket(byte[] packet, Packet packetObj, PacketDirection direction)
        {
            if (!EnableLogging)
                return;

            ConsoleTable tableOutPut = BuildConsoleTable(packet, packetObj, direction.ToString());

            LogToAllOutputs(tableOutPut.ToStringAlternative());
        }

        /// <summary>
        /// Builds a <see cref="ConsoleTable"/> with the given parameters and returns it.
        /// </summary>
        /// <param name="packet">The serialised packet.</param>
        /// <param name="packetObj">The <see cref="Packet"/> object.</param>
        /// <param name="direction"> The direction that the packet is traveling across the network.</param>
        /// <returns>The built <see cref="ConsoleTable"/>.</returns>
        private ConsoleTable BuildConsoleTable(byte[] packet, Packet packetObj, string direction)
        {
            object type = monitoredConnection.GetType().Name;
            object local = monitoredConnection.IPLocalEndPoint?.ToString();
            object ascii = Encoding.ASCII.GetString(packet, 0, packet.Length).Replace("\0", "").Replace("\n", "").Replace("\r", "");
            object packetName = packetObj.GetType().Name;

            ConsoleTable tableOutPut;

            if (string.IsNullOrWhiteSpace((string)ascii))
            {
                tableOutPut = new ConsoleTable("Direction", "Type", "Local", "Packet");
                tableOutPut.AddRow(direction, type, local, packetName);
            }
            else
            {
                tableOutPut = new ConsoleTable("Direction", "Type", "Local", "ASCII", "Packet");
                tableOutPut.AddRow(direction, type, local, ascii, packetName);
            }

            return tableOutPut;
        }

        /// <summary>
        /// Builds and returns the header for each log message.
        /// </summary>
        /// <param name="exception">The <see cref=" Exception"/> to log.</param>
        /// <param name="logLevel">The <see cref="Network.Enums.LogLevel"/> for the log message.</param>
        /// <returns>The formatted log message header <see cref="string"/>.</returns>
        private string BuildLogHeader(Exception exception, LogLevel logLevel) =>
            $"[{TimeStamp}] {logLevel.ToString()} {exception?.Message} {Environment.NewLine}{Environment.NewLine}";

        /// <summary>
        /// Builds and returns a <see cref="string"/> message containing an <see cref="Exception"/>.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to format as a <see cref="string"/>.</param>
        /// <returns>The <see cref="Exception"/> formatted as a <see cref="string"/> message.</returns>
        private string BuildException(Exception exception)
        {
            StringBuilder exceptionBuilder =
                new StringBuilder(exception?.ToString());

            exception = exception?.InnerException;

            while (exception != null)
            {
                exceptionBuilder.AppendLine(exception.ToString());
                exception = exception.InnerException;
            }

            return exceptionBuilder.ToString();
        }

        #endregion Methods
    }
}