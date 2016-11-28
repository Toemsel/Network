#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : NetworkTestClient
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

namespace NetworkTestClient
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("<1> Async example");
            Console.WriteLine("<2> Lambda example");
            Console.WriteLine("<3> Delegate example");
            Console.WriteLine("<4> Bluetooth example");
            Console.WriteLine("<5> Object driven example");
            Console.WriteLine("<6> TcpConnection only example");
            Console.WriteLine("<7> RawData example");
            Console.Write("> ");


            int input = 0;
            while (!int.TryParse(Console.ReadLine(), out input) || input < 1 || input > 7)
                Console.Write("> ");

            switch (input)
            {
                case 1:
                    new AsyncExample().Demo();
                    break;
                case 2:
                    new LambdaExample().Demo();
                    break;
                case 3:
                    new DelegateExample().Demo();
                    break;
                case 4:
                    new BluetoothExample().Demo();
                    break;
                case 5:
                    new ObjectExample().Demo();
                    break;
                case 6:
                    new SingleConnectionExample().Demo();
                    break;
                case 7:
                    new RawDataExample().Demo();
                    break;
                default:
                    throw new ArgumentException();
            }

            Console.ReadLine();
        }
    }
}
