﻿namespace AzureFunctionsTodo.Models;

public class Todo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    public string TaskDescription { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
