using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace DutyChecklist;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool ShowUnlockedDuties { get; set; } = true;
    public bool ShowLockedDuties { get; set; } = true;

    [NonSerialized]
    private IDalamudPluginInterface? PluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.PluginInterface = pluginInterface;
    }

    public void Save()
    {
        this.PluginInterface!.SavePluginConfig(this);
    }
}
