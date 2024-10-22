using Refit;

namespace KdqParser;

internal interface IKdqApi
{
    [Get("")]
    Task<string> GetAllKdqItemsAsync();
    
    [Get("/kerndaten/{id}")]
    Task<string>GetKdqItemAsync(string id);
}