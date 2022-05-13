//Установить через NuGet FirebirdSql.Data.FirebirdClient + добавить зависимось от длл  FirebirdSql.Data.FirebirdClient
//Установить EntityFrameworkCore.FirebirdSQL
//Установить EntityFramework.Firebird
//Установить FirebirdSQL.EntityFrameworkCore.Firebird
//Установить System.Text.Encoding.CodePage + добавить зависимость от соответсвующей длл 
//Установить Newtonsoft.Json 
using System.Text;
using System.Threading;
using FirebirdSql.Data.FirebirdClient;
namespace OmniVision_APIserver
{
    internal class Program
    {
        public static List<Boller> listOfBollers = new List<Boller>();

        public static void UpdateCatalog()
        {
            while (true)
            {
                listOfBollers = MakeNewCatalog();
                Console.WriteLine("Update" + DateTime.Now);
                Thread.Sleep(900 * 1000);
            }
        }
        public static List<Boller> MakeNewCatalog()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Для подключения кодировки win1251
            FbConnectionStringBuilder fbConn = new FbConnectionStringBuilder(); // Переменная с параметрами подключения к БД
            fbConn.DataSource = "127.0.0.1"; // Записываем в переменную параметры
            fbConn.Database = @"C:\C#\OmniVision\MBD.fdb";
            fbConn.UserID = "SYSDBA";
            fbConn.Password = "masterkey";
            fbConn.Charset = "WIN1251";
            fbConn.ServerType = FbServerType.Default;
            FbConnection dbConn = new FbConnection(fbConn.ToString()); // Создадим коннект к БД
            dbConn.Open(); // Активируем коннект
            FbTransaction fbt = dbConn.BeginTransaction(); //  Создадим транзакцию
            FbCommand selectSql = new FbCommand("SELECT ID_K, NAM_K, TIP_NP, NAM_NP, TIP_U, NAM_U, K_DOM, IP_ADR " +
                                                "FROM KOT LEFT JOIN NP ON K_NP = ID_NP LEFT JOIN ULC ON ID_U = K_U WHERE " +
                                                "K_DEL = 0 AND SHEMA_K > 0", dbConn);
            selectSql.Transaction = fbt; // Инициализация запроса транзакцией
            FbDataReader reader = selectSql.ExecuteReader(); // Выполним запрос
            listOfBollers.Clear();
            while (reader.Read()) // Запишем результат в переменную для отправки
            {
                listOfBollers.Add(new Boller(reader.GetInt32(0), reader.GetString(1),
                    reader.GetString(2) + " " + reader.GetString(3), reader.GetString(4) + " " +
                                                                     reader.GetString(5), reader.GetString(6),
                    reader.GetString(7)));
            }

            reader.Close(); // Обязательно закрываем запрос
            dbConn.Close();
            return listOfBollers;
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

            Thread UpdatingCatalog = new Thread(new ThreadStart(UpdateCatalog));
            UpdatingCatalog.Start();
            //listOfBollers = MakeNewCatalog();  // Подготовим начальный список котельных
            
            app.Run();
        }
    }
}



