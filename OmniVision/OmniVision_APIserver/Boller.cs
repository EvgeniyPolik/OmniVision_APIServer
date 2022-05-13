namespace OmniVision_APIserver;

[Serializable]
public class Boller
{
    public int Id;
    public string[] Properties = new string[4];
    public string Ip;

    public Boller(int ids, string name, string city, string street, string bulding, string ips)
    {
        Id = ids;
        Properties[0] = name;
        Properties[0] = city;
        Properties[1] = street;
        Properties[2] = bulding;
        Ip = ips;
    }
    public Boller()
    {
        Id = -1;
        Properties[0] = "City";
        Properties[1] = "Street";
        Properties[2] = "Bulding";
        Ip = "0.0.0.0";
    }

    public override string ToString()
    {
        return string.Format("Index: {0}, City: {1}, Street: {2}, Build: {3}, ip-adress: {4}", 
            Id, Properties[0], Properties[1], Properties[2], Ip);
    }
}