
dotnet ef migrations add Initial --context InboxOutboxPattern.Service1.ServiceDbContext --output-dir Migrations/Service1
dotnet ef database update --context InboxOutboxPattern.Service1.ServiceDbContext


dotnet ef migrations add Initial --context InboxOutboxPattern.Service2.ServiceDbContext --output-dir Migrations/Service2
dotnet ef database update --context InboxOutboxPattern.Service2.ServiceDbContext