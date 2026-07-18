namespace UnitySimulationX.SceneModel
{
    public sealed class SceneInvariantException : System.InvalidOperationException
    {
        public SceneInvariantException(string code, string message) : base(message)
        {
            Code = code;
        }

        public string Code { get; }
    }
}
