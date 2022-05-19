using System.Net.Sockets;
using NModbus;

namespace OmniVision_APIserver;

[Serializable]
public class Commands
{
    private enum comandRegistry
    {
        resetErrorBoller = 8256,
        switchSecurity = 8257,
        switchWinter = 8259
    }
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
        Console.WriteLine("Команда " + registr.ToString()); 
        tcpClient.Close();
        Program.BusyCmd = "noBusy";
        return true;
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
                    if (cmd == 1)  // Включение или отключение охраны
                    {
                        ProccesingCmd(modbusServer, clientTCP,(ushort) comandRegistry.switchSecurity);
                        return true;
                    }
                    else if (cmd == 2) // Сброс аварии котла
                    {
                        ProccesingCmd(modbusServer, clientTCP, (ushort) comandRegistry.resetErrorBoller);
                        return true;
                    }
                    else if (cmd == 3) // Перевод режима зима/лето
                    {
                        ProccesingCmd(modbusServer, clientTCP, (ushort) comandRegistry.switchWinter);
                        return true;
                    }
                    else
                    {
                        clientTCP.Close();
                        Program.BusyCmd = "noBusy";
                        return false;
                    }
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Program.BusyCmd = "noBusy";
                    return false;
                }
            }
        }
        return true;
    }
}