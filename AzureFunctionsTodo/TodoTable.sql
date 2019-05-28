CREATE TABLE [dbo].[Todos]
(
	[Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
    [TaskDescription] NVARCHAR(50) NOT NULL, 
    [IsCompleted] BIT NOT NULL, 
    [CreatedTime] DATETIME NOT NULL
)