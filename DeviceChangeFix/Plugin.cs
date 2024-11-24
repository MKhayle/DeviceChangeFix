using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using SharpDX.DirectInput;
using Dalamud.Utility.Signatures;

namespace DeviceChangeFix
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "DeviceChangeFix";

        private int controllerCount;
        private readonly DirectInput directInput = new();

        public delegate nint DeviceChangeDelegate(nint inputDeviceManager);

        private IPluginLog pluginLog { get; init; }

        // function that is called from WndProc when a device change happens
        [Signature("E8 ?? ?? ?? ?? 33 C0 E9 ?? ?? ?? ?? 41 8B C6", DetourName = nameof(DeviceChangeDetour))]
        private readonly Hook<DeviceChangeDelegate> deviceChangeDelegateHook = null!;

        public Plugin(IPluginLog pluginLog, IGameInteropProvider gameInteropProvider)
        {
            this.pluginLog = pluginLog;

            this.controllerCount = this.GetControllerCount();

            gameInteropProvider.InitializeFromAttributes(this);

            this.deviceChangeDelegateHook.Enable();
        }

        private unsafe nint DeviceChangeDetour(nint inputDeviceManager)
        {
            // Only call the original if the number of connected controllers
            // actually changed since the last time the hook was called
            int newControllerCount = this.GetControllerCount();
            if (newControllerCount != this.controllerCount)
            {
                this.pluginLog.Information($"{Math.Abs(newControllerCount - this.controllerCount)} device(s) {(newControllerCount > this.controllerCount ? "added" : "removed")}, polling started");
                this.controllerCount = newControllerCount;

                return this.deviceChangeDelegateHook.Original(inputDeviceManager);
            }
            this.pluginLog.Information("No input devices changed, polling skipped");

            return nint.Zero;
        }

        private int GetControllerCount()
        {
            return this.directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AllDevices).Count;
        }

        public void Dispose()
        {
            this.deviceChangeDelegateHook.Dispose();
            this.directInput.Dispose();
        }
    }
}
