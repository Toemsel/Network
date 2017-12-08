#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 12-03-2017
//
// Last Modified By : Thomas
// Last Modified On : 12-03-2017
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
using Network.Attributes;
using Network.Enums;
using Network.Reactive;

namespace Network.Packets
{
    [PacketType(12)]
    internal sealed class AddReactiveObject : ReactivePacket
    {
        public AddReactiveObject() { }

        public AddReactiveObject(ReactiveObject reactiveObject)
        {
            ReactiveObject = reactiveObject;
        }

        public string ReactiveObjectType { get; set; }

        public string AssemblyName { get; set; }

        public ReactiveObject ReactiveObject { get; set; }

        public override void BeforeSend()
        {
            AssemblyName = ReactiveObject.GetType().Assembly.FullName;
            ReactiveObjectType = ReactiveObject.GetType().FullName;
        }
    }
}