using shared;

namespace shared
{
    // Client to server
    public class ChangeSkinRequest : ISerializable
    {
        public int NewSkin;

        public void Serialize(Packet pPacket)
        {
            pPacket.Write(NewSkin);
        }

        public void Deserialize(Packet pPacket)
        {
            NewSkin = pPacket.ReadInt();
        }
    }
}