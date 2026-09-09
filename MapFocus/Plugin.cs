using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using MapFocus.Windows;

namespace MapFocus;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/mapfocus";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;

    public Configuration Configuration { get; }
    public readonly WindowSystem WindowSystem = new("MapFocus");
    private readonly ConfigWindow configWindow;
    private readonly MapBehaviorService mapBehavior;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        configWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(configWindow);
        mapBehavior = new MapBehaviorService(this);
        mapBehavior.Enable();

        CommandManager.AddHandler(CommandName, new CommandInfo((_, args) => mapBehavior.OnCommand(args ?? string.Empty))
        {
            HelpMessage = "Open MapFocus settings. Subcommands: center | toggle",
        });

        PluginInterface.UiBuilder.Draw += () => WindowSystem.Draw();
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUi;
    }

    public void Dispose()
    {
        mapBehavior.Dispose();
        CommandManager.RemoveHandler(CommandName);
        WindowSystem.RemoveAllWindows();
        configWindow.Dispose();
    }

    public void ToggleConfigUi() => configWindow.Toggle();

    public void CenterMapNow() => mapBehavior.CenterMapNow();
}
