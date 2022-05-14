//Установить через NuGet FirebirdSql.Data.FirebirdClient + добавить зависимось от длл  FirebirdSql.Data.FirebirdClient
//Установить EntityFrameworkCore.FirebirdSQL
//Установить EntityFramework.Firebird
//Установить FirebirdSQL.EntityFrameworkCore.Firebird
//Установить System.Text.Encoding.CodePage + добавить зависимость от соответсвующей длл 
//Установить Newtonsoft.Json 
//Использована длл FluentModbus установить зависимость

using System.Net;
using System.Net.Sockets;
using NModbus;
using NModbus.Utility;
using System.Text;
using System.Threading;

using FirebirdSql.Data.FirebirdClient;
using Newtonsoft.Json;

namespace OmniVision_APIserver
{
    internal class Program
    {
        public static List<Boller> ListOfBollers = new List<Boller>();
        public static Dictionary<int, bool[]> HealthBoller = new Dictionary<int, bool[]>();

        public static void UpdateCatalog()
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
        
         public static void UpdHealth()
         {
             HealthBoller.Clear();
             Console.WriteLine($"New helth information on {DateTime.Now}: ");
             for (int i = 0; i < ListOfBollers.Count; i++)
             {
                 TcpClient clientTCP = new TcpClient(ListOfBollers[i].Ip, 502);
                 var targetKontroller = new ModbusFactory();
                 IModbusMaster modbusServer = targetKontroller.CreateMaster(clientTCP);
                 bool[] dq = modbusServer.ReadCoils(0, 8192, 4);  // Discrete outputs
                 ushort[] ai = modbusServer.ReadInputRegisters(0, 0, 2);  // Analog inputs
                 bool[] di = modbusServer.ReadInputs(0, 0, 6); // Discrete inputs
                 bool[] flag = modbusServer.ReadCoils(0, 8258, 1); // Read flag
                 Console.WriteLine($"Q1:{dq[0]}, Q2:{dq[1]}, Q3:{dq[2]}, Q4:{dq[3]}"); 
                 Console.WriteLine($"A1:{ai[0]}, A2:{ai[1]}"); 
                 Console.WriteLine($"I1:{di[0]}, I2:{di[1]}, I3:{di[2]}, I4:{di[3]}, I5:{di[4]}, I6:{di[5]}");
                 Console.WriteLine($"Flag M3:{flag[0]}");

             }
         }

        public static List<Boller> MakeNewCatalog()
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

        public static void Main(string[] args)
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



