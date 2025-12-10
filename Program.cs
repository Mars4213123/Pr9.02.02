using TaskManagerTelegramBot_Кантуганов;
using TaskManagerTelegramBot_Кантуганов.TaskManagerTelegramBot_Кантуганов.Classes;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
