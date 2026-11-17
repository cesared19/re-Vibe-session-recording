using System;
using System.Collections.Generic;

namespace XRSessionCapture
{
    /// <summary>Which XR plug-in the app runs on.</summary>
    public enum XRBackendKind
    {
        /// <summary>No XR plug-in is running (for example, in the Editor without a headset).</summary>
        None,
        OpenXR,
        /// <summary>HTC's Wave XR plug-in.</summary>
        WaveXR,
        /// <summary>Another XR plug-in.</summary>
        Other,
    }

    /// <summary>Where the headset and controllers are read from while recording.</summary>
    public enum CaptureSource
    {
        /// <summary>The Input System when it has the device, and Unity's XR input subsystem when it doesn't.</summary>
        Auto,
        /// <summary>Only the Input System (XRHMD and XRController devices).</summary>
        InputSystem,
        /// <summary>Only Unity's XR input subsystem (UnityEngine.XR.InputDevices), which every XR plug-in provides.</summary>
        XRSubsystem,
    }

    /// <summary>Decides which plug-in a set of names belongs to. It has no Unity types, so it can be tested on its own.</summary>
    public static class XRBackendClassifier
    {
        /// <summary>
        /// The first name that says which plug-in it is decides: OpenXR's subsystems and layouts have "OpenXR" in their
        /// names, and Wave's have "Wave" or "WVR" ("WVR Input Provider", "VIVE Controller (Wave)", ...). Names that say
        /// neither only mean that some other plug-in is running.
        /// </summary>
        public static XRBackendKind Classify(IEnumerable<string> names)
        {
            var other = false;
            if (names != null)
            {
                foreach (var name in names)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        continue;
                    if (name.IndexOf("openxr", StringComparison.OrdinalIgnoreCase) >= 0)
                        return XRBackendKind.OpenXR;
                    if (name.IndexOf("wave", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("wvr", StringComparison.OrdinalIgnoreCase) >= 0)
                        return XRBackendKind.WaveXR;
                    other = true;
                }
            }
            return other ? XRBackendKind.Other : XRBackendKind.None;
        }

        public static string DisplayName(XRBackendKind kind)
        {
            switch (kind)
            {
                case XRBackendKind.OpenXR: return "OpenXR";
                case XRBackendKind.WaveXR: return "Wave XR";
                case XRBackendKind.Other: return "Other XR";
                default: return "No XR";
            }
        }

        /// <summary>A few letters for the overlay's first line; empty when there's nothing to say.</summary>
        public static string ShortName(XRBackendKind kind)
        {
            switch (kind)
            {
                case XRBackendKind.OpenXR: return "OpenXR";
                case XRBackendKind.WaveXR: return "Wave";
                case XRBackendKind.Other: return "XR";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Whether a binding path is made for the generic devices a replay creates (&lt;XRController&gt;, &lt;XRHMD&gt;,
        /// &lt;TrackedDevice&gt;) or for another kind of device. A binding to a plug-in's own layout, such as
        /// &lt;ViveWaveController&gt;, doesn't follow a replay. Paths without a layout, and those for non-XR devices
        /// such as the keyboard, aren't a problem.
        /// </summary>
        public static bool IsVendorSpecificXRLayout(string bindingPath, out string layout)
        {
            layout = null;
            if (string.IsNullOrEmpty(bindingPath) || bindingPath[0] != '<')
                return false;
            var end = bindingPath.IndexOf('>');
            if (end < 2)
                return false;
            layout = bindingPath.Substring(1, end - 1);
            switch (layout)
            {
                case "XRController":
                case "XRControllerWithRumble":
                case "XRHMD":
                case "TrackedDevice":
                case "*":
                    return false;
            }
            // The layouts that Wave and the OpenXR plug-ins register for their devices.
            return layout.IndexOf("Wave", StringComparison.OrdinalIgnoreCase) >= 0
                || layout.IndexOf("OpenXR", StringComparison.OrdinalIgnoreCase) >= 0
                || layout.IndexOf("Vive", StringComparison.OrdinalIgnoreCase) >= 0
                || layout.IndexOf("Oculus", StringComparison.OrdinalIgnoreCase) >= 0
                || layout.IndexOf("Quest", StringComparison.OrdinalIgnoreCase) >= 0
                || layout.IndexOf("Index", StringComparison.OrdinalIgnoreCase) >= 0
                || layout.IndexOf("WMR", StringComparison.OrdinalIgnoreCase) >= 0
                || layout.IndexOf("HPMixed", StringComparison.OrdinalIgnoreCase) >= 0
                || layout.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0
                || layout.IndexOf("Eye", StringComparison.OrdinalIgnoreCase) >= 0
                || layout.IndexOf("Tracker", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
