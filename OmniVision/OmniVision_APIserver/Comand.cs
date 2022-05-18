using System.Net.Sockets;
using NModbus;

namespace OmniVision_APIserver;

[Serializable]
public class Commands
{
    public int Id { get; set; }
    public int Command { get; set; }
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

    public static bool ExucuteCmd(int ids, int cmd)
    {
        for (int i = 0; i < Program.ListOfBollers.Count; i++)
        {
            string ipAdress = "No ip-address";
            if (Program.ListOfBollers[i].Id == ids)
            {
                ipAdress = Program.ListOfBollers[i].Ip;
            }

            if (ipAdress != "No ip-address")
            {
                try
                {
                    TcpClient clientTCP = new TcpClient(ipAdress, 502);
                    var targetKontroller = new ModbusFactory();
                    IModbusMaster modbusServer = targetKontroller.CreateMaster(clientTCP);
                    modbusServer.Transport.Retries = 0;
                    modbusServer.Transport.ReadTimeout = 1500;
                    modbusServer.Transport.WriteTimeout = 1500;
                    if (cmd == 1)  // Включение или отключение охраны
                    {
                        modbusServer.WriteSingleCoil(0, 8257, true);
                        Thread.Sleep(500);
                        modbusServer.WriteSingleCoil(0, 8257, false);
                        clientTCP.Close();  // Не забывай отключать соединения!
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                    
                }

            }
        }

        return true;
    }
}