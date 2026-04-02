using Discord.WebSocket;
namespace AurisBot.Services;

public class GuildGuardService
{
    // Variable para almacenar el ID del servidor permitido, leído una sola vez al arrancar el bot
    private readonly ulong _allowedGuildId;

    // Constructor: lee el ALLOWED_GUILD_ID del .env una sola vez al arrancar
    public GuildGuardService()
    {
        var raw = Environment.GetEnvironmentVariable("ALLOWED_GUILD_ID")
            ?? throw new Exception("ALLOWED_GUILD_ID no encontrado en .env");

        // Convierte el string a ulong
        _allowedGuildId = ulong.Parse(raw);
    }

    // Valida si el servidor es el permitido, y si no lo es, abandona el servidor
    public async Task ValidateGuildAsync(SocketGuild guild)
    {
        if (guild.Id != _allowedGuildId)
        {
            Console.WriteLine($"Servidor no autorizado: {guild.Name} ({guild.Id}) — abandonando...");
            await guild.LeaveAsync();
            Console.WriteLine($"Bot abandonó el servidor {guild.Name}");
        }
        else
        {
            Console.WriteLine($"Servidor autorizado: {guild.Name} ({guild.Id})");
        }
    }
}