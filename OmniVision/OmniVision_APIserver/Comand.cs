namespace OmniVision_APIserver;

[Serializable]
public class Commands
{
    public int Id;
    public int Command;
    //{"Id":0,"Command":0}

    public Commands(int ids, int cmd)
    {
        Id = ids;
        Command = cmd;
    }

    public Commands()
    {
        Id = 0;
        Command = 5;
    }

    public override string ToString()
    {
        return $"Identification: {Id}, number command: {Command}";
    }

    public static void ExucuteCmd()
    {
        
    }
}