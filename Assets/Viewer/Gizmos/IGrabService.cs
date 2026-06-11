namespace UnitySimulationX.Viewer.Gizmos
{
    public interface IGrabService
    {
        bool IsGrabbing { get; }

        /// <summary>
        /// Returns true once after grab confirm so selection input ignores the same LMB.
        /// </summary>
        bool ConsumeSelectionClick();
    }
}
