using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(
    typeof(FPSUnlock.Core),
    "FPSUnlock",
    "1.1.0",
    "Lebenoa",
    null
)]

namespace FPSUnlock;

using System.Runtime.InteropServices;

public sealed class Core : MelonMod
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern int MessageBoxW(
        IntPtr hWnd, string text, string caption, uint type);

    private const string ConfigCategory = "FPSUnlock";
    private const float BootApplyDuration = 5f;

    private MelonPreferences_Category _config = null!;

    private MelonPreferences_Entry<bool> _enabled = null!;
    private MelonPreferences_Entry<int> _targetFps = null!;
    private MelonPreferences_Entry<bool> _disableVSync = null!;
    private MelonPreferences_Entry<bool> _forceEveryFrame = null!;
    private MelonPreferences_Entry<KeyCode> _toggleKey = null!;

    private bool _showMenu;
    private bool _toggleKeyWasDown;
    private bool _imGuiBroken;
    private bool _textBoxBroken;
    private float _initTime;
    private string _targetFpsText = string.Empty;
    private int _originalTargetFrameRate;
    private int _originalVSyncCount;

    public override void OnInitializeMelon()
    {
        _config = MelonPreferences.CreateCategory(
            ConfigCategory,
            "FPS Unlock"
        );

        _enabled = _config.CreateEntry(
            "Enabled",
            true,
            "Enable FPS unlocking."
        );

        _targetFps = _config.CreateEntry(
            "TargetFPS",
            240,
            "Target FPS. Use -1 for unlimited."
        );

        _disableVSync = _config.CreateEntry(
            "DisableVSync",
            true,
            "Disable Unity VSync."
        );

        _forceEveryFrame = _config.CreateEntry(
            "ForceEveryFrame",
            false,
            "Reapply FPS settings every frame."
        );

        _toggleKey = _config.CreateEntry(
            "ToggleMenuKey",
            KeyCode.F6,
            "Key used to open/close configuration UI."
        );

        ClampTargetFps();
        SyncTargetFpsText();

        _initTime = Time.time;
        _originalTargetFrameRate = Application.targetFrameRate;
        _originalVSyncCount = QualitySettings.vSyncCount;

        ApplySettings();

        MelonPreferences.Save();

        LoggerInstance.Msg("Initialized.");
    }

    public override void OnUpdate()
    {
        bool down = IsToggleKeyDown();
        if (down && !_toggleKeyWasDown)
        {
            if (_imGuiBroken)
                NotifyImGuiUnavailable(showMessageBox: false);
            else
                _showMenu = !_showMenu;
        }
        _toggleKeyWasDown = down;

        if (!_enabled.Value)
            return;

        // The game's own startup overwrites targetFrameRate/vSyncCount after
        // OnInitializeMelon; re-apply for the first few seconds so the saved
        // config takes effect at boot.
        if (_forceEveryFrame.Value || Time.time - _initTime < BootApplyDuration)
            ApplySettings();
    }

    private bool IsToggleKeyDown()
    {
        int vk = KeyCodeToVK(_toggleKey.Value);
        if (vk == 0)
            return false;

        const short highBit = unchecked((short)0x8000);
        return (GetAsyncKeyState(vk) & highBit) != 0;
    }

    private static int KeyCodeToVK(KeyCode key)
    {
        // Function keys: KeyCode.F1 = 282 .. KeyCode.F12 = 293 map to VK_F1..VK_F12
        if (key >= KeyCode.F1 && key <= KeyCode.F12)
            return 0x70 + (int)(key - KeyCode.F1);

        // Letters: KeyCode.A = 97 .. KeyCode.Z = 122 map straight to VK_A..VK_Z
        if (key >= KeyCode.A && key <= KeyCode.Z)
            return (int)key;

        // Digits: KeyCode.Alpha0 = 48 .. Alpha9 = 57 map to VK_0..VK_9
        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
            return (int)'0' + (int)(key - KeyCode.Alpha0);

        return 0;
    }

    public override void OnGUI()
    {
        if (!_showMenu)
            return;

        try
        {
            GuiRenderMenu();
        }
        catch (Exception e)
        {
            OnImGuiUnavailable(e);
        }
    }

    private void GuiRenderMenu()
    {
        const float x = 20f;
        const float y = 20f;
        const float width = 360f;
        const float height = 270f;

        GUI.Box(
            new Rect(x, y, width, height),
            "FPS Unlock"
        );

        bool enabled = GUI.Toggle(
            new Rect(x + 15f, y + 35f, 200f, 25f),
            _enabled.Value,
            "Enabled"
        );

        if (enabled != _enabled.Value)
        {
            _enabled.Value = enabled;
            ApplySettings();
            SavePreferences();
        }

        bool vsync = GUI.Toggle(
            new Rect(x + 15f, y + 65f, 200f, 25f),
            _disableVSync.Value,
            "Disable VSync"
        );

        if (vsync != _disableVSync.Value)
        {
            _disableVSync.Value = vsync;
            ApplySettings();
            SavePreferences();
        }

        GUI.Label(
            new Rect(x + 15f, y + 100f, 130f, 25f),
            "Target FPS:"
        );

        GuiRenderTargetFpsInput(x, y);

        GUI.Label(
            new Rect(x + 15f, y + 130f, 300f, 20f),
            _targetFps.Value == -1
                ? "Current: Unlimited"
                : $"Current: {_targetFps.Value} FPS"
        );

        int sliderValue = _targetFps.Value == -1
            ? 1000
            : _targetFps.Value;

        int fps = Mathf.RoundToInt(
            GUI.HorizontalSlider(
                new Rect(x + 15f, y + 155f, 300f, 20f),
                sliderValue,
                30f,
                1000f
            )
        );

        if (fps != sliderValue)
        {
            _targetFps.Value = fps;
            SyncTargetFpsText();
            ApplySettings();
            SavePreferences();
        }

        if (GUI.Button(
            new Rect(x + 15f, y + 185f, 100f, 30f),
            "Unlimited"
        ))
        {
            _targetFps.Value = -1;
            SyncTargetFpsText();
            ApplySettings();
            SavePreferences();
        }

        if (GUI.Button(
            new Rect(x + 125f, y + 185f, 100f, 30f),
            "240 FPS"
        ))
        {
            _targetFps.Value = 240;
            SyncTargetFpsText();
            ApplySettings();
            SavePreferences();
        }

        bool force = GUI.Toggle(
            new Rect(x + 15f, y + 225f, 250f, 25f),
            _forceEveryFrame.Value,
            "Force every frame"
        );

        if (force != _forceEveryFrame.Value)
        {
            _forceEveryFrame.Value = force;
            SavePreferences();
        }
    }

    private void OnImGuiUnavailable(Exception e)
    {
        _imGuiBroken = true;
        _showMenu = false;
        LoggerInstance.Error(
            "IMGUI menu disabled: engine method stripped by IL2CPP " +
            $"({e.GetType().Name}: {e.Message}). " +
            "Configure via MelonLoader settings."
        );
        NotifyImGuiUnavailable(showMessageBox: true);
    }

    private void NotifyImGuiUnavailable(bool showMessageBox)
    {
        const string msg =
            "FPSUnlock: this build's IL2CPP stripped the IMGUI " +
            "methods the menu needs, so the in-game menu is disabled.\n\n" +
            "Configure FPSUnlock via MelonLoader's settings instead " +
            "(UserData/MelonPreferences.cfg).";
        LoggerInstance.Warning(msg);
        if (showMessageBox)
            MessageBoxW(IntPtr.Zero, msg, "FPSUnlock", 0x00000040 /* MB_ICONINFORMATION */);
    }

    private void GuiRenderTargetFpsInput(float x, float y)
    {
        if (_textBoxBroken)
            return;

        try
        {
            _targetFpsText = GUI.TextField(
                new Rect(x + 95f, y + 97f, 100f, 25f),
                _targetFpsText,
                5
            );

            if (GUI.Button(
                new Rect(x + 205f, y + 97f, 70f, 25f),
                "Apply"
            ))
            {
                ApplyTargetFps();
            }
        }
        catch
        {
            // DoTextField was stripped by this build's IL2CPP; disable the
            // text box and keep the rest of the menu (slider + buttons).
            _textBoxBroken = true;
            LoggerInstance.Warning(
                "IMGUI text field stripped by IL2CPP; " +
                "using slider + preset buttons only."
            );
        }
    }

    private void ApplyTargetFps()
    {
        if (!int.TryParse(_targetFpsText, out int fps))
        {
            SyncTargetFpsText();
            return;
        }

        if (fps != -1)
            fps = Mathf.Clamp(fps, 30, 1000);

        _targetFps.Value = fps;
        ApplySettings();
        SavePreferences();
    }

    private void ApplySettings()
    {
        if (!_enabled.Value)
        {
            Application.targetFrameRate = _originalTargetFrameRate;
            QualitySettings.vSyncCount = _originalVSyncCount;
            return;
        }

        QualitySettings.vSyncCount = _disableVSync.Value ? 0 : 1;

        Application.targetFrameRate = _targetFps.Value;
    }

    private void ClampTargetFps()
    {
        if (_targetFps.Value == -1)
            return;

        _targetFps.Value = Mathf.Clamp(
            _targetFps.Value,
            30,
            1000
        );
    }

    private void SyncTargetFpsText()
    {
        _targetFpsText = _targetFps.Value.ToString();
    }

    private static void SavePreferences()
    {
        MelonPreferences.Save();
    }
}