using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace DutyChecklist;

public class DutyFinderButton : IDisposable
{
    private readonly Plugin plugin;

    public DutyFinderButton(Plugin plugin)
    {
        this.plugin = plugin;
    }

    public void Dispose()
    {
    }

    public unsafe void Draw()
    {
        try
        {
            var addonPtr = Service.GameGui.GetAddonByName("ContentsFinder");
            if (addonPtr.Address == nint.Zero)
                return;

            var addon = (AtkUnitBase*)addonPtr.Address;
            if (addon == null || !addon->IsVisible)
                return;

            var pos = new Vector2(addon->X, addon->Y);
            var scale = addon->Scale;

            var buttonPos = new Vector2(
                pos.X + 90 * scale,
                pos.Y + 37 * scale
            );

            ImGui.SetNextWindowPos(buttonPos);
            ImGui.SetNextWindowSize(new Vector2(135 * scale, 0));

            var flags = ImGuiWindowFlags.NoTitleBar
                      | ImGuiWindowFlags.NoResize
                      | ImGuiWindowFlags.NoMove
                      | ImGuiWindowFlags.NoScrollbar
                      | ImGuiWindowFlags.NoSavedSettings
                      | ImGuiWindowFlags.AlwaysAutoResize
                      | ImGuiWindowFlags.NoBackground;

            if (ImGui.Begin("##DutyChecklistButton", flags))
            {
                ImGui.SetWindowFontScale(scale);
                if (ImGui.Button("Unlock CHecklist", new Vector2(-1, 0)))
                {
                    plugin.MainWindow.IsOpen = !plugin.MainWindow.IsOpen;
                }
            }
            ImGui.End();
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Error drawing Duty Finder button");
        }
    }
}
