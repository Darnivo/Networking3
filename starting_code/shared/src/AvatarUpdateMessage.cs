using System.Collections.Generic;
using shared;

namespace shared
{   
    //server to client
    public class AvatarUpdateMessage : ISerializable 
    {
        public List<AvatarInfoMessage> Avatars = new List<AvatarInfoMessage>();
        public void Serialize(Packet p) 
        {
            p.Write(Avatars.Count);
            foreach (var avatar in Avatars) avatar.Serialize(p);
        }

        public void Deserialize(Packet p) 
        {
            int count = p.ReadInt();
            for (int i = 0; i < count; i++) 
            {
                var avatar = new AvatarInfoMessage();
                avatar.Deserialize(p);
                Avatars.Add(avatar);
            }
        }
    }
}
