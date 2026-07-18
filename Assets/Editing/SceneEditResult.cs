namespace UnitySimulationX.Editing
{
    public sealed class SceneEditResult
    {
        public bool Succeeded { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public SceneChangeSet ChangeSet { get; set; }
    }
}
