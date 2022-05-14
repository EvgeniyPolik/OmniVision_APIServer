namespace OmniVision_APIserver;

[Serializable]
public class Boller
{
    public int Id;
    public string[] Properties = new string[4];
    public string Ip;
    public int ShemaControl;

    public Boller(int ids, string name, string city, string street, string bulding, string ips, int shema)
    {
        Id = ids;
        Properties[0] = name;
        Properties[1] = city;
        Properties[2] = street;
        Properties[3] = bulding;
        Ip = ips;
        ShemaControl = shema;
    }
    public Boller()
    {
        Id = -1;
        Properties[0] = "name";
        Properties[1] = "City";
        Properties[2] = "Street";
        Properties[3] = "Bulding";
        Ip = "0.0.0.0";
        ShemaControl = 0;
    }

    public override string ToString()
    {
        return string.Format("Index: {0}, Name: {1}, City: {2}, Street: {3}, Build: {4}, ip-adress: {5}, NumberShema: {6}", 
            Id, Properties[0], Properties[1], Properties[2], Properties[3], Ip, ShemaControl);
    }
}