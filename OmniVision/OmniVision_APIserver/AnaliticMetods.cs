using FirebirdSql.Data.FirebirdClient;

namespace OmniVision_APIserver;

public class AnaliticMetods
{
    private enum positionName
    {
        connection,
        temperatureUP
    }

    public void InsertQuery(string query)
    {
        FbCommand insertSql = new FbCommand(query, Program.dbConn);
        Program.dbConn.Open();
        Console.WriteLine(Program.dbConn.State.ToString());
        FbTransaction fbt = Program.dbConn.BeginTransaction();
        insertSql.Transaction = fbt;
        insertSql.ExecuteNonQuery();
        fbt.Commit();
        Program.dbConn.Close();
    }
    public string doDateOrTimeInString(bool doDate)  // Дата или время в нужном формате
    {
        string result;
        if (doDate)
        {
            result = "'" + DateTime.Now.Day.ToString() + ".";
            result += DateTime.Now.Month.ToString() + ".";
            result += DateTime.Now.Year.ToString() + "'";
        }
        else
        {
            result = "'" + DateTime.Now.Hour.ToString() + ".";
            result += DateTime.Now.Minute.ToString() + "'";
        }
        return result;
    }
    
    //Сбор информации о новых и действующих угрозах
    public SortedSet<string> ActiveWarnings(Dictionary<int, ushort[]> bollerStatus)  
    {
        SortedSet<string> newActiveWarnings = new SortedSet<string>();
        foreach (var key in bollerStatus)
        {
            if (bollerStatus[key.Key][0] == 0)  // Первый элемент состояние связи
            {
                string item = key.Key.ToString() + positionName.connection.ToString() + key.Value[0].ToString();
                newActiveWarnings.Add(item);
                if (!Program.Warnings.Contains(item))
                {
                    string newQuery =
                        $"INSERT INTO EVENET_LOG (E_DATE, E_TIME, E_KOT, E_TEXT) VALUES " +
                        $"({doDateOrTimeInString(true)}, {doDateOrTimeInString(false)}, {key.Key}, " +
                        $"'Обнаружено: отсутствие связи')";
                    InsertQuery(newQuery);
                }
            }
            else
            {
                string item = key.Key.ToString() + positionName.connection.ToString() + "0";
                if (Program.Warnings.Contains(item))
                {
                    string newQuery =  $"INSERT INTO EVENET_LOG (E_DATE, E_TIME, E_KOT, E_TEXT) VALUES " +
                                       $"({doDateOrTimeInString(true)}, {doDateOrTimeInString(false)}, {key.Key}, " +
                                       $"'Устранено: отсутствие связи')";
                    InsertQuery(newQuery);
                }
                // Температура подачи ниже 35 градусов и включен режим Зима
                if ((bollerStatus[key.Key][1] < 35) && (bollerStatus[key.Key][13] == 1))
                {
                    item = key.Key.ToString() + positionName.temperatureUP.ToString() + "0";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        string newQuery = $"INSERT INTO EVENET_LOG (E_DATE, E_TIME, E_KOT, E_TEXT) VALUES " +
                                          $"({doDateOrTimeInString(true)}, {doDateOrTimeInString(false)}, {key.Key}, " +
                                          $"'Обнаружено: предельно низкая температура подачи')";
                        InsertQuery(newQuery);
                    }
                }
                else
                {
                    item = key.Key.ToString() + positionName.temperatureUP.ToString() + "0";
                    if (Program.Warnings.Contains(item))
                    {
                        string newQuery = $"INSERT INTO EVENET_LOG (E_DATE, E_TIME, E_KOT, E_TEXT) VALUES " +
                                          $"({doDateOrTimeInString(true)}, {doDateOrTimeInString(false)}, {key.Key}, " +
                                          $"'Устранено: предельно низкая температура подачи')";
                        InsertQuery(newQuery);
                    }
                }
            }
        }
        return newActiveWarnings;
    }
}