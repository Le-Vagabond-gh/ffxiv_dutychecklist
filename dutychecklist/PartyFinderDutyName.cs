using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace DutyChecklist;

public class PartyFinderDutyName : IDisposable
{
    public PartyFinderDutyName(Plugin plugin)
    {
    }

    public void Dispose()
    {
    }

    public unsafe void Draw()
    {
        try
        {
            DrawDetailOverlay();
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Error drawing Party Finder duty name");
        }
    }

    private unsafe void DrawDetailOverlay()
    {
        var addonPtr = Service.GameGui.GetAddonByName("LookingForGroupDetail");
        if (addonPtr.Address == nint.Zero)
            return;

        var addon = (AddonLookingForGroupDetail*)addonPtr.Address;
        if (addon == null || !addon->AtkUnitBase.IsVisible)
            return;

        var dutyNameNode = addon->DutyNameTextNode;
        if (dutyNameNode == null)
            return;

        var currentText = dutyNameNode->NodeText.ToString();

        var agent = AgentLookingForGroup.Instance();
        if (agent == null)
            return;

        var dutyId = agent->LastViewedListing.DutyId;
        if (dutyId == 0)
            return;

        var dutyName = GetDutyName(dutyId);
        if (string.IsNullOrEmpty(dutyName))
            return;

        var isLocked = string.IsNullOrEmpty(currentText)
                    || currentText.Contains("Locked", StringComparison.OrdinalIgnoreCase)
                    || currentText.Contains("???", StringComparison.OrdinalIgnoreCase);

        if (!isLocked)
            return;

        var scale = addon->AtkUnitBase.Scale;
        var nodePos = new Vector2(
            dutyNameNode->ScreenX + dutyNameNode->Width * scale - 400 * scale,
            dutyNameNode->ScreenY + 3 * scale
        );

        ImGui.SetNextWindowPos(nodePos);

        var flags = ImGuiWindowFlags.NoTitleBar
                  | ImGuiWindowFlags.NoResize
                  | ImGuiWindowFlags.NoMove
                  | ImGuiWindowFlags.NoScrollbar
                  | ImGuiWindowFlags.NoSavedSettings
                  | ImGuiWindowFlags.AlwaysAutoResize
                  | ImGuiWindowFlags.NoBackground
                  | ImGuiWindowFlags.NoFocusOnAppearing
                  | ImGuiWindowFlags.NoNav;

        if (ImGui.Begin("##PartyFinderDutyNameDetail", flags))
        {
            ImGui.SetWindowFontScale(scale);
            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.5f, 1f), $"({dutyName})");
        }
        ImGui.End();
    }

    private string? GetDutyName(ushort dutyId)
    {
        var cfcSheet = Service.DataManager.GetExcelSheet<ContentFinderCondition>();
        if (cfcSheet == null)
            return null;

        foreach (var cfc in cfcSheet)
        {
            if (cfc.RowId == dutyId)
            {
                return cfc.Name.ExtractText();
            }
        }

        return null;
    }
}
