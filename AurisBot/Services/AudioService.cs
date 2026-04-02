using Discord.WebSocket;
using Lavalink4NET; // Asegúrate de tener este using

namespace AurisBot.Services
{
    internal class AudioService
    {
        private readonly IAudioService _lavalink;

        // El constructor recibe el servicio real de Lavalink que configuramos en Program.cs
        public AudioService(IAudioService lavalink)
        {
            _lavalink = lavalink;
        }

        internal async Task InitializeAsync(DiscordSocketClient client)
        {
            // Aquí esperamos a que el servidor de Lavalink (el .jar) responda
            await _lavalink.WaitForReadyAsync();
            Console.WriteLine("✅ [AudioService] Lavalink está listo para reproducir música.");
        }
    }
}