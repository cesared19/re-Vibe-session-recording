namespace XRSessionCapture
{
    /// <summary>
    /// The input of the frame being replayed, for apps that read the headset and controllers themselves (Wave XR's own
    /// input classes, for example) and so can't be fed through the virtual Input System devices. While nothing is replayed,
    /// every call reads the real devices, so the same code runs live and in a replay.
    /// </summary>
    public static class ReplayInput
    {
        static FrameSample s_Frame;

        /// <summary>True while a capture is being replayed and a sample has been applied.</summary>
        public static bool IsReplaying { get; private set; }

        /// <summary>The recorded frame number being applied, or -1 when not replaying.</summary>
        public static int RecordedFrame => IsReplaying ? s_Frame.frame : -1;

        public static HeadSample ReadHead() => IsReplaying ? s_Frame.head : XRInputReader.ReadHead();

        public static ControllerSample ReadController(bool left) => IsReplaying ? (left ? s_Frame.left : s_Frame.right) : XRInputReader.ReadController(left);

        internal static void Set(FrameSample frame)
        {
            s_Frame = frame;
            IsReplaying = true;
        }

        internal static void Clear()
        {
            IsReplaying = false;
            s_Frame = default;
        }
    }
}
