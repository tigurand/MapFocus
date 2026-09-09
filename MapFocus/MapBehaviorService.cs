using System;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace MapFocus;

public sealed unsafe class MapBehaviorService : IDisposable
{
    private const long ViewCommitMs = 800;
    private const long FollowUpMs = 150;

    private readonly Plugin plugin;

    private delegate void CenterOnPlayerDelegate(Atk2DAreaMap* map);

    private Hook<CenterOnPlayerDelegate>? centerOnPlayerHook;

    private bool wasVisible;
    private long openedAtTick;

    private float savedOffX;
    private float savedOffY;
    private bool hasSavedView;

    private bool manualCenterPending;
    private bool pendingSet;
    private float pendingX;
    private float pendingY;
    private int pendingAttempts;
    private long pendingAtTick;

    public MapBehaviorService(Plugin plugin) => this.plugin = plugin;

    public void Enable()
    {
        Plugin.Framework.Update += OnUpdate;

        try
        {
            var address = Atk2DAreaMap.Addresses.CenterOnPlayer.Value;
            if (address == nint.Zero)
            {
                Plugin.Log.Warning("Could not resolve Atk2DAreaMap::CenterOnPlayer.");
                return;
            }

            centerOnPlayerHook = Plugin.GameInterop.HookFromAddress<CenterOnPlayerDelegate>(address, CenterOnPlayerDetour);
            centerOnPlayerHook.Enable();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to hook Atk2DAreaMap::CenterOnPlayer.");
        }
    }

    public void Dispose()
    {
        Plugin.Framework.Update -= OnUpdate;
        centerOnPlayerHook?.Dispose();
    }

    public void CenterMapNow() => manualCenterPending = true;

    public void OnCommand(string args)
    {
        var a = args.Trim();
        if (a.Equals("toggle", StringComparison.OrdinalIgnoreCase))
        {
            plugin.Configuration.Enabled = !plugin.Configuration.Enabled;
            plugin.Configuration.Save();
            return;
        }

        if (a.Equals("center", StringComparison.OrdinalIgnoreCase) || a.Equals("fit", StringComparison.OrdinalIgnoreCase))
        {
            CenterMapNow();
            return;
        }

        plugin.ToggleConfigUi();
    }

    private void OnUpdate(IFramework _)
    {
        var cfg = plugin.Configuration;
        if (!cfg.Enabled)
        {
            wasVisible = false;
            manualCenterPending = false;
            pendingSet = false;
            pendingAttempts = 0;
            return;
        }

        var addon = Plugin.GameGui.GetAddonByName("AreaMap");
        var visible = !addon.IsNull && addon.IsVisible;

        if (visible && !wasVisible)
        {
            openedAtTick = Environment.TickCount64;

            if (manualCenterPending)
            {
                manualCenterPending = false;
                SetPending(0f, 0f);
            }
            else if (cfg.CenterMapOnOpen)
            {
                SetPending(0f, 0f);
            }
            else if (cfg.StopRecenterOnPlayer && hasSavedView)
            {
                SetPending(savedOffX, savedOffY);
            }
        }

        wasVisible = visible;

        if (!visible)
            return;

        var map = (AddonAreaMap*)addon.Address;
        ref var area = ref map->AreaMap;

        if (cfg.StopRecenterOnPlayer || cfg.CenterMapOnOpen || manualCenterPending || pendingSet)
        {
            UncheckFollowPlayer(map);
        }

        var now = Environment.TickCount64;
        if (cfg.StopRecenterOnPlayer && now - openedAtTick > ViewCommitMs)
        {
            savedOffX = area.MapOffsetX;
            savedOffY = area.MapOffsetY;
            hasSavedView = true;
        }

        if (manualCenterPending)
        {
            manualCenterPending = false;
            SetPending(0f, 0f);
        }

        if (pendingSet && pendingAttempts > 0 && now >= pendingAtTick)
        {
            ApplyPending(map);
            pendingAttempts--;
            if (pendingAttempts > 0)
                pendingAtTick = now + FollowUpMs;
            else
                pendingSet = false;
        }
    }

    private void SetPending(float offX, float offY)
    {
        pendingX = offX;
        pendingY = offY;
        pendingSet = true;
        pendingAttempts = 2;
        pendingAtTick = Environment.TickCount64;
    }

    private void CenterOnPlayerDetour(Atk2DAreaMap* map)
    {
        var cfg = plugin.Configuration;
        if (cfg.Enabled && (cfg.StopRecenterOnPlayer || cfg.CenterMapOnOpen) && IsCurrentAreaMap(map))
        {
            UncheckFollowPlayer(AreaMapToAddon(map));
            return;
        }

        centerOnPlayerHook?.Original(map);
    }

    private bool IsCurrentAreaMap(Atk2DAreaMap* map)
    {
        var addon = Plugin.GameGui.GetAddonByName("AreaMap");
        if (addon.IsNull || !addon.IsVisible)
            return false;

        var areaMap = (AddonAreaMap*)addon.Address;
        return &areaMap->AreaMap == map;
    }

    private static AddonAreaMap* AreaMapToAddon(Atk2DAreaMap* areaMap)
        => (AddonAreaMap*)((byte*)areaMap - 0x3A8);

    private void UncheckFollowPlayer(AddonAreaMap* map)
    {
        if (map == null)
            return;

        var followCheckbox = map->FollowPlayerCheckbox;
        if (followCheckbox != null && followCheckbox->IsChecked)
            followCheckbox->SetChecked(false);
    }

    private void ApplyPending(AddonAreaMap* map)
    {
        if (map == null)
            return;

        UncheckFollowPlayer(map);

        ref var area = ref map->AreaMap;
        area.MapOffsetX = pendingX;
        area.MapOffsetY = pendingY;
        area.ClampPosition();
    }
}
