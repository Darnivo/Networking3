using System;
using shared;

namespace shared
{
    //server to client
    public class AvatarInfoMessage : ISerializable
    {
        public int Id;
        public int Skin;
        public float X, Y, Z;

        public void Serialize(Packet p)
        {
            p.Write(Id);
            p.Write(Skin);
            p.Write(X);
            p.Write(Y);
            p.Write(Z);
        }

        public void Deserialize(Packet p)
        {
            Id = p.ReadInt();
            Skin = p.ReadInt();
            X = p.ReadFloat();
            Y = p.ReadFloat();
            Z = p.ReadFloat();
        }
    }
}
