using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace XRSessionCapture
{
    /// <summary>Finds out which XR plug-in is running (OpenXR or Wave XR) and writes what it sees, to help when something doesn't follow.</summary>
    public static class XRBackendInfo
    {
        /// <summary>The running plug-in, judged from the loaded XR device, the active XR subsystems and the devices the Input System has.</summary>
        public static XRBackendKind Detect()
        {
            return XRBackendClassifier.Classify(CollectNames(includeInputSystemDevices: true));
        }

        static IEnumerable<string> CollectNames(bool includeInputSystemDevices)
        {
            var names = new List<string>();
            try
            {
                names.Add(XRSettings.loadedDeviceName);
            }
            catch
            {
                // XRSettings isn't available on every platform
            }
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances(displays);
            foreach (var display in displays)
                if (display.running && display.subsystemDescriptor != null)
                    names.Add(display.subsystemDescriptor.id);
            var inputs = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances(inputs);
            foreach (var input in inputs)
                if (input.running && input.subsystemDescriptor != null)
                    names.Add(input.subsystemDescriptor.id);
            if (includeInputSystemDevices)
            {
                foreach (var device in InputSystem.devices)
                {
                    if (device is ReplayHMD || device is ReplayController)
                        continue;
                    names.Add(device.layout);
                    names.Add(device.description.product);
                }
            }
            return names;
        }

        /// <summary>Describes the XR setup in text: plug-in, subsystems, devices of both input paths and which one the capture reads.</summary>
        public static string Describe(CaptureSource source)
        {
            var sb = new StringBuilder();
            var kind = Detect();
            sb.AppendLine("re-Play XR diagnostics");
            sb.AppendLine("Unity: " + Application.unityVersion + " · " + Application.platform);
            sb.AppendLine("XR plug-in: " + XRBackendClassifier.DisplayName(kind) + " (loaded device: \"" + XRSettings.loadedDeviceName + "\")");
            sb.AppendLine("Capture source setting: " + source);

            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances(displays);
            foreach (var d in displays)
                sb.AppendLine("Display subsystem: " + (d.subsystemDescriptor != null ? d.subsystemDescriptor.id : "?") + (d.running ? " (running)" : " (stopped)"));
            var inputs = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances(inputs);
            foreach (var i in inputs)
                sb.AppendLine("Input subsystem: " + (i.subsystemDescriptor != null ? i.subsystemDescriptor.id : "?") + (i.running ? " (running)" : " (stopped)"));

            sb.AppendLine();
            sb.AppendLine("Input System devices:");
            foreach (var device in InputSystem.devices)
            {
                var virtualDevice = device is ReplayHMD || device is ReplayController ? " [re-Play virtual]" : string.Empty;
                sb.AppendLine("  " + device.layout + " · \"" + device.description.product + "\" · " + device.description.manufacturer + virtualDevice);
            }

            sb.AppendLine();
            sb.AppendLine("XR subsystem devices (UnityEngine.XR.InputDevices):");
            var all = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevices(all);
            foreach (var device in all)
                sb.AppendLine("  " + device.name + " · " + device.characteristics + (device.isValid ? string.Empty : " (not valid)"));

            sb.AppendLine();
            sb.AppendLine("Capture read right now:");
            sb.AppendLine("  Input System has the headset: " + (XRInputReader.FindHeadset() != null));
            sb.AppendLine("  Input System has the left / right controller: " + (XRInputReader.FindController(true) != null) + " / " + (XRInputReader.FindController(false) != null));
            sb.AppendLine("  XR subsystem has the headset: " + XRSubsystemInputReader.TryFindHeadset(out _));
            sb.AppendLine("  XR subsystem has the left / right controller: " + XRSubsystemInputReader.TryFindController(true, out _) + " / " + XRSubsystemInputReader.TryFindController(false, out _));
            return sb.ToString();
        }

        /// <summary>
        /// The enabled input actions that are bound to a plug-in's own device layout (&lt;ViveWaveController&gt;, ...). The virtual
        /// devices of a replay are generic XR devices, so these bindings don't follow it. Empty when there are none.
        /// </summary>
        public static List<string> FindVendorSpecificBindings()
        {
            var found = new List<string>();
            var actions = new List<InputAction>();
            InputSystem.ListEnabledActions(actions);
            foreach (var action in actions)
            {
                foreach (var binding in action.bindings)
                {
                    if (XRBackendClassifier.IsVendorSpecificXRLayout(binding.effectivePath, out var layout))
                    {
                        found.Add(action.name + " → " + layout);
                        break;
                    }
                }
            }
            return found;
        }

        /// <summary>"Input System", "XR subsystem" or "Mixed", from what the last reads used.</summary>
        public static string SourceLabel()
        {
            var sub = (XRInputReader.LastHeadFromSubsystem ? 1 : 0) + (XRInputReader.LastLeftFromSubsystem ? 1 : 0) + (XRInputReader.LastRightFromSubsystem ? 1 : 0);
            return sub == 0 ? "Input System" : sub == 3 ? "XR subsystem" : "Mixed";
        }
    }
}
