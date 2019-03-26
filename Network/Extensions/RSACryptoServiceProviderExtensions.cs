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
    /// Provides additional functionality to the <see cref="RSACryptoServiceProvider"/>
    /// class, that is unavailable under Linux by default.
    /// </summary>
    internal static class RSACryptoServiceProviderExtensions
    {
        #region Methods

        /// <summary>
        /// Reads in and imports <see cref="RSAParameters"/> from the given XML
        /// string.
        /// </summary>
        /// <param name="rsaCryptoServiceProvider">
        /// The <see cref="RSACryptoServiceProvider"/> this extension method
        /// affects.
        /// </param>
        /// <param name="xml">
        /// The XML string from which to load the parameters.
        /// </param>
        public static void ImportParametersFromXmlString(
            this RSACryptoServiceProvider rsaCryptoServiceProvider, string xml)
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
        /// Decodes a base-64 encoded string into a byte array and returns it.
        /// </summary>
        /// <param name="text">
        /// The base-64 encoded string.
        /// </param>
        /// <returns>
        /// The byte array that the base-64 encoded text represents.
        /// </returns>
        private static byte[] ConvertFromBase64String(string text) =>
            string.IsNullOrWhiteSpace(text) ? null : Convert.FromBase64String(text);

        #endregion Methods
    }
}