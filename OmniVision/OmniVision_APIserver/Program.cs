//Установить через NuGet FirebirdSql.Data.FirebirdClient + добавить зависимось от длл  FirebirdSql.Data.FirebirdClient
//Установить EntityFrameworkCore.FirebirdSQL
//Установить EntityFramework.Firebird
//Установить FirebirdSQL.EntityFrameworkCore.Firebird
//Установить System.Text.Encoding.CodePage + добавить зависимость от соответсвующей длл 
//Установить Newtonsoft.Json 
//Использована длл FluentModbus установить зависимость


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
        public static Dictionary<int, ushort[]> HealthBoller = new Dictionary<int, ushort[]>();
        

        private static ushort[] BoolToUshort(bool[] originArray)
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

        private static ushort[] SummQudroArray(ushort[] analogInp, ushort[] discreteInp, ushort[] discreteOut,
            ushort[] flags)
        {
            ushort[] result = new ushort[analogInp.Length + discreteInp.Length + discreteOut.Length + flags.Length + 1];
            result[0] = 1;
            for (int i = 0; i < analogInp.Length; i++)
            {
                result[i + 1] = analogInp[i];
            }
            for (int i = 0; i < discreteInp.Length; i++)
            {
                result[i + analogInp.Length + 1] = discreteInp[i];
            }
            for (int i = 0; i < discreteOut.Length; i++)
            {
                result[i + analogInp.Length + discreteInp.Length + 1] = discreteOut[i];
            }
            for (int i = 0; i < flags.Length; i++)
            {
                result[i + analogInp.Length + discreteInp.Length + discreteOut.Length + 1] = flags[i];
            }
            return result;
        }

        private static ushort[] noAnswerArray()
        {
            ushort[] result = new ushort[]{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

            return result;
        }
        
        private static void UpdateCatalog()
        {
            int countRepit = 120;
            while (true)
            {
                if (countRepit > 119)
                {
                    ListOfBollers = MakeNewCatalog();
                    Console.WriteLine("Update catalog: " + DateTime.Now);
                    UpdHealth();
                    countRepit = 0;
                    Thread.Sleep(15 * 1000);
                }
                else
                {
                    UpdHealth();
                    countRepit++;
                    Thread.Sleep(15 * 1000);
                }

            }
        }
        
         private static void UpdHealth()
         {
             HealthBoller.Clear();
             Console.WriteLine($"New helth information on {DateTime.Now}: ");
             for (int i = 0; i < ListOfBollers.Count; i++)
             {
                 ushort[] status = new ushort[4];
                 try
                 {
                     TcpClient clientTCP = new TcpClient(ListOfBollers[i].Ip, 502);
                     var targetKontroller = new ModbusFactory();
                     IModbusMaster modbusServer = targetKontroller.CreateMaster(clientTCP);
                     ushort[] dq = BoolToUshort(modbusServer.ReadCoils(0, 8192, 4)); // Discrete outputs
                     ushort[] ai = modbusServer.ReadInputRegisters(0, 0, 2); // Analog inputs
                     ushort[] di = BoolToUshort(modbusServer.ReadInputs(0, 0, 6)); // Discrete inputs
                     ushort[] flag = BoolToUshort(modbusServer.ReadCoils(0, 8258, 1)); // Read flag
                     status = SummQudroArray(ai, di, dq, flag);
                 }
                 catch (SocketException ex)
                 {
                     status = noAnswerArray();
                 }
                 for (int z = 0; z < status.Length; z++)
                     Console.Write($"{z + 1}: {status[z]} ");
                 Console.WriteLine();    
             }
         }

        private static List<Boller> MakeNewCatalog()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Для подключения кодировки win1251
            FbConnectionStringBuilder fbConn = new FbConnectionStringBuilder(); // Переменная с параметрами подключения к БД
            fbConn.DataSource = "127.0.0.1"; // Записываем в переменную параметры
            fbConn.Database = @"D:\Rider\education C#\OmniVision\MBD.fdb";
            fbConn.UserID = "SYSDBA";
            fbConn.Password = "masterkey";
            fbConn.Charset = "WIN1251";
            fbConn.ServerType = FbServerType.Default;
            FbConnection dbConn = new FbConnection(fbConn.ToString()); // Создадим коннект к БД
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



