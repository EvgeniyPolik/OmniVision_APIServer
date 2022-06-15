using System.Net.Sockets;
using NModbus;

namespace OmniVision_APIserver;

[Serializable]
public class Commands
{
    public int Id  { get; set; }
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

    private static bool ProccesingCmd(IModbusMaster mServer, TcpClient tcpClient,ushort registr)
    {
        mServer.WriteSingleCoil(0, registr, true);
        Thread.Sleep(500);
        mServer.WriteSingleCoil(0, registr, false);
        tcpClient.Close();
        Program.BusyCmd = "noBusy";
        return true;
    }

    public static bool ExucuteCmd(int ids, int cmd)
    {
        Dictionary<int, ushort> registers = new Dictionary<int, ushort>();
        for (int i = 0; i < Program.ListOfBollers.Count; i++)
        {
            string ipAdress = "No ip-address";
            if (Program.ListOfBollers[i].Id == ids)
            {
                ipAdress = Program.ListOfBollers[i].Ip;
                if (Program.ListOfBollers[i].ShemaControl == 1)
                {
                    registers[1] = 8257; // Security
                    registers[2] = 8256; // ResetError
                    registers[3] = 8259; // Winter
                }
                else if (Program.ListOfBollers[i].ShemaControl == 2)
                {
                    registers[1] = 1536; // Security
                    registers[2] = 1538; // ResetError
                    registers[3] = 1539; // Winter
                }
            }

            if (ipAdress != "No ip-address")
            {
                if ((Program.BusyIp == ipAdress) || (Program.BusyCmd == ipAdress))
                {
                    Thread.Sleep(500);
                }
                Program.BusyCmd = ipAdress;
                try
                {
                    TcpClient clientTCP = new TcpClient(ipAdress, 502);
                    var targetKontroller = new ModbusFactory();
                    IModbusMaster modbusServer = targetKontroller.CreateMaster(clientTCP);
                    modbusServer.Transport.Retries = 0;
                    modbusServer.Transport.ReadTimeout = 1500;
                    modbusServer.Transport.WriteTimeout = 1500;
                    ProccesingCmd(modbusServer, clientTCP, registers[cmd]); 
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
                finally
                {                    
                    Program.BusyCmd = "noBusy";
                }
            }
        }
        return true;
    }
}