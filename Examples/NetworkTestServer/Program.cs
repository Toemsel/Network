#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : NetworkTestClient
// Author           : Thomas Christof
// Created          : 02-11-2016
//
// Last Modified By : Thomas Christof
// Last Modified On : 27-08-2018
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

using System;

namespace NetworkTestServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            int input = 0;

            Console.WriteLine("<1> Secure-Server example. (Requires selection <8> in the client)");
            Console.WriteLine("<2> UnSecure-Server example. (Working with every other example)");
            Console.WriteLine("<3> Exit");
            Console.Write("> ");

            input = 0;
            while (!int.TryParse(Console.ReadLine(), out input) || input < 1 || input > 2)
                Console.Write("> ");

            switch (input)
            {
                case 1:
                    new SecureServerExample().Demo();
                    break;

                case 2:
                    new UnSecureServerExample().Demo();
                    break;

                default:
                    throw new ArgumentException();
            }
        }
    }
}