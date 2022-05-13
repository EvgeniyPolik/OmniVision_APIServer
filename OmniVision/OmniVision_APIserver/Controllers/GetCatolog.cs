using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace OmniVision_APIserver.Controllers;
[Route("[controller]")]  // Прописать в адресную строку название контроеллера и параметр id
[ApiController]
public class GetCatolog : Controller
{
    
    [HttpGet("{id}")]  // Считает id из адресной строки ниже этой записи метод GET
    public string GetReguest(int id)
    {
        
        string result ="endoffile";
        if (id < Program.listOfBollers.Count)
        {
            result = JsonConvert.SerializeObject(Program.listOfBollers[id]);
        } 
        return result;
    }
}