#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 07-23-2015
//
// Last Modified By : Thomas
// Last Modified On : 08-05-2015
// ***********************************************************************
// <copyright>
// Company: Indie-Dev
// Thomas Christof (c) 2018
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
namespace Network.Enums
{
    /// <summary>
    /// Contains some reasons why the connection has been or should be closed.
    /// </summary>
    public enum CloseReason : int
    {
        /// <summary>
        /// An unknown exception occurred in the network library
        /// </summary>
        NetworkError = 0,
        /// <summary>
        /// The server closed the connection
        /// </summary>
        ServerClosed = 1,
        /// <summary>
        /// The client closed the connection
        /// </summary>
        ClientClosed = 2,
        /// <summary>
        /// The endpoint sent an unknown packet which cant be processed
        /// </summary>
        UnknownPacket = 3,
        /// <summary>
        /// Connection timeout reached
        /// </summary>
        Timeout = 4,
        /// <summary>
        /// The endpoints version is different.
        /// </summary>
        DifferentVersion = 5,
        /// <summary>
        /// UDP connection requested in an improper situation.
        /// </summary>
        InvalidUdpRequest = 6,
        /// <summary>
        /// The client requested too many UDP connections.
        /// </summary>
        UdpLimitExceeded = 7,
        /// <summary>
        /// An internal handled exception.
        /// </summary>
        InternalException = 8,
        /// <summary>
        /// An exception in the writePacketThread occured.
        /// </summary>
        WritePacketThreadException = 9,
        /// <summary>
        /// An exception in the readPacketThread occured.
        /// </summary>
        ReadPacketThreadException = 10,
        /// <summary>
        /// An exception in the invokePacketThread occured.
        /// </summary>
        InvokePacketThreadException = 11,
        /// <summary>
        /// The assembly for the incomming packet is not available.
        /// Make sure that every project is including that assembly.
        /// </summary>
        AssemblyDoesNotExist = 12
    }
}
