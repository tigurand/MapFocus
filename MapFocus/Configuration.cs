using System;
using Dalamud.Configuration;

namespace MapFocus;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool Enabled { get; set; } = true;
    public bool IsConfigWindowMovable { get; set; } = true;
    public bool StopRecenterOnPlayer { get; set; } = true;
    public bool CenterMapOnOpen { get; set; } = false;

    public bool ShowMapCenterButton { get; set; } = true;
    public bool MapButtonsVertical { get; set; } = false;
    public int MapButtonsSpacing { get; set; } = 4;
    public int MapCenterButtonOffsetX { get; set; } = 0;
    public int MapCenterButtonOffsetY { get; set; } = 0;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
