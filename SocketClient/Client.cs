using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketClient
{
    /// <summary>
    ///   Client object
    /// </summary>
    class Client
    {
        private string name;

        public Client(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public Client(byte[] data)
        {
            int nameLength = BitConverter.ToInt32(data, 0);
            Name = Encoding.ASCII.GetString(data, 4, nameLength);
        }

        public byte[] ToByteArray()
        {
            List<byte> byteList = new List<byte>();

            byteList.AddRange(BitConverter.GetBytes(Name.Length));
            byteList.AddRange(Encoding.ASCII.GetBytes(Name));

            return byteList.ToArray();
        }
    }
}
