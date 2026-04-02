using Discord;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;

namespace AurisBot.Services;

public class AudioService
{
    // IAudioService: interfaz principal de Lavalink4NET
    // Se inyecta automáticamente via DI cuando llamas AddLavalink() en Program.cs
    private readonly IAudioService _lavalink;

    public AudioService(IAudioService lavalink)
    {
        _lavalink = lavalink;
    }

    // ── PLAY ─────────────────────────────────────────────────
    // query      = nombre de canción o link de YouTube
    // guildId    = ID del servidor
    // voiceState = estado de voz del usuario (nos dice en qué canal está)
    public async Task<string> PlayAsync(string query, ulong guildId, IVoiceState? voiceState)
    {
        // Verificar que el usuario está en un canal de voz
        if (voiceState?.VoiceChannel == null)
            return "Necesitas estar en un canal de voz.";

        var playerResult = await _lavalink.Players.RetrieveAsync<QueuedLavalinkPlayer, QueuedLavalinkPlayerOptions>(
            guildId,
            voiceState.VoiceChannel.Id,
            PlayerFactory.Queued,
            Options.Create(new QueuedLavalinkPlayerOptions()),
            new PlayerRetrieveOptions(ChannelBehavior: PlayerChannelBehavior.Join)
        );

        if (!playerResult.IsSuccess)
            return $"No se pudo conectar al canal de voz: {playerResult.Status}";

        var player = playerResult.Player;

        // Busqueda de la pista en Lavalink4NET
        // Detectar si es link directo o búsqueda por nombre
        TrackLoadResult searchResult;

        if (Uri.IsWellFormedUriString(query, UriKind.Absolute))
        {
            // Es un link directo
            searchResult = await _lavalink.Tracks.LoadTracksAsync(query, TrackSearchMode.None);
        }
        else
        {
            // Es una búsqueda por nombre
            searchResult = await _lavalink.Tracks.LoadTracksAsync(query, TrackSearchMode.YouTube);
        }

        if (!searchResult.HasMatches)
            return $"No encontré resultados para: `{query}`";

        var track = searchResult.Track;

        if (track == null)
            return "No se pudo obtener el track.";

        // Reproducir o agregar a la cola según el estado actual del player
        if (player.State == PlayerState.Playing || player.State == PlayerState.Paused)
        {
            // Ya hay algo reproduciéndose: agregar a la cola
            await player.Queue.AddAsync(new TrackQueueItem(track));
            return $"Agregado a la cola: **{track.Title}** `[{track.Duration:mm\\:ss}]`";
        }
        else
        {
            // No hay nada: reproducir inmediatamente
            await player.PlayAsync(track);
            return $"Reproduciendo: **{track.Title}** `[{track.Duration:mm\\:ss}]`";
        }
    }

    // Skip (saltar a la siguiente pista)
    public async Task<string> SkipAsync(ulong guildId)
    {
        var player = await GetPlayerAsync(guildId);
        if (player == null) return "No hay nada reproduciéndose.";

        var currentTrack = player.CurrentTrack?.Title ?? "Desconocido";

        await player.SkipAsync();

        var next = player.CurrentTrack?.Title;
        return next != null
            ? $"Saltado: **{currentTrack}** — Ahora: **{next}**"
            : $"Saltado: **{currentTrack}** — La cola está vacía.";
    }

    // Pause (pausar la reproducción)
    public async Task<string> PauseAsync(ulong guildId)
    {
        var player = await GetPlayerAsync(guildId);
        if (player == null) return "❌ No hay nada reproduciéndose.";

        if (player.State == PlayerState.Paused)
            return "Ya está pausado.";

        await player.PauseAsync();
        return "Reproducción pausada.";
    }

    // Resume (reanudar la reproducción)
    public async Task<string> ResumeAsync(ulong guildId)
    {
        var player = await GetPlayerAsync(guildId);
        if (player == null) return "No hay nada reproduciéndose.";

        if (player.State != PlayerState.Paused)
            return "No está pausado.";

        await player.ResumeAsync();
        return "Reproducción reanudada.";
    }

    // Stop (detener la reproducción y desconectar el bot)
    public async Task<string> StopAsync(ulong guildId)
    {
        var player = await GetPlayerAsync(guildId);
        if (player == null) return "No hay nada reproduciéndose.";

        await player.StopAsync();
        await player.DisconnectAsync();

        return "Reproducción detenida. Bot desconectado.";
    }

    // Queue (mostrar la cola actual)
    public async Task<string> GetQueueAsync(ulong guildId)
    {
        var player = await GetPlayerAsync(guildId);
        if (player == null) return "❌ No hay nada en la cola.";

        if (player.CurrentTrack == null && player.Queue.Count == 0)
            return "La cola está vacía.";

        var sb = new System.Text.StringBuilder();

        if (player.CurrentTrack != null)
            sb.AppendLine($"**Ahora:** {player.CurrentTrack.Title} `[{player.CurrentTrack.Duration:mm\\:ss}]`");

        if (player.Queue.Count > 0)
        {
            sb.AppendLine("\n**Cola:**");
            var i = 1;
            foreach (var item in player.Queue)
            {
                sb.AppendLine($"`{i}.` {item.Track?.Title} `[{item.Track?.Duration:mm\\:ss}]`");
                i++;
                if (i > 10) { sb.AppendLine("...y más"); break; }
            }
        }

        return sb.ToString();
    }

    // Helper para obtener el player del servidor
    private async Task<QueuedLavalinkPlayer?> GetPlayerAsync(ulong guildId)
    {
        return await _lavalink.Players.GetPlayerAsync<QueuedLavalinkPlayer>(guildId);
    }
}