using System;
using Dalamud.Game.Command;
using Dalamud.Plugin;

namespace DutyChecklist;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "DutyChecklist";
    private const string CommandName = "/dutychecklist";
    private const string CommandNameShort = "/dcl";

    private IDalamudPluginInterface PluginInterface { get; init; }
    public Configuration Configuration { get; init; }
    public MainWindow MainWindow { get; init; }
    public DutyFinderButton DutyFinderButton { get; init; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        this.PluginInterface = pluginInterface;
        this.PluginInterface.Create<Service>();

        this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.Configuration.Initialize(this.PluginInterface);

        this.MainWindow = new MainWindow(this);
        this.DutyFinderButton = new DutyFinderButton(this);

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Duty Checklist window"
        });

        Service.CommandManager.AddHandler(CommandNameShort, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Duty Checklist window (shortcut)"
        });

        this.PluginInterface.UiBuilder.Draw += DrawUI;
        this.PluginInterface.UiBuilder.OpenMainUi += OpenMainUI;
    }

    public void Dispose()
    {
        this.PluginInterface.UiBuilder.Draw -= DrawUI;
        this.PluginInterface.UiBuilder.OpenMainUi -= OpenMainUI;

        Service.CommandManager.RemoveHandler(CommandName);
        Service.CommandManager.RemoveHandler(CommandNameShort);

        this.DutyFinderButton.Dispose();
        this.MainWindow.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        this.MainWindow.IsOpen = true;
    }

    private void DrawUI()
    {
        this.MainWindow.Draw();
        this.DutyFinderButton.Draw();
    }

    private void OpenMainUI()
    {
        this.MainWindow.IsOpen = true;
    }
}
