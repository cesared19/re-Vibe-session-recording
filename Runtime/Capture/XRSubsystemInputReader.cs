using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace XRSessionCapture
{
    /// <summary>
    /// Reads the headset and controllers from Unity's XR input subsystem (UnityEngine.XR.InputDevices). Every XR
    /// plug-in feeds it (OpenXR, Wave XR, ...), whether or not the Input System shows the devices, so it's the safety net
    /// when <see cref="XRInputReader"/> finds nothing. It has no aim (pointer) pose: the device pose stands in for it.
    /// </summary>
    public static class XRSubsystemInputReader
    {
        static readonly List<UnityEngine.XR.InputDevice> s_Devices = new List<UnityEngine.XR.InputDevice>();

        public static bool TryFindHeadset(out UnityEngine.XR.InputDevice device) => TryFind(InputDeviceCharacteristics.HeadMounted, out device);

        public static bool TryFindController(bool left, out UnityEngine.XR.InputDevice device)
        {
            var side = left ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right;
            return TryFind(InputDeviceCharacteristics.Controller | side, out device);
        }

        static bool TryFind(InputDeviceCharacteristics characteristics, out UnityEngine.XR.InputDevice device)
        {
            s_Devices.Clear();
            InputDevices.GetDevicesWithCharacteristics(characteristics, s_Devices);
            foreach (var candidate in s_Devices)
            {
                if (candidate.isValid)
                {
                    device = candidate;
                    s_Devices.Clear();
                    return true;
                }
            }
            device = default;
            return false;
        }

        public static HeadSample ReadHead()
        {
            if (!TryFindHeadset(out var device))
                return new HeadSample { pose = PoseSample.Identity };
            // The center eye is what the app sees; the device pose is the fallback.
            if (!device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyePosition, out var position))
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out position);
            if (!device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyeRotation, out var rotation))
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out rotation);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out var tracked);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trackingState, out var state);
            return new HeadSample
            {
                isTracked = tracked,
                trackingState = (int)state,
                pose = new PoseSample(position, PoseSample.Normalize(rotation)),
            };
        }

        public static ControllerSample ReadController(bool left)
        {
            if (!TryFindController(left, out var device))
                return default;
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out var position);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out var rotation);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out var tracked);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trackingState, out var state);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out var trigger);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out var grip);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out var stick);

            var pose = new PoseSample(position, PoseSample.Normalize(rotation));
            var buttons = ControllerButtons.None;
            // Some plug-ins give only the analog value, so a trigger or grip past half way counts as pressed.
            if (Bool(device, UnityEngine.XR.CommonUsages.triggerButton) || trigger > 0.5f) buttons |= ControllerButtons.TriggerPressed;
            if (Bool(device, UnityEngine.XR.CommonUsages.gripButton) || grip > 0.5f) buttons |= ControllerButtons.GripPressed;
            if (Bool(device, UnityEngine.XR.CommonUsages.primaryButton)) buttons |= ControllerButtons.Primary;
            if (Bool(device, UnityEngine.XR.CommonUsages.secondaryButton)) buttons |= ControllerButtons.Secondary;
            if (Bool(device, UnityEngine.XR.CommonUsages.menuButton)) buttons |= ControllerButtons.Menu;
            if (Bool(device, UnityEngine.XR.CommonUsages.primary2DAxisClick)) buttons |= ControllerButtons.StickClick;
            if (Bool(device, UnityEngine.XR.CommonUsages.primary2DAxisTouch)) buttons |= ControllerButtons.StickTouch;
            if (Bool(device, UnityEngine.XR.CommonUsages.primaryTouch)) buttons |= ControllerButtons.PrimaryTouch;
            if (Bool(device, UnityEngine.XR.CommonUsages.secondaryTouch)) buttons |= ControllerButtons.SecondaryTouch;

            return new ControllerSample
            {
                connected = true,
                isTracked = tracked,
                trackingState = (int)state,
                device = pose,
                pointer = pose,
                trigger = trigger,
                grip = grip,
                stick = stick,
                buttons = buttons,
            };
        }

        static bool Bool(UnityEngine.XR.InputDevice device, InputFeatureUsage<bool> usage) => device.TryGetFeatureValue(usage, out var value) && value;
    }
}
