using FirebirdSql.Data.FirebirdClient;

namespace OmniVision_APIserver;

public class AnaliticMetods
{
    private enum PositionName
    {
        Connection,
        TemperatureUp,
        DevationTemperature,
        PressureError,
        FireAlarm,
        BoillerError,
        NoPower,
        GazAlarm,
        StateBoiller,
        SecurityAlarm,
        SecurityAttention
    }
    private enum QueryType
    {
        EventLog,
        HourInfo
    }

    public void InsertQuery(int keys, int type, string messgeText)  // Ведение журнала событий
    {
        string newQuery;
        if (type == 0)
        {
            newQuery =
            $"INSERT INTO EVENET_LOG (E_DATE, E_TIME, E_KOT, E_TEXT) VALUES " +
            $"({doDateOrTimeInString(true)}, {doDateOrTimeInString(false)}, {keys}, " +
            $"'{messgeText}')";
        }
        else if (type == 1)
        {
            newQuery =
            $"INSERT INTO HOUR_TABLE (DI_DATE, DI_TIME, DI_KOT, T1, T2) VALUES " +
            $"({doDateOrTimeInString(true)},{doDateOrTimeInString(false)},{keys}," +
            $"{messgeText})";
        }
        else
        {
            newQuery = "";
        }
        FbCommand insertSql = new FbCommand(newQuery, Program.dbConn);
        Program.dbConn.Open();
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
    public SortedSet<string> ActiveWarnings(Dictionary<int, short[]> bollerStatus)  
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
                    InsertQuery(key.Key, (int) QueryType.EventLog,"Обнаружено: отсутствие связи");
                }
            }
            else
            {
                string item = key.Key.ToString() + PositionName.Connection.ToString() + "0";
                if (Program.Warnings.Contains(item))
                {
                    InsertQuery(key.Key, (int) QueryType.EventLog,"Устранено: отсутствие связи");
                }
                // Связь с контроллером установлена, узнаем другие пораметры
                
                if (Program.HourStatusAirTemperature.ContainsKey(key.Key)) // Собираем часовую информацию
                {
                    if (Program.HourStatusAirTemperature[key.Key][0] == DateTime.Now.Hour)
                    {
                        // записываем час учета и количество подсчитаных раз
                        Program.HourStatusAirTemperature[key.Key][Program.HourStatusAirTemperature[key.Key][1] + 2] = 
                            bollerStatus[key.Key][1];
                        Program.HourStatusUpTemperature[key.Key][Program.HourStatusAirTemperature[key.Key][1]] =
                            bollerStatus[key.Key][2];
                        Program.HourStatusAirTemperature[key.Key][1]++;
                    }
                    else
                    {
                        int sumT1 = 0;
                        int sumT2 = 0;
                        for (int i = 2; i < Program.HourStatusAirTemperature[key.Key].Length - 2; i++)
                        {
                            sumT1 += Program.HourStatusAirTemperature[key.Key][i];
                            sumT2 += Program.HourStatusUpTemperature[key.Key][i - 2];

                        }
                        double averageT1 = (double)(sumT1 / Program.HourStatusAirTemperature[key.Key][1]);
                        double averageT2 = (double)(sumT2 / Program.HourStatusAirTemperature[key.Key][1]);
                        string text = $"{averageT1}, {averageT2}";
                        InsertQuery(key.Key, (int)QueryType.HourInfo, text);
                        Console.WriteLine($"Данные на {doDateOrTimeInString(false)} T1 = {averageT1}, T2 = {averageT2}");
                        Program.HourStatusAirTemperature[key.Key][0] = (short) DateTime.Now.Hour;
                        Program.HourStatusAirTemperature[key.Key][1] = 0;    
                        for (int i = 2; i < Program.HourStatusAirTemperature[key.Key].Length; i++)
                        {
                            Program.HourStatusAirTemperature[key.Key][i] = 0;
                            Program.HourStatusUpTemperature[key.Key][i - 2] = 0;
                        }
                    }
                }
                else
                {
                    Program.HourStatusAirTemperature[key.Key] = new short[242];
                    Program.HourStatusUpTemperature[key.Key] = new short[240];
                    Program.HourStatusAirTemperature[key.Key][0] = (short) DateTime.Now.Hour;
                    Program.HourStatusAirTemperature[key.Key][1] = 0;
                    for (int i = 2; i < Program.HourStatusAirTemperature[key.Key].Length; i++)
                    {
                        Program.HourStatusAirTemperature[key.Key][i] = 0;
                        Program.HourStatusUpTemperature[key.Key][i - 2] = 0;
                    }
                }
                // Температура подачи ниже 35 градусов и включен режим Зима - Значит что-то не так
                if ((bollerStatus[key.Key][2] < 35) && (bollerStatus[key.Key][13] == 1))
                {
                    item = key.Key.ToString() + PositionName.TemperatureUp.ToString() + "0";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Обнаружено: предельно низкая температура подачи");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.TemperatureUp.ToString() + "0";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Устранено: предельно низкая температура подачи");
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
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Обнаружено: отклонение от температурного графика");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.DevationTemperature + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Устранено: отклонение от температурного графика");
                    }
                }

                if ((bollerStatus[key.Key][3] == 1) && (bollerStatus[key.Key][13] == 1)) // Ошибка давления
                {
                    item = key.Key.ToString() + PositionName.PressureError + "1";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Обнаружено: не допустимое давление теплонисителя");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.PressureError + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Устранено: не допустимое давление теплонисителя");
                    }
                }

                if (bollerStatus[key.Key][5] == 0) // Пожарная тревога
                {
                    item = key.Key.ToString() + PositionName.FireAlarm + "0";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Обнаружено: пожарная тревога!");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.FireAlarm + "0";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Устранено: пожарная тревога");
                    }
                }
                if (bollerStatus[key.Key][6] == 1) // Аварийная остановка котла
                {
                    item = key.Key.ToString() + PositionName.BoillerError + "1";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Обнаружено: аварийная остановка котла");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.BoillerError + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Устранено: аварийная остановка котла");
                    }
                }
                if (bollerStatus[key.Key][7] == 0) // Отключение питания
                {
                    item = key.Key.ToString() + PositionName.NoPower + "0";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Обнаружено: отключение питания");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.NoPower + "0";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Устранено: отключение питания");
                    }
                }
                if (bollerStatus[key.Key][8] == 0) // Загазованность
                {
                    item = key.Key.ToString() + PositionName.GazAlarm + "1";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Обнаружено: загазованность помещения");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.GazAlarm + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Устранено: загазованность помещения");
                    }
                }
                if ((bollerStatus[key.Key][9] == 0)  && (bollerStatus[key.Key][13] == 1))// Статус котла
                {
                    item = key.Key.ToString() + PositionName.StateBoiller + "1";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Обнаружено: котел выключен");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.StateBoiller + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Устранено: котел включен");
                    }
                }
                if (bollerStatus[key.Key][10] == 1) // Охранная тревога
                {
                    item = key.Key.ToString() + PositionName.SecurityAlarm + "1";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Обнаружено: проникновение, нарушение периметра");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.SecurityAlarm + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Устранено: проникновение, нарушение периметра");
                    }
                }
                if (bollerStatus[key.Key][14] == 0) // Снятие с охраны
                {
                    item = key.Key.ToString() + PositionName.SecurityAttention + "1";
                    newActiveWarnings.Add(item);
                    if (!Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Объект снят с охраны");
                    }
                }
                else
                {
                    item = key.Key.ToString() + PositionName.SecurityAttention + "1";
                    if (Program.Warnings.Contains(item))
                    {
                        InsertQuery(key.Key, (int) QueryType.EventLog,"Объект поставлен под охрану");
                    }
                }
            }
        }
        return newActiveWarnings; 
    }
}