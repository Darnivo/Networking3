using shared;

namespace shared
{   
    //client to server
    public class ClientJoinRequest : ISerializable 
    {
    public void Serialize(Packet p) {} // No data needed
    public void Deserialize(Packet p) {}
    }
}
