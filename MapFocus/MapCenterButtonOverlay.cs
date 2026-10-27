using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MapFocus;

public sealed unsafe class MapCenterButtonOverlay : IDisposable
{
    private const string AddonName = "AreaMap";
    private const string WindowId = "###MapFocusMapButtons";
    private const string PlayerButtonId = "##MapFocusCenterOnPlayer";
    private const string MapButtonId = "##MapFocusCenterOnMap";

    private const float ButtonSize = 28f;
    private const float EdgeMargin = 24f;

    private static readonly Vector4 IdleBackground = new(0f, 0f, 0f, 0.58f);
    private static readonly Vector4 HoveredBackground = new(0.06f, 0.06f, 0.08f, 0.82f);
    private static readonly Vector4 HeldBackground = new(0.10f, 0.10f, 0.12f, 0.92f);
    private static readonly Vector4 IdleBorder = new(1f, 1f, 1f, 0.55f);
    private static readonly Vector4 HoveredBorder = new(1f, 0.83f, 0.35f, 1f);
    private static readonly Vector4 HeldBorder = new(1f, 0.72f, 0.20f, 1f);
    private static readonly Vector4 IdleIconColor = new(1f, 1f, 1f, 0.95f);
    private static readonly Vector4 HoveredIconColor = new(1f, 0.90f, 0.62f, 1f);
    private static readonly Vector4 OutlineColor = new(0f, 0f, 0f, 0.55f);

    private readonly Plugin plugin;

    private bool enabled;

    public MapCenterButtonOverlay(Plugin plugin) => this.plugin = plugin;

    public void Enable()
    {
        if (enabled)
            return;

        Plugin.PluginInterface.UiBuilder.Draw += OnDraw;
        enabled = true;
    }

    public void Dispose()
    {
        if (!enabled)
            return;

        Plugin.PluginInterface.UiBuilder.Draw -= OnDraw;
        enabled = false;
    }

    private void OnDraw()
    {
        var cfg = plugin.Configuration;
        if (!cfg.Enabled || !cfg.ShowMapCenterButton)
            return;

        var addon = Plugin.GameGui.GetAddonByName(AddonName);
        if (addon.IsNull || !addon.IsReady || !addon.IsVisible)
            return;

        var unit = (AtkUnitBase*)addon.Address;
        var root = unit->RootNode;
        if (root is null)
            return;

        var scale = unit->Scale;
        if (!float.IsFinite(scale) || scale <= 0f)
            scale = 1f;

        var width = root->Width * scale;
        var height = root->Height * scale;
        if (width <= 0f || height <= 0f)
            return;

        var buttonSize = MathF.Round(ButtonSize * scale);
        if (buttonSize < 8f)
            buttonSize = 8f;

        var spacing = Math.Max(0, cfg.MapButtonsSpacing);
        var vertical = cfg.MapButtonsVertical;
        var margin = EdgeMargin * scale;
        var groupWidth = vertical ? buttonSize : (buttonSize * 2f) + spacing;
        var groupHeight = vertical ? (buttonSize * 2f) + spacing : buttonSize;

        var x = root->ScreenX + width - margin - groupWidth + cfg.MapCenterButtonOffsetX;
        var y = root->ScreenY + height - margin - groupHeight + cfg.MapCenterButtonOffsetY;

        using var windowPadding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        using var windowMinSize = ImRaii.PushStyle(ImGuiStyleVar.WindowMinSize, Vector2.Zero);
        using var windowBorder = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 0f);
        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 0f);
        using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);

        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoBackground |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoNavFocus |
            ImGuiWindowFlags.NoBringToFrontOnFocus;

        ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(groupWidth, groupHeight), ImGuiCond.Always);

        if (ImGui.Begin(WindowId, flags))
        {
            ImGui.SetWindowFontScale(scale);

            var drawList = ImGui.GetWindowDrawList();
            var buttonSizeVec = new Vector2(buttonSize, buttonSize);
            var thickness = MathF.Max(1f, scale);
            var radius = (buttonSize / 2f) - thickness;

            void DrawButton(Vector2 topLeft, FontAwesomeIcon icon, bool hovered, bool held)
            {
                var center = topLeft + (buttonSizeVec / 2f);

                drawList.AddCircle(center, radius + thickness, ImGui.GetColorU32(OutlineColor), 0, thickness * 1.5f);

                drawList.AddCircleFilled(
                    center,
                    radius,
                    ImGui.GetColorU32(held ? HeldBackground : hovered ? HoveredBackground : IdleBackground));

                drawList.AddCircle(
                    center,
                    radius,
                    ImGui.GetColorU32(held ? HeldBorder : hovered ? HoveredBorder : IdleBorder),
                    0,
                    thickness);

                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    var text = icon.ToIconString();
                    var textSize = ImGui.CalcTextSize(text);
                    var iconColor = hovered || held ? HoveredIconColor : IdleIconColor;
                    drawList.AddText(center - (textSize / 2f), ImGui.GetColorU32(iconColor), text);
                }
            }

            var groupTopLeft = ImGui.GetCursorScreenPos();
            var playerButtonPos = groupTopLeft;
            var mapButtonPos = vertical
                                   ? groupTopLeft + new Vector2(0f, buttonSize + spacing)
                                   : groupTopLeft + new Vector2(buttonSize + spacing, 0f);

            ImGui.SetCursorScreenPos(playerButtonPos);
            var playerClicked = ImGui.InvisibleButton(PlayerButtonId, buttonSizeVec);
            var playerHovered = ImGui.IsItemHovered();
            var playerHeld = ImGui.IsItemActive();

            ImGui.SetCursorScreenPos(mapButtonPos);
            var mapClicked = ImGui.InvisibleButton(MapButtonId, buttonSizeVec);
            var mapHovered = ImGui.IsItemHovered();
            var mapHeld = ImGui.IsItemActive();

            DrawButton(playerButtonPos, FontAwesomeIcon.User, playerHovered, playerHeld);
            DrawButton(mapButtonPos, FontAwesomeIcon.Crosshairs, mapHovered, mapHeld);

            if (playerHovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                ImGui.SetTooltip("Center on player");
            }
            else if (mapHovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                ImGui.SetTooltip("Center the map");
            }

            if (playerClicked)
                plugin.CenterOnPlayerNow();

            if (mapClicked)
                plugin.CenterMapNow();
        }

        ImGui.End();
    }
}
