//Установить через NuGet FirebirdSql.Data.FirebirdClient + добавить зависимось от длл  FirebirdSql.Data.FirebirdClient
//Установить EntityFrameworkCore.FirebirdSQL
//Установить EntityFramework.Firebird
//Установить FirebirdSQL.EntityFrameworkCore.Firebird
//Установить System.Text.Encoding.CodePage + добавить зависимость от соответсвующей длл 

using System.Text;
using FirebirdSql.Data.FirebirdClient;

namespace OmniVision_APIserver;

public class UpdateCatalog
{
    public string MakeNewCatalog()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);  // Для подключения кодировки win1251
        FbConnectionStringBuilder fbConn = new FbConnectionStringBuilder();  // Переменная с параметрами подключения к БД
        fbConn.DataSource = "127.0.0.1";  // Записываем в переменную параметры
        fbConn.Database = @"D:\Rider\education C#\OmniVision\MBD.fdb";
        fbConn.UserID = "SYSDBA";
        fbConn.Password = "masterkey";
        fbConn.Charset = "WIN1251";
        fbConn.ServerType = FbServerType.Default; 
        FbConnection dbConn = new FbConnection(fbConn.ToString());  // Создадим коннект к БД
        dbConn.Open(); // Активируем коннект
        string result = "";  // Сюда пока сложим результат запроса
        FbTransaction fbt = dbConn.BeginTransaction();  //  Создадим транзакцию
        FbCommand selectSQL = new FbCommand("SELECT ID_K, NAM_K, TIP_NP, NAM_NP, TIP_U, NAM_U, K_DOM, IP_ADR " +
                                            "FROM KOT LEFT JOIN NP ON K_NP = ID_NP LEFT JOIN ULC ON ID_U = K_U WHERE " +
                                            "K_DEL = 0 AND SHEMA_K > 0", dbConn);
        selectSQL.Transaction = fbt;  // Инициализация запроса транзакцией
        FbDataReader reader = selectSQL.ExecuteReader();  // Выполним запрос
        while (reader.Read())  // Запишем результат в переменную для отправки
        {
           result += reader.GetInt32(0).ToString() + "\t" + reader.GetString(1)  + "\t" + reader.GetString(2) + 
                     "\t" + reader.GetString(3) + "\t" + reader.GetString(4) + "\t" + reader.GetString(5) + "\t" +
                     reader.GetString(6) + "\t" + reader.GetString(7) + "\t" + "\n";
        }
        reader.Close();  // Обязательно закрываем запрос
        dbConn.Close();
        return result;
    }
    
}