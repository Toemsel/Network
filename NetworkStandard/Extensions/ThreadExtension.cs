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
using System.Threading;

namespace Network.Extensions
{
    /// <summary>
    /// Offers some nice extensions for a thread instance.
    /// </summary>
    internal static class ThreadExtension
    {
        /// <summary>
        /// Aborts a thread and catches all the exceptions if some occurs.
        /// </summary>
        /// <param name="thread">The thread to abort.</param>
        /// <returns>If an exception occured.</returns>
        internal static bool AbortSave(this Thread thread)
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
    }
}
