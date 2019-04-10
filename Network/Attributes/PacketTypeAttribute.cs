using Network.Packets;
using Network.Packets.RSA;
using System;

namespace Network.Attributes
{
    /// <summary>
    /// To identify every packet server and client side, a unique identifier is needed. Mark every packet class with this
    /// attribute and set a unique id (UInt16). 2^16 (65536) unique ids are possible. Double usage of one id will lead to an exception.
    /// <list type="table">
    /// <listheader> <description>Following ids are already taken by the network lib:</description> </listheader>
    /// <item> <term>00</term> <description><see cref="PingRequest"/>               </description>  </item>
    /// <item> <term>01</term> <description><see cref="PingResponse"/>              </description>  </item>
    /// <item> <term>02</term> <description><see cref="CloseRequest"/>              </description>  </item>
    /// <item> <term>03</term> <description><see cref="EstablishUdpRequest"/>       </description>  </item>
    /// <item> <term>04</term> <description><see cref="EstablishUdpResponse"/>      </description>  </item>
    /// <item> <term>05</term> <description><see cref="EstablishUdpResponseACK"/>   </description>  </item>
    /// <item> <term>06</term> <description><see cref="AddPacketTypeRequest"/>      </description>  </item>
    /// <item> <term>07</term> <description><see cref="AddPacketTypeResponse"/>     </description>  </item>
    /// <item> <term>08</term> <description><see cref="UDPPingRequest"/>            </description>  </item>
    /// <item> <term>09</term> <description><see cref="UDPPingResponse"/>           </description>  </item>
    /// <item> <term>10</term> <description><see cref="RawData"/>                   </description>  </item>
    /// <item> <term>11</term> <description><see cref="RSAKeyInformationRequest"/>  </description>  </item>
    /// <item> <term>12</term> <description><see cref="RSAKeyInformationResponse"/> </description>  </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Knowledge about the ID isn't essential anymore (Since version 2.0.0.0). However, the above IDs should NOT be
    /// overwritten, for compatibility purposes.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class PacketTypeAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Constructs and returns a new instance of the <see cref="PacketTypeAttribute"/> class, with the given ID to
        /// be used for the decorated <see cref="Packet"/>.
        /// </summary>
        /// <param name="packetType">The ID to use for the decorated <see cref="Packet"/>.</param>
        public PacketTypeAttribute(ushort packetType)
        {
            Id = packetType;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The ID to use for the decorated <see cref="Packet"/>.
        /// </summary>
        public ushort Id { get; }

        #endregion Properties
    }
}