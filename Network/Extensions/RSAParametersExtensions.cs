#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 29.09.2018
//
// Last Modified By : Thomas
// Last Modified On : 29.09.2018
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
using System.Security.Cryptography;

namespace Network.Extensions
{
    /// <summary>
    /// RSAParamters Extensions.
    /// </summary>
    internal static class RSAParametersExtensions
    {
        /// <summary>
        /// Extracts the private key.
        /// </summary>
        /// <param name="rsaParameter">The RSA parameter.</param>
        /// <returns>System.String.</returns>
        internal static string ExtractPrivateKey(this RSAParameters rsaParameter)
        {
            return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
                Convert.ToBase64String(rsaParameter.Modulus),
                Convert.ToBase64String(rsaParameter.Exponent),
                Convert.ToBase64String(rsaParameter.P),
                Convert.ToBase64String(rsaParameter.Q),
                Convert.ToBase64String(rsaParameter.DP),
                Convert.ToBase64String(rsaParameter.DQ),
                Convert.ToBase64String(rsaParameter.InverseQ),
                Convert.ToBase64String(rsaParameter.D));
        }

        /// <summary>
        /// Extracts the public key.
        /// </summary>
        /// <param name="rsaParameter">The RSA parameter.</param>
        /// <returns>System.String.</returns>
        internal static string ExtractPublicKey(this RSAParameters rsaParameter)
        {
            return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent></RSAKeyValue>",
                Convert.ToBase64String(rsaParameter.Modulus),
                Convert.ToBase64String(rsaParameter.Exponent));
        }
    }
}