#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 10-21-2018
//
// Last Modified By : Thomas
// Last Modified On : 10-21-2018
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
using System.Xml;

namespace Network.Extensions
{
    /// <summary>
    /// Due the lack of implementation under Linux, we need to implement some basic functions by ourself.
    /// </summary>
    internal static class RSACryptoServiceProviderExtensions
    {
        /// <summary>
        /// Converts a string, containing a valid XML, to a RSA Parameter.
        /// </summary>
        /// <param name="xml">The XML to convert.</param>
        /// <returns><see cref="RSAParameters" /></returns>
        internal static void FromXmlString(this RSACryptoServiceProvider rsaCryptoServiceProvider, string xml)
        {
            RSAParameters rsaParameter = new RSAParameters();

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            foreach (XmlNode currentNode in xmlDocument.DocumentElement.ChildNodes)
            {
                switch (currentNode.Name)
                {
                    case "Modulus":
                        rsaParameter.Modulus = ConvertFromBase64String(currentNode.InnerText);
                        break;

                    case "Exponent":
                        rsaParameter.Exponent = ConvertFromBase64String(currentNode.InnerText);
                        break;

                    case "P":
                        rsaParameter.P = ConvertFromBase64String(currentNode.InnerText);
                        break;

                    case "Q":
                        rsaParameter.Q = ConvertFromBase64String(currentNode.InnerText);
                        break;

                    case "DP":
                        rsaParameter.DP = ConvertFromBase64String(currentNode.InnerText);
                        break;

                    case "DQ":
                        rsaParameter.DQ = ConvertFromBase64String(currentNode.InnerText);
                        break;

                    case "InverseQ":
                        rsaParameter.InverseQ = ConvertFromBase64String(currentNode.InnerText);
                        break;

                    case "D":
                        rsaParameter.D = ConvertFromBase64String(currentNode.InnerText);
                        break;
                }
            }

            rsaCryptoServiceProvider.ImportParameters(rsaParameter);
        }

        /// <summary>
        /// Decodes a base64 string.
        /// </summary>
        /// <param name="text">The text to decode.</param>
        /// <returns>Decoded text in bytes.</returns>
        private static byte[] ConvertFromBase64String(string text) => string.IsNullOrWhiteSpace(text) ? null : Convert.FromBase64String(text);
    }
}