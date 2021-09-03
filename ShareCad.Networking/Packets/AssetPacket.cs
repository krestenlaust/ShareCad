using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ShareCad.Networking.Packets
{
    public abstract class AssetPacket : Packet
    {
        public int ID { get; private set; }

        /// <summary>
        /// Read general asset properties.
        /// </summary>
        /// <param name="stream"></param>
        public AssetPacket(Stream stream)
        {
            // deserialize ID
            byte[] idBytes = new byte[sizeof(int)];
            stream.Read(idBytes, 0, idBytes.Length);
            ID = BitConverter.ToInt32(idBytes, 0);
        }

        /// <summary>
        /// Assign general asset properties.
        /// </summary>
        /// <param name="id"></param>
        public AssetPacket(int id)
        {
            ID = id;
        }

        /// <summary>
        /// Serializes asset properties (ID).
        /// </summary>
        /// <returns></returns>
        public override byte[] Serialize()
        {
            return BitConverter.GetBytes(ID);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
    }
}
