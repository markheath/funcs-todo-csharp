namespace AzureFunctionsTodo.Models;

public class TodoUpdateModel
{
    public string TaskDescription { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
