namespace AzureFunctionsTodo.EntityFramework;

public class TodoEf
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    public string TaskDescription { get; set; } = String.Empty;
    public bool IsCompleted { get; set; }
}
