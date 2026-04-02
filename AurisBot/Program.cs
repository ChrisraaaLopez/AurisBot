using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using DotNetEnv;
using AurisBot.Services;

// Cargar variables de entorno desde el archivo .env
Env.Load("../.env");

// Configuración del bot
var config = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds            // Eventos relacionados con servidores 
                    | GatewayIntents.GuildMessages    // Mensajes en los canales
                    | GatewayIntents.GuildVoiceStates, // Cambios entre canales de voz
    
    LogLevel = LogSeverity.Info, // Nivel de log
};

// Contenedor de dependencias
var services = new ServiceCollection()
    .AddSingleton(new DiscordSocketClient(config))  // Instancia cliente de discord
    .AddSingleton<InteractionService>()             // Servicio de interacciones (comandos slash)
    .AddSingleton<GuildGuardService>()              // Servicio para verificar que los comandos se ejecuten solo en servidores específicos
    .AddSingleton<AudioService>()                   // Cola de canciones y reproducción de audio 
    .BuildServiceProvider();

// Variables para obtener las instancias de los servicios
var client = services.GetRequiredService<DiscordSocketClient>();
var interactionService = services.GetRequiredService<InteractionService>();
var guard = services.GetRequiredService<GuildGuardService>();
var audio = services.GetRequiredService<AudioService>();

// Logs
client.Log += log =>
{
    Console.WriteLine($"[{log.Severity}] {log.Source}: {log.Message}");
    return Task.CompletedTask;
};

// Eventos (Aqui se configuran los eventos para el cliente)

/* Listo: Cuando el bot se conecta a Discord
client.Ready += async () =>
{
Console.WriteLine($"Bot conectado: {client.CurrentUser.Username}"); */


