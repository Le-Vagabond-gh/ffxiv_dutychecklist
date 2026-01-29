using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Web;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace DutyChecklist;

public class MainWindow : IDisposable
{
    private readonly Plugin plugin;
    private List<DutyInfo> allDuties = new();
    private string searchFilter = string.Empty;
    private int selectedContentType = 0;
    private bool needsRefresh = true;
    public bool IsOpen = false;

    private static readonly string[] ContentTypeNames =
    [
        "All",
        "Dungeons",
        "Trials",
        "Raids",
        "Alliance Raids",
        "Guildhests",
        "PvP",
        "Other"
    ];

    public MainWindow(Plugin plugin)
    {
        this.plugin = plugin;
        Service.ClientState.Login += OnLogin;
    }

    public void Dispose()
    {
        Service.ClientState.Login -= OnLogin;
    }

    private void OnLogin()
    {
        needsRefresh = true;
    }

    public void Draw()
    {
        if (!IsOpen)
            return;

        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGui.SetNextWindowSize(new Vector2(920, 500), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Duty Checklist###DutyChecklist", ref this.IsOpen))
        {
            if (Service.ClientState.LocalPlayer == null)
            {
                ImGui.TextColored(new Vector4(1, 0.5f, 0.5f, 1), "Please log in to view duty information.");
                ImGui.End();
                return;
            }

            if (needsRefresh)
            {
                RefreshDutyList();
                needsRefresh = false;
            }

            DrawToolbar();
            ImGui.Separator();
            DrawDutyList();
        }
        ImGui.End();
    }

    private void DrawToolbar()
    {
        if (ImGui.Button("Refresh"))
        {
            needsRefresh = true;
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        ImGui.InputTextWithHint("##search", "Search...", ref searchFilter, 256);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        ImGui.Combo("##contentType", ref selectedContentType, ContentTypeNames, ContentTypeNames.Length);

        ImGui.SameLine();
        var showUnlocked = plugin.Configuration.ShowUnlockedDuties;
        if (ImGui.Checkbox("Show Unlocked", ref showUnlocked))
            plugin.Configuration.ShowUnlockedDuties = showUnlocked;

        ImGui.SameLine();
        var showLocked = plugin.Configuration.ShowLockedDuties;
        if (ImGui.Checkbox("Show Locked", ref showLocked))
            plugin.Configuration.ShowLockedDuties = showLocked;

        var filtered = GetFilteredDuties();
        var unlockedCount = filtered.Count(d => d.IsUnlocked);
        var totalCount = filtered.Count;
        ImGui.Text($"Progress: {unlockedCount}/{totalCount} ({(totalCount > 0 ? (unlockedCount * 100 / totalCount) : 0)}%%)");
    }

    private int sortColumnIndex = 2; // Default sort by Level
    private bool sortAscending = true;

    private unsafe void DrawDutyList()
    {
        var filtered = GetFilteredDuties();

        if (ImGui.BeginChild("DutyList", new Vector2(0, 0), false))
        {
            if (ImGui.BeginTable("DutyTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable | ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort, 60);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort, 50);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Wiki", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort, 40);
                ImGui.TableHeadersRow();

                try
                {
                    var sortSpecs = ImGui.TableGetSortSpecs();
                    if (sortSpecs.SpecsDirty)
                    {
                        if (sortSpecs.SpecsCount > 0)
                        {
                            var spec = sortSpecs.Specs;
                            sortColumnIndex = spec.ColumnIndex;
                            sortAscending = spec.SortDirection == ImGuiSortDirection.Ascending;
                        }
                        sortSpecs.SpecsDirty = false;
                    }
                }
                catch (Exception ex)
                {
                    Service.PluginLog.Error(ex, "Error getting sort specs");
                }

                var sorted = SortDuties(filtered);

                foreach (var duty in sorted)
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    if (duty.IsUnlocked)
                    {
                        ImGui.TextColored(new Vector4(0.5f, 1, 0.5f, 1), "Yes");
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(1, 0.5f, 0.5f, 1), "No");
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text(duty.Name);

                    ImGui.TableNextColumn();
                    ImGui.Text(duty.Level.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(duty.ContentTypeName);

                    ImGui.TableNextColumn();
                    ImGui.PushID((int)duty.RowId);
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.ExternalLinkAlt))
                    {
                        var searchQuery = HttpUtility.UrlEncode(duty.Name.Replace(" ", "+"));
                        var url = $"https://ffxiv.consolegameswiki.com/mediawiki/index.php?search={searchQuery}";
                        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Search on Wiki");
                    }
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }

    private List<DutyInfo> SortDuties(List<DutyInfo> duties)
    {
        IOrderedEnumerable<DutyInfo> sorted = sortColumnIndex switch
        {
            0 => sortAscending ? duties.OrderBy(d => d.IsUnlocked) : duties.OrderByDescending(d => d.IsUnlocked),
            1 => sortAscending ? duties.OrderBy(d => d.Name) : duties.OrderByDescending(d => d.Name),
            2 => sortAscending ? duties.OrderBy(d => d.Level) : duties.OrderByDescending(d => d.Level),
            3 => sortAscending ? duties.OrderBy(d => d.ContentTypeName) : duties.OrderByDescending(d => d.ContentTypeName),
            _ => duties.OrderBy(d => d.Level)
        };
        return sorted.ToList();
    }

    private List<DutyInfo> GetFilteredDuties()
    {
        var result = allDuties.AsEnumerable();

        if (!plugin.Configuration.ShowUnlockedDuties)
            result = result.Where(d => !d.IsUnlocked);

        if (!plugin.Configuration.ShowLockedDuties)
            result = result.Where(d => d.IsUnlocked);

        if (!string.IsNullOrWhiteSpace(searchFilter))
            result = result.Where(d => d.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase));

        if (selectedContentType > 0)
            result = result.Where(d => d.ContentTypeCategory == selectedContentType);

        return result.ToList();
    }

    private unsafe void RefreshDutyList()
    {
        try
        {
            allDuties.Clear();

            var cfcSheet = Service.DataManager.GetExcelSheet<ContentFinderCondition>();
            if (cfcSheet == null)
            {
                Service.PluginLog.Error("Failed to get ContentFinderCondition sheet");
                return;
            }

            foreach (var cfc in cfcSheet)
            {
                // Skip invalid entries
                if (cfc.RowId == 0)
                    continue;

                var name = cfc.Name.ExtractText();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var contentType = cfc.ContentType.RowId;
                var contentTypeName = cfc.ContentType.ValueNullable?.Name.ExtractText() ?? "Unknown";
                var level = cfc.ClassJobLevelRequired;

                bool isUnlocked = false;
                try
                {
                    // Use Content.RowId which points to the actual InstanceContent
                    isUnlocked = UIState.IsInstanceContentUnlocked(cfc.Content.RowId);
                }
                catch
                {
                    // Ignore unlock check failures
                }

                var dutyInfo = new DutyInfo
                {
                    RowId = cfc.RowId,
                    Name = name,
                    Level = level,
                    ContentType = contentType,
                    ContentTypeName = contentTypeName,
                    ContentTypeCategory = GetContentTypeCategory(contentType),
                    IsUnlocked = isUnlocked,
                };

                allDuties.Add(dutyInfo);
            }

            // Deduplicate entries with the same name (case-insensitive for "The" vs "the" variants)
            // Keep the unlocked one, or if both same status, keep higher RowId (newer entry)
            allDuties = allDuties
                .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(d => d.IsUnlocked).ThenByDescending(d => d.RowId).First())
                .ToList();

            Service.PluginLog.Information($"Loaded {allDuties.Count} duties");
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Error refreshing duty list");
        }
    }

    private int GetContentTypeCategory(uint contentType)
    {
        return contentType switch
        {
            2 => 1,  // Dungeons
            4 => 2,  // Trials
            5 => 3,  // Raids
            3 => 4,  // Alliance Raids
            1 => 5,  // Guildhests
            6 => 6,  // PvP
            _ => 7,  // Other
        };
    }

    private class DutyInfo
    {
        public uint RowId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public uint ContentType { get; set; }
        public string ContentTypeName { get; set; } = string.Empty;
        public int ContentTypeCategory { get; set; }
        public bool IsUnlocked { get; set; }
    }
}
