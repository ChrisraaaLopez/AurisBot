using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using AurisBot.Services;

namespace AurisBot.Modules;

// InteractionModuleBase es la clase base para módulos de comandos de interacción (slash commands)
// SocketInteractionContext es el contexto específico para bots que usan Discord.Net con WebSocket (la mayoría de los casos)
public class MusicModule : InteractionModuleBase<SocketInteractionContext>
{
    // AudioService se inyecta automáticamente via DI
    private readonly AudioService _audio;

    public MusicModule(AudioService audio)
    {
        _audio = audio;
    }

    // /play
    [SlashCommand("play", "Reproduce una canción por nombre o link de YouTube")]
    public async Task PlayAsync(
        [Summary("busqueda", "Nombre de la canción o link de YouTube")] string query)
    {
        // Responde inmediatamente con un mensaje de "procesando" para evitar timeout (Discord espera respuesta en 3 segundos)
        await DeferAsync();

        // Verificar que el usuario está en un canal de voz
        var voiceState = Context.User as IVoiceState;

        var result = await _audio.PlayAsync(query, Context.Guild.Id, voiceState);

        // Editar la respuesta original con el resultado de la operación
        await ModifyOriginalResponseAsync(msg => msg.Content = result);
    }

    // /skip
    [SlashCommand("skip", "Salta la canción actual")]
    public async Task SkipAsync()
    {
        var result = await _audio.SkipAsync(Context.Guild.Id);
        await RespondAsync(result);
    }

    // /pause
    [SlashCommand("pause", "Pausa la reproducción")]
    public async Task PauseAsync()
    {
        var result = await _audio.PauseAsync(Context.Guild.Id);
        await RespondAsync(result);
    }

    // /resume
    [SlashCommand("resume", "Reanuda la reproducción")]
    public async Task ResumeAsync()
    {
        var result = await _audio.ResumeAsync(Context.Guild.Id);
        await RespondAsync(result);
    }

    // /queue
    [SlashCommand("queue", "Muestra la cola de canciones")]
    public async Task QueueAsync()
    {
        var result = await _audio.GetQueueAsync(Context.Guild.Id);

        // Mensaje embed para mostrar la cola de reproducción de forma más atractiva
        var embed = new EmbedBuilder()
            .WithTitle("🎵 Cola de reproducción")
            .WithDescription(result)
            .WithColor(new Color(168, 85, 247)) // Morado
            .WithFooter("AurisBot")
            .Build();

        await RespondAsync(embed: embed);
    }

    // /stop
    [SlashCommand("stop", "Detiene la reproducción y desconecta el bot")]
    public async Task StopAsync()
    {
        var result = await _audio.StopAsync(Context.Guild.Id);
        await RespondAsync(result);
    }
}