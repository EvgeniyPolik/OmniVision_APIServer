namespace OmniVision_APIserver;

public class AnaliticMetods
{
    private enum positionName
    {
        connection
    }
    public SortedSet<string> ActiveWarnings(Dictionary<int, ushort[]> bollerStatus)
    {
        SortedSet<string> newActiveWarnings = new SortedSet<string>();
        foreach (var key in bollerStatus)
        {
            if (bollerStatus[key.Key][0] == 0)  // Первый элемент состояние связи
            {
                string item = key.Key.ToString() + positionName.connection.ToString() + key.Value[0].ToString();
                newActiveWarnings.Add(item);
                Console.WriteLine("нет связи с " + key.Key);
                if (!Program.Warnings.Contains(item))
                {
                    Console.WriteLine("Обнаружена новая угроза: нет связи с " + key.Key);
                }
                else
                {
                    Console.WriteLine("Неисправность: отсутствие связи устранено");
                }
            }
        }
        return newActiveWarnings;
    }
}