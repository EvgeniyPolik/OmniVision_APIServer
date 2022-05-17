using FirebirdSql.Data.FirebirdClient;

namespace OmniVision_APIserver;

public class AnaliticMetods
{
    private enum PositionName
    {
        Connection,
        TemperatureUp,
        DevationTemperature,
        PressureLow,
        PressureHigh,
        FireAlarm,
        BoillerError,
        NoPower,
        GazAlarm,
        StateBoiller,
        SecurityAlarm,
        SecurityAttention
    }

    public void InsertQuery(int keys, string messgeText)  // Ведение журнала событий
    {
        string newQuery =
            $"INSERT INTO EVENET_LOG (E_DATE, E_TIME, E_KOT, E_TEXT) VALUES " +
            $"({doDateOrTimeInString(true)}, {doDateOrTimeInString(false)}, {keys}, " +
            $"'{messgeText}')";
        FbCommand insertSql = new FbCommand(newQuery, Program.dbConn);
        Program.dbConn.Open();
        Console.WriteLine(Program.dbConn.State.ToString());
        FbTransaction fbt = Program.dbConn.BeginTransaction();
        insertSql.Transaction = fbt;
        insertSql.ExecuteNonQuery();
        fbt.Commit();
        Program.dbConn.Close();
        Console.WriteLine(messgeText);
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
    
    // Расчет кооэффицента корреции для температурного коээфициента
    public double tempcoefficient(int tAir)
    {
        double result;
        if (tAir >= 4)
        {
            result = 0;
        }
        else if ((tAir < 4) && (tAir >= -4))
        {
            result = 0.1;
        }
        else if ((tAir < -4) && (tAir >= -10))
        {
            result = 0.2;
        }
        else if ((tAir < -10) && (tAir >= -17))
        {
            result = 0.3;
        }
        else if ((tAir < -17) && (tAir >= -24))
        {
            result = 0.4;
        }
        else
        {
            result = 0.5;
        }

        return result;
    }
    //Сбор информации о новых и действующих угрозах
    public SortedSet<string> ActiveWarnings(Dictionary<int, ushort[]> bollerStatus)  
    {
        SortedSet<string> newActiveWarnings = new SortedSet<string>();
        foreach (var key in bollerStatus)
        {
            if (bollerStatus[key.Key][0] == 0) // Первый элемент состояние связи
            {
                string item = key.Key.ToString() + PositionName.Connection.ToString() + key.Value[0].ToString();
                newActiveWarnings.Add(item);
                if (!Program.Warnings.Contains(item))
                {
                    InsertQuery(key.Key, "Обнаружено: отсутствие связи");
                }
            }
            else
            {
                string item = key.Key.ToString() + PositionName.Connection.ToString() + "0";
                if (Program.Warnings.Contains(item))
                {
                    InsertQuery(key.Key, "Устранено: отсутствие связи");
                }

                // Температура подачи ниже 35 градусов и включен режим Зима
                if ((bollerStatus[key.Key][2] < 35) && (bollerStatus[key.Key][13] == 1))
                {
                    item = key.Key.ToString() + PositionName.TemperatureUp.ToString() + "0";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Обнаружено: предельно низкая температура подачи");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.TemperatureUp.ToString() + "0";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Устранено: предельно низкая температура подачи");
                    }
                }

                // Проверка соответствия температурному графику
                double coefficient = tempcoefficient(bollerStatus[key.Key][1]);
                double tempEstimate = ((10 - bollerStatus[key.Key][1]) * 1.2) + 40.4 + coefficient;
                if ((Math.Abs(tempEstimate - bollerStatus[key.Key][2]) > 3) && (bollerStatus[key.Key][13] == 1))
                {
                    item = key.Key.ToString() + PositionName.DevationTemperature + "1";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Обнаружено: отклонение от температурного графика");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.DevationTemperature + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Устранено: отклонение от температурного графика");
                    }
                }

                if ((bollerStatus[key.Key][3] < 1) && (bollerStatus[key.Key][13] == 1)) //Давление теплоносителя низкое
                {
                    item = key.Key.ToString() + PositionName.PressureLow + "0";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Обнаружено: давление теплонисителя низкое");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.PressureLow + "0";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Устранено: давление теплонисителя низкое");
                    }
                }

                if (bollerStatus[key.Key][3] > 3) //Давление теплоносителя высокое
                {
                    item = key.Key.ToString() + PositionName.PressureHigh + "1";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Обнаружено: давление теплонисителя низкое");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.PressureHigh + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Устранено: давление теплонисителя низкое");
                    }
                }
                
                if (bollerStatus[key.Key][5] == 0) // Пожарная тревога
                {
                    item = key.Key.ToString() + PositionName.FireAlarm + "0";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Обнаружено: пожарная тревога!");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.FireAlarm + "0";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Устранено: пожарная тревога");
                    }
                }
                if (bollerStatus[key.Key][6] == 1) // Аварийная остановка котла
                {
                    item = key.Key.ToString() + PositionName.BoillerError + "1";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Обнаружено: аварийная остановка котла");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.BoillerError + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Устранено: аварийная остановка котла");
                    }
                }
                if (bollerStatus[key.Key][7] == 0) // Отключение питания
                {
                    item = key.Key.ToString() + PositionName.NoPower + "0";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Обнаружено: отключение питания");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.NoPower + "0";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Устранено: отключение питания");
                    }
                }
                if (bollerStatus[key.Key][8] == 0) // Загазованность
                {
                    item = key.Key.ToString() + PositionName.GazAlarm + "1";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Обнаружено: загазованность помещения");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.GazAlarm + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Устранено: загазованность помещения");
                    }
                }
                if (bollerStatus[key.Key][9] == 0) // Статус котла
                {
                    item = key.Key.ToString() + PositionName.StateBoiller + "1";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Обнаружено: котел выключен");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.StateBoiller + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Устранено: котел включен");
                    }
                }
                if (bollerStatus[key.Key][10] == 0) // Охранная тревога
                {
                    item = key.Key.ToString() + PositionName.SecurityAlarm + "1";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Обнаружено: проникновение, нарушение периметра");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.SecurityAlarm + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Устранено: проникновение, нарушение периметра");
                    }
                }
                if (bollerStatus[key.Key][14] == 0) // Снятие с охраны
                {
                    item = key.Key.ToString() + PositionName.SecurityAttention + "1";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Объект снят с охраны");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.SecurityAttention + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, "Объект поставлен под охрану");
                    }
                }
            }
        }
        return newActiveWarnings; 
    }
}