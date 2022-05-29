//Установить через NuGet FirebirdSql.Data.FirebirdClient + добавить зависимось от длл  FirebirdSql.Data.FirebirdClient
//Установить EntityFrameworkCore.FirebirdSQL
//Установить EntityFramework.Firebird
//Установить FirebirdSQL.EntityFrameworkCore.Firebird
//Установить System.Text.Encoding.CodePage + добавить зависимость от соответсвующей длл для кодировок!!!
//Установить Newtonsoft.Json 
//Использована длл FluentModbus установить зависимость


using System.Net.Mime;
using System.Net.Sockets;
using NModbus;
using System.Text;
using System.Threading;

using FirebirdSql.Data.FirebirdClient;
using Newtonsoft.Json;

namespace OmniVision_APIserver
{
    internal class Program
    {
        public static List<Boller> ListOfBollers = new List<Boller>();
        public static Dictionary<int, short[]> HealthBoller = new Dictionary<int, short[]>();
        public static SortedSet<string> Warnings = new SortedSet<string>();
        public static FbConnection dbConn = new FbConnection();
        public static Dictionary<int, short[]> HourStatusAirTemperature = new Dictionary<int, short[]>();
        public static Dictionary<int, short[]> HourStatusUpTemperature = new Dictionary<int, short[]>();
        public static string BusyIp = "noBusy";
        public static string BusyCmd = "noBusy";
        public enum TypePlc
        {
            siemens,
            onisystem
        }

        private static ushort[] BoolToUshort(bool[] originArray)  // Преобразование массива bool в ushort
        {
            ushort[] makedArrey = new ushort[originArray.Length];
            for (int i = 0; i < originArray.Length; i++)
            {
                if (originArray[i])
                {
                    makedArrey[i] = 1;
                }
                else
                {
                    makedArrey[i] = 0;
                }
            }
            return makedArrey;
        }

        //  Соединение 4х параметров в единый массив
        private static short[] SummQudroArray(ushort[] analogInp, ushort[] discreteInp, ushort[] discreteOut,
            ushort[] flag1, ushort[] flag2)
        {
            short[] result = new short[analogInp.Length + discreteInp.Length + discreteOut.Length + flag1.Length + flag2.Length + 1];
            result[0] = 1;
            for (int i = 0; i < analogInp.Length; i++)
            {
                result[i + 1] = (short)(analogInp[i] - 273);
            }
            for (int i = 0; i < discreteInp.Length; i++)
            {
                result[i + analogInp.Length + 1] = (short)discreteInp[i];
            }
            for (int i = 0; i < discreteOut.Length; i++)
            {
                result[i + analogInp.Length + discreteInp.Length + 1] = (short)discreteOut[i];
            }
    
            result[analogInp.Length + discreteInp.Length + discreteOut.Length + 1] = (short)flag1[0];
            result[analogInp.Length + discreteInp.Length + discreteOut.Length + 2] = (short)flag2[0];
            
            return result;
        }

        private static short[] noAnswerArray()
        {
            short[] result = new short[]{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

            return result;
        }
        
        private static void UpdateCatalog()  // поток обновления информации
        {
            int countRepit = 120;
            dbConn = MadeConnectDb();  // Создание конекта к БД
            while (true)
            {
                if (countRepit > 119)  // Переодическое бновление списка котельных
                {
                    ListOfBollers = MadeNewCatalog();
                    HealthBoller = UpdHealth();
                    countRepit = 0;
                    Thread.Sleep(15 * 1000);
                }
                else
                {
                    HealthBoller = UpdHealth();  // Чтение параметров работы котельных
                    countRepit++;
                    Thread.Sleep(15 * 1000);
                }

            }
        }

        private static Dictionary<string, ushort[]> AdressInPlc(TypePlc plc)
        {
            Dictionary<string, ushort[]> result = new Dictionary<string, ushort[]>();
            
            if (plc == TypePlc.siemens)
            {
                result["DI"] = new ushort[] {0, 6};
                result["DO"] = new ushort[] {8192, 4};
                result["AI"] = new ushort[] {0, 2};
                result["Flag1"] = new ushort[] {8258, 1};
                result["Flag2"] = new ushort[] {8260, 1};
            }
            else if (plc == TypePlc.onisystem)
            {
                result["DI"] = new ushort[] {0, 6};
                result["DO"] = new ushort[] {4096, 4};
                result["AI"] = new ushort[] {0, 2};
                result["Flag1"] = new ushort[] {1540, 1};
                result["Flag2"] = new ushort[] {1537, 1};
            }

            return result;
        }
         private static Dictionary<int, short[]> UpdHealth()  // Обновление списка состояния котельных
         {
             Dictionary<int, short[]> newHeathStatus = new Dictionary<int, short[]>();
             for (int i = 0; i < ListOfBollers.Count; i++)
             {
                 short[] status = new short[15];
                 if ((BusyIp == ListOfBollers[i].Ip) || (BusyCmd == ListOfBollers[i].Ip))
                 {
                     Thread.Sleep(500);
                 }
                 BusyIp = ListOfBollers[i].Ip;
                 try
                 {
                     TcpClient clientTCP = new TcpClient(ListOfBollers[i].Ip, 502);
                     var targetKontroller = new ModbusFactory();
                     IModbusMaster modbusServer = targetKontroller.CreateMaster(clientTCP);
                     modbusServer.Transport.Retries = 0;
                     modbusServer.Transport.ReadTimeout = 1500;
                     Dictionary<string, ushort[]> AddressOfRegistr = new Dictionary<string, ushort[]>();
                     if (ListOfBollers[i].ShemaControl == 1) // 1 Это сименс
                     {
                         AddressOfRegistr = AdressInPlc(TypePlc.siemens);
                     }
                     else if (ListOfBollers[i].ShemaControl == 2) // 2 Это они онисистемс
                     {
                         AddressOfRegistr = AdressInPlc(TypePlc.onisystem);
                     }

                     ushort[] dq = BoolToUshort(modbusServer.ReadCoils(0,
                         AddressOfRegistr["DO"][0], AddressOfRegistr["DO"][1])); // Discrete outputs
                     ushort[] ai = modbusServer.ReadInputRegisters(0, AddressOfRegistr["AI"][0],
                         AddressOfRegistr["AI"][1]); // Analog inputs
                     ushort[] di = BoolToUshort(modbusServer.ReadInputs(0,
                         AddressOfRegistr["DI"][0], AddressOfRegistr["DI"][1])); // Discrete inputs
                     ushort[] flag1 = BoolToUshort(modbusServer.ReadCoils(0,
                         AddressOfRegistr["Flag1"][0], AddressOfRegistr["Flag1"][1])); // Read flag
                     ushort[] flag2 = BoolToUshort(modbusServer.ReadCoils(0,
                         AddressOfRegistr["Flag2"][0], AddressOfRegistr["Flag2"][1]));
                     status = SummQudroArray(ai, di, dq, flag1, flag2);
                     clientTCP.Close();
                 }
                 catch (Exception)
                 {
                     status = noAnswerArray();
                 }
                 finally
                 {
                     BusyIp = "noBusy"; 
                 }
                 newHeathStatus[ListOfBollers[i].Id] = status;

             }

             AnaliticMetods analitics = new AnaliticMetods();
             Warnings = analitics.ActiveWarnings(newHeathStatus);
            
             return newHeathStatus;
         }

         private static FbConnection MadeConnectDb()  // Создание конекта к БД
         {
             Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Для подключения кодировки win1251
             FbConnectionStringBuilder fbConn = new FbConnectionStringBuilder(); // Переменная с параметрами подключения к БД
              // Записываем в переменную параметры
             FbConnection dbCursor = new FbConnection();
             while (true)
             {
                 try  // Проверим корректность настроек БД
                 {   
                     string[] param = new String[2];
                     param = File.ReadAllLines("conf.cfg");
                     fbConn.DataSource = param[0];
                     fbConn.Database = param[1];
                     fbConn.UserID = "SYSDBA";
                     fbConn.Password = "masterkey";
                     fbConn.Charset = "WIN1251";
                     fbConn.ServerType = FbServerType.Default;
                     dbCursor.ConnectionString = fbConn.ToString();
                     dbCursor.Open();
                     dbCursor.Close();
                     break;
                 }
                 catch (Exception)
                 {
                     Console.WriteLine("База данных не доступна. Уточниете настройки или введите exit для выхода");
                     Console.Write("Адрес сервера #:");
                     fbConn.DataSource =  Console.ReadLine();
                     if (fbConn.DataSource == "exit")
                     {
                         Environment.Exit(0);  // От настройки отказались выходим
                     }
                     string[] param = new String[2];
                     param[0] = fbConn.DataSource;
                     Console.Write("Путь до БД #:");
                     fbConn.Database =  Console.ReadLine();
                     param[1] = fbConn.Database;
                     File.WriteAllLines("conf.cfg", param);
                     dbCursor.ConnectionString = fbConn.ToString();
                 }
             }
            
             return dbCursor;
         }
         
        private static List<Boller> MadeNewCatalog()
        {

            dbConn.Open(); // Активируем коннект
            FbTransaction fbt = dbConn.BeginTransaction(); //  Создадим транзакцию
            FbCommand selectSql = new FbCommand("SELECT ID_K, NAM_K, TIP_NP, NAM_NP, TIP_U, NAM_U, K_DOM, IP_ADR, SHEMA_K " +
                                                "FROM KOT LEFT JOIN NP ON K_NP = ID_NP LEFT JOIN ULC ON ID_U = K_U WHERE " +
                                                "K_DEL = 0 AND SHEMA_K > 0", dbConn);
            selectSql.Transaction = fbt; // Инициализация запроса транзакцией
            FbDataReader reader = selectSql.ExecuteReader(); // Выполним запрос
            ListOfBollers.Clear();
            while (reader.Read()) // Запишем результат в переменную для отправки
            {
                ListOfBollers.Add(new Boller(reader.GetInt32(0), reader.GetString(1),
                    reader.GetString(2) + " " + reader.GetString(3), reader.GetString(4) + " " + 
                    reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetInt32(8)));
            }
            
            reader.Close(); // Обязательно закрываем запрос
            dbConn.Close();
            return ListOfBollers;
        }

        private static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

            builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

// Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();
         
// Организация отдельного потока для создания списка котельных
            Thread updatingCatalog = new Thread(new ThreadStart(UpdateCatalog));
            updatingCatalog.Start();
           
            app.Run();
        }
    }
}



