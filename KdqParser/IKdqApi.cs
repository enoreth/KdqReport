using Refit;

namespace KdqParser;

internal interface IKdqApi
{
    [Get("")]
    Task<string> GetAllKdqItemsAsync();
    
    [Get("/OpenData/kdq?id=87BA5ED1")]
    Task<string> GetAllKdqItemsAusschreibungAtAsync();
    
    [Get("/kerndaten/{id}")]
    Task<string>GetKdqItemAsync(string id);
    
    [Get("/OpenData/kd?id={id}")]
    Task<string>GetKdqItemAusschreibungAtAsync(string id);
}