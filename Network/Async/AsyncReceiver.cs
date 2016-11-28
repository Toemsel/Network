#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas Christof
// Created          : 02-11-2016
//
// Last Modified By : Thomas Christof
// Last Modified On : 10-10-2015
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
using System.Threading;
using System.Threading.Tasks;
using Network.Extensions;
using Network.Interfaces;
using Network.Packets;

namespace Network.Async
{
    /// <summary>
    /// Sends and receives a packet async.
    /// </summary>
    internal class AsyncReceiver : IDisposable
    {
        /// <summary>
        /// The packet we actually received.
        /// </summary>
        private Packet receivedAsyncPacket = null;
        /// <summary>
        /// A manualResetEvent to let the instance know that the packet arrived.
        /// </summary>
        private ManualResetEvent packetReceivedEvent = new ManualResetEvent(false);

        /// <summary>
        /// Sends the specified packet.
        /// </summary>
        /// <typeparam name="T">The packet we would like to receive.</typeparam>
        /// <param name="packet">The packet.</param>
        /// <param name="connection">The connection.</param>
        /// <returns>Task&lt;T&gt;.</returns>
        internal async Task<T> Send<T>(Packet packet, Connection connection) where T : ResponsePacket
        {
            object tempObject = new object();

            //Register the packet we would like to receive.
            connection.RegisterPacketHandler<T>(((packetAnswer, c) =>
            {
                receivedAsyncPacket = packetAnswer;
                c.UnRegisterPacketHandler<T>(tempObject);
                packetReceivedEvent.Set();
            }), tempObject);

            //Send the packet normally.
            connection.Send(packet, tempObject);

            //Wait for an answer or till we reach the timeout.
            try
            {
                if (receivedAsyncPacket == null)
                    await packetReceivedEvent.AsTask(TimeSpan.FromMilliseconds(connection.TIMEOUT));
            }
            catch { }

            //No answer from the endPoint
            if (receivedAsyncPacket == null)
            {
                T emptyPacket = Activator.CreateInstance<T>();
                emptyPacket.State = Enums.PacketState.Timeout;
                return emptyPacket;
            }

            return (T)receivedAsyncPacket;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            packetReceivedEvent.Dispose();
        }
    }
}
