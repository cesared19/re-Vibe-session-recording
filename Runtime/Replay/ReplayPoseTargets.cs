using UnityEngine;

namespace XRSessionCapture
{
    /// <summary>
    /// Moves the headset camera and the controller objects to the recorded poses while a capture is replayed. It works
    /// whatever the XR plug-in is (OpenXR, Wave XR, ...) because it doesn't depend on Input System devices, so use it on
    /// projects whose tracking doesn't follow the virtual devices. Add it once to any object; it does nothing outside a replay.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    [AddComponentMenu("XR Session Capture/Replay Pose Targets")]
    public sealed class ReplayPoseTargets : MonoBehaviour
    {
        [Tooltip("The camera that follows the recorded head. Empty: Camera.main.")]
        public Transform head;
        public Transform leftController;
        public Transform rightController;
        [Tooltip("The rig the poses are relative to (the XR Origin or Camera Offset). Empty: the head's parent.")]
        public Transform trackingSpace;

        void OnEnable() => Application.onBeforeRender += Apply;

        void OnDisable() => Application.onBeforeRender -= Apply;

        void LateUpdate() => Apply();

        void Apply()
        {
            if (!ReplayInput.IsReplaying)
                return;
            var headTransform = head != null ? head : (Camera.main != null ? Camera.main.transform : null);
            var space = trackingSpace != null ? trackingSpace : (headTransform != null ? headTransform.parent : null);
            if (headTransform != null)
                Place(headTransform, ReplayInput.ReadHead().pose, space);
            var left = ReplayInput.ReadController(true);
            var right = ReplayInput.ReadController(false);
            if (leftController != null && left.connected)
                Place(leftController, left.device, space);
            if (rightController != null && right.connected)
                Place(rightController, right.device, space);
        }

        static void Place(Transform target, PoseSample pose, Transform space)
        {
            if (space != null)
                target.SetPositionAndRotation(space.TransformPoint(pose.position), space.rotation * pose.rotation);
            else
                target.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }
}
