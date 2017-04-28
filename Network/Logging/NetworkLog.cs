#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 11-23-2015
//
// Last Modified By : Thomas
// Last Modified On : 11-23-2015
// ***********************************************************************
// <copyright>
// Company: Indie-Dev
// Thomas Christof (c) 2015
// </copyright>
// <License>
// GNU LESSER GENERAL PUBLIC LICENSE
// </License>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// ***********************************************************************
#endregion Licence - LGPLv3
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Network.Enums;
using ConsoleTables;

namespace Network.Logging
{
    /// <summary>
    /// This class is in charge of logging network specific events and states
    /// into a given stream or dumping it onto the output window.
    /// </summary>
    internal class NetworkLog
    {
        /// <summary>
        /// We need the connection to retrieve specific connection information.
        /// </summary>
        private volatile Connection connection;

        internal NetworkLog(Connection connection)
        {
            this.connection = connection;
        }

        /// <summary>
        /// A property for a current timestamp.
        /// </summary>
        private string TimeStamp { get { return DateTime.Now.ToString("HH:mm:ss:fff"); } }

        /// <summary>
        /// Determins if we should enable logging or not.
        /// </summary>
        internal bool EnableLogging { get; set; } = false;

        /// <summary>
        /// The stream we are going to log into.
        /// </summary>
        private StreamWriter StreamLogger { get; set; }

        /// <summary>
        /// Enables to log into a costum stream.
        /// </summary>
        /// <param name="stream">The stream to log into.</param>
        internal void LogIntoStream(Stream stream)
        {
            StreamLogger = new StreamWriter(stream);
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="logLevel">The level of the log.</param>
        internal void Log(string message, LogLevel logLevel = LogLevel.Information)
        {
            Log(message, null, logLevel);
        }

        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="logLevel">The level of the log.</param>
        internal void Log(Exception exception, LogLevel logLevel = LogLevel.Information)
        {
            Log(string.Empty, exception, logLevel);
        }

        /// <summary>
        /// Logs a message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="logLevel">The log level.</param>
        internal void Log(string message, Exception exception, LogLevel logLevel = LogLevel.Information)
        {
            if(!EnableLogging)
                return;

            string finalLogMessage = BuildLogHeader(exception, logLevel);
            ConsoleTable tableOutput = new ConsoleTable("Type", "Local", "Message", "(Exception)");
            tableOutput.AddRow(connection.GetType().Name, message, connection.IPLocalEndPoint?.ToString(), exception?.ToString() ?? "NULL");
            finalLogMessage += tableOutput.ToMarkDownString();
            Log(finalLogMessage);
        }

        /// <summary>
        /// Logs receiving packets.
        /// </summary>
        /// <param name="packet">The receiving packet.</param>
        /// <param name="packetObj">The receiving object.</param>
        internal void LogInComingPacket(byte[] packet, Packet packetObj)
        {
            LogPacket(packet, packetObj, "Incoming");
        }

        /// <summary>
        /// Logs the sending packet.
        /// </summary>
        /// <param name="packet">The bytes of the packet.</param>
        /// <param name="packetObj">The packet to send.</param>
        internal void LogOutgoingPacket(byte[] packet, Packet packetObj)
        {
            LogPacket(packet, packetObj, "Outgoing");
        }

        private void LogPacket(byte[] packet, Packet packetObj, string direction)
        {
            var tableOutPut = BuildConsoleTable(packet, packetObj, direction);
            Log(tableOutPut.ToStringAlternative());
        }

        /// <summary>
        /// Builds the console table.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <param name="packetObj">The packet object.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>ConsoleTable.</returns>
        private ConsoleTable BuildConsoleTable(byte[] packet, Packet packetObj, string direction)
        {
            var type = connection.GetType().Name;
            var local = connection.IPLocalEndPoint?.ToString();
            var ascii = Encoding.ASCII.GetString(packet, 0, packet.Length).Replace("\0", "").Replace("\n", "").Replace("\r", "");
            var packetName = packetObj.GetType().Name.ToString();

            ConsoleTable tableOutPut = null;

            if (string.IsNullOrWhiteSpace(ascii))
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
        /// Creates the header for each log.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="logLevel">The log level.</param>
        /// <returns>A header.</returns>
        private string BuildLogHeader(Exception exception, LogLevel logLevel)
        {
            return $"[{TimeStamp}] {logLevel.ToString()} {exception?.Message} {Environment.NewLine}{Environment.NewLine}";
        }

        /// <summary>
        /// Writes everything to the desired stream.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void Log(string message)
        {
            Debug.WriteLine(message);
            StreamLogger?.WriteLine(message);
            StreamLogger?.Flush();
        }
    }
}
