namespace ShareCad.Networking
{
    public abstract class Packet
    {
        public PacketType PacketType { get; protected set; }

        public abstract void Parse();
        public abstract byte[] Serialize();
    }
}
