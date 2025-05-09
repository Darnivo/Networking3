using shared;

namespace shared
{
    public class WhisperMessage : ISerializable
    {
        public int SenderId;
        public float SenderX, SenderY, SenderZ;
        public string Text;

        public void Serialize(Packet pPacket)
        {
            pPacket.Write(SenderId);
            pPacket.Write(SenderX);
            pPacket.Write(SenderY);
            pPacket.Write(SenderZ);
            pPacket.Write(Text);
        }

        public void Deserialize(Packet pPacket)
        {
            SenderId = pPacket.ReadInt();
            SenderX = pPacket.ReadFloat();
            SenderY = pPacket.ReadFloat();
            SenderZ = pPacket.ReadFloat();
            Text = pPacket.ReadString();
        }
    }
}