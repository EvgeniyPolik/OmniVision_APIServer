using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace OmniVision_APIserver;

[Route("[controller]")]  // Прописать в адресную строку название контроеллера и параметр id
[ApiController]
public class GetStatus : Controller


{
    [HttpGet("{id}")]  // Считает id из адресной строки ниже этой записи метод GET
    public string GetReguest(int id)
    {
        string result ="Not_found";
        if (Program.HealthBoller.ContainsKey(id))
        {
            result = JsonConvert.SerializeObject(Program.HealthBoller[id]);
        } 
        return result;
    }

    [HttpPost]
    public IActionResult ProcesorComand([FromBody]Commands cmd)
    {
        if (cmd == null)
        {
            return BadRequest();
        }
        Console.WriteLine(cmd.ToString());
        Commands.ExucuteCmd(cmd.Id, cmd.Command);
        return new OkResult();
    }
}