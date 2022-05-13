namespace OmniVision_APIserver;

[Serializable]
public class Boller
{
    public int id;
    public string[] properties = new string[4];
    public string ip;

    public Boller(int ids, string name, string city, string street, string bulding, string ips)
    {
        id = ids;
        properties[0] = name;
        properties[0] = city;
        properties[1] = street;
        properties[2] = bulding;
        ip = ips;
    }
    public Boller()
    {
        id = -1;
        properties[0] = "City";
        properties[1] = "Street";
        properties[2] = "Bulding";
        ip = "0.0.0.0";
    }

    public override string ToString()
    {
        return string.Format("Index: {0}, City: {1}, Street: {2}, Build: {3}, ip-adress: {4}", 
            id, properties[0], properties[1], properties[2], ip);
    }
}