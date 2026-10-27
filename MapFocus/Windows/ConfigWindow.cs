using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ECommons;
using System;
using System.Numerics;

namespace MapFocus.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private const float ContentWidth = 380f;

    private static readonly Vector4 AccentColor = new(0.92f, 0.92f, 0.96f, 1f);
    private static readonly Vector4 MutedColor = new(0.62f, 0.62f, 0.68f, 1f);

    private readonly Plugin plugin;

    public ConfigWindow(Plugin plugin) : base("MapFocus Settings###MapFocusConfig")
    {
        this.plugin = plugin;
        Flags = ImGuiWindowFlags.AlwaysAutoResize;
        SizeCondition = ImGuiCond.Always;

        this.TitleBarButtons.Add(new TitleBarButton { ShowTooltip = () => ImGui.SetTooltip("Support Me"), Icon = FontAwesomeIcon.Heart, IconOffset = new Vector2(1, 1), Click = _ => GenericHelpers.ShellStart("https://linktr.ee/LucilleBagul") });
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        if (plugin.Configuration.IsConfigWindowMovable)
            Flags &= ~ImGuiWindowFlags.NoMove;
        else
            Flags |= ImGuiWindowFlags.NoMove;

        if (!plugin.Configuration.Enabled)
            ImGui.SetNextWindowSizeConstraints(new Vector2(250f, 0f), new Vector2(float.MaxValue, float.MaxValue));
    }

    public override void Draw()
    {
        var cfg = plugin.Configuration;
        var changed = false;

        var enabled = cfg.Enabled;
        if (ImGui.Checkbox("Enable MapFocus", ref enabled))
        {
            cfg.Enabled = enabled;
            changed = true;
        }

        if (!enabled)
        {
            if (changed)
                cfg.Save();
            return;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        SectionHeader("Map behavior");

        var stopCenter = cfg.StopRecenterOnPlayer;
        if (ImGui.Checkbox("Stop the map from centering on you", ref stopCenter))
        {
            cfg.StopRecenterOnPlayer = stopCenter;
            changed = true;
        }
        Hint("While on, the Area Map never automatically centers on your character.");

        ImGui.Spacing();

        var centerOnOpen = cfg.CenterMapOnOpen;
        if (ImGui.Checkbox("Center the map when it opens", ref centerOnOpen))
        {
            cfg.CenterMapOnOpen = centerOnOpen;
            changed = true;
        }
        Hint("Each time the map opens it is automatically centered on the map itself.");

        ImGui.Spacing();

        if (ImGui.Button("Center the map now"))
            plugin.CenterMapNow();
        Hint("Centers the open map on the map itself. "
             + "Also available as the \"/mapfocus center\" command.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        SectionHeader("Map button");

        var showMapButton = cfg.ShowMapCenterButton;
        if (ImGui.Checkbox("Show center buttons on the map", ref showMapButton))
        {
            cfg.ShowMapCenterButton = showMapButton;
            changed = true;
        }
        Hint("Adds two round buttons to the bottom-right corner of the map window: "
             + "center on player and center the map. Their layout is configured below.");

        if (showMapButton)
        {
            ImGui.Spacing();

            var vertical = cfg.MapButtonsVertical;
            if (ImGui.RadioButton("Horizontal", !vertical))
            {
                cfg.MapButtonsVertical = false;
                changed = true;
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("Vertical", vertical))
            {
                cfg.MapButtonsVertical = true;
                changed = true;
            }

            var spacingValue = cfg.MapButtonsSpacing;
            ImGui.SetNextItemWidth(160f);
            if (ImGui.DragInt("Button spacing (px)", ref spacingValue, 1f, 0, 100))
                cfg.MapButtonsSpacing = spacingValue;
            if (ImGui.IsItemDeactivatedAfterEdit())
                changed = true;

            var offsetX = cfg.MapCenterButtonOffsetX;
            ImGui.SetNextItemWidth(160f);
            if (ImGui.DragInt("Horizontal offset (px)", ref offsetX, 1f, -2000, 2000))
                cfg.MapCenterButtonOffsetX = offsetX;
            if (ImGui.IsItemDeactivatedAfterEdit())
                changed = true;

            var offsetY = cfg.MapCenterButtonOffsetY;
            ImGui.SetNextItemWidth(160f);
            if (ImGui.DragInt("Vertical offset (px)", ref offsetY, 1f, -2000, 2000))
                cfg.MapCenterButtonOffsetY = offsetY;
            if (ImGui.IsItemDeactivatedAfterEdit())
                changed = true;

            if (ImGui.Button("Reset button position"))
            {
                cfg.MapCenterButtonOffsetX = 0;
                cfg.MapCenterButtonOffsetY = 0;
                changed = true;
            }
            Hint("Puts both buttons back in their default bottom-right corner.");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        SectionHeader("Settings window");

        var movable = cfg.IsConfigWindowMovable;
        if (ImGui.Checkbox("Allow moving this window", ref movable))
        {
            cfg.IsConfigWindowMovable = movable;
            changed = true;
        }

        if (changed)
            cfg.Save();
    }

    private static void SectionHeader(string text)
    {
        ImGui.TextColored(AccentColor, text);
        ImGui.Spacing();
    }

    private static void Hint(string text)
    {
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(text);
    }
}
