using shared;

namespace shared
{
    public class AvatarMoveRequest : ISerializable
    {
        public float X, Y, Z;

        public void Serialize(Packet pPacket)
        {
            pPacket.Write(X);
            pPacket.Write(Y);
            pPacket.Write(Z);
        }

        public void Deserialize(Packet pPacket)
        {
            X = pPacket.ReadFloat();
            Y = pPacket.ReadFloat();
            Z = pPacket.ReadFloat();
        }
    }
}