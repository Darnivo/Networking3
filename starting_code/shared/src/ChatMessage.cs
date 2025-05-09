using shared;

namespace shared
{   
    // two way
    public class ChatMessage : ISerializable {
        public int SenderId;
        public string Text;
        public void Serialize(Packet pPacket) {
            pPacket.Write(SenderId);
            pPacket.Write(Text);
        }
        public void Deserialize(Packet pPacket) {
            SenderId = pPacket.ReadInt();
            Text = pPacket.ReadString();
        }
    }
}
