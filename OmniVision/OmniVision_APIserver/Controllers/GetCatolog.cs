using Microsoft.AspNetCore.Mvc;

namespace OmniVision_APIserver.Controllers;
[Route("[controller]")]  // Прописать в адресную строку название контроеллера и параметр id
[ApiController]
public class GetCatolog : Controller
{
    
    [HttpGet("{id}")]  // Считает id из адресной строки ниже этой записи метод GET
    public string GetReguest(int id)
    {
        UpdateCatalog catalog = new UpdateCatalog();
        return catalog.MakeNewCatalog();
    }
}