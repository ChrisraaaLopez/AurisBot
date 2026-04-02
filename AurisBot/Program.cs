using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AurisBot.Services;

// Cargar Variables de Entorno
var root = Directory.GetParent(AppContext.BaseDirectory);
while (root != null && !File.Exists(Path.Combine(root.FullName, ".env")))
    root = root.Parent;

Env.Load(Path.Combine(root!.FullName, ".env"));

// Contruir Host
var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddConsole();
    })
    .ConfigureServices(services =>
    {
        // Configuración del cliente DiscordSocketClient
        var socketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds              // Eventos relacionados con servidores (guilds)
                           | GatewayIntents.GuildMessages       // Mensajes en canales de texto
                           | GatewayIntents.GuildVoiceStates,   // Cambios en canales de voz (necesario para música)
            LogLevel = LogSeverity.Info                         // Nivel de logging para el cliente Discord
        };

        // Singleton del cliente DiscordSocketClient con la configuración personalizada
        services.AddSingleton(new DiscordSocketClient(socketConfig));

        // Sefvicio para manejar interacciones (slash commands)
        services.AddSingleton<InteractionService>(provider =>
            new InteractionService(provider.GetRequiredService<DiscordSocketClient>()));

        // Servicios personalizados de la aplicación
        services.AddSingleton<GuildGuardService>();
        services.AddSingleton<AudioService>();

        // Lavalink4NET para música
        services.AddLavalink();

        // Configurar la conexión al servidor Lavalink
        services.ConfigureLavalink(config =>
        {
            // Toma la configuración de Lavalink desde variables de entorno, con valores por defecto
            var host = Environment.GetEnvironmentVariable("LAVALINK_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("LAVALINK_PORT") ?? "2333";
            var password = Environment.GetEnvironmentVariable("LAVALINK_PASSWORD") ?? "youshallnotpass";
            var secure = bool.Parse(Environment.GetEnvironmentVariable("LAVALINK_SECURE") ?? "false");

            config.BaseAddress = new Uri($"{(secure ? "https" : "http")}://{host}:{port}");
            config.Passphrase = password;
            config.ReadyTimeout = TimeSpan.FromSeconds(10);
        });
    })
    .Build();

// Obtener servicios necesarios
var client = host.Services.GetRequiredService<DiscordSocketClient>();
var interactions = host.Services.GetRequiredService<InteractionService>();
var guard = host.Services.GetRequiredService<GuildGuardService>();

// Logs del cliente Discord
client.Log += log =>
{
    Console.WriteLine($"[Discord] [{log.Severity}] {log.Source}: {log.Message}");
    return Task.CompletedTask;
};

// Eventos del cliente Discord

// Ready: el bot se conectó exitosamente a Discord
client.Ready += async () =>
{
    Console.WriteLine($"AurisBot conectado: {client.CurrentUser.Username}");

    await interactions.RegisterCommandsGloballyAsync();
    Console.WriteLine("Slash commands registrados");
};

// GuildAvailable: el bot se unió a un nuevo servidor o se reconectó a uno existente
client.GuildAvailable += async guild =>
{
    await guard.ValidateGuildAsync(guild);
};

// InteractionCreated: se creó una nueva interacción (slash command)
client.InteractionCreated += async interaction =>
{
    var ctx = new SocketInteractionContext(client, interaction);
    await interactions.ExecuteCommandAsync(ctx, host.Services);
};

// Registrar modulos de comandos de interacción
await interactions.AddModulesAsync(
    assembly: System.Reflection.Assembly.GetEntryAssembly(),
    services: host.Services
);

// Conexion a Discord
var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
    ?? throw new Exception("DISCORD_TOKEN no encontrado en .env");

await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

Console.WriteLine("AurisBot arrancando...");

// Mantener la aplicación en ejecución
await host.RunAsync();