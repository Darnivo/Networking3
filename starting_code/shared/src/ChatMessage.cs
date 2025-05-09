using shared;

namespace shared
{   
    // two way
    public class ChatMessage : ISerializable {
        public int SenderId;
        public string Text;
        public bool IsBroadcast = true;
        public void Serialize(Packet pPacket) {
            pPacket.Write(SenderId);
            pPacket.Write(Text);
            pPacket.Write(IsBroadcast);
        }
        public void Deserialize(Packet pPacket) {
            SenderId = pPacket.ReadInt();
            Text = pPacket.ReadString();
            IsBroadcast = pPacket.ReadBool();
        }
    }
}
