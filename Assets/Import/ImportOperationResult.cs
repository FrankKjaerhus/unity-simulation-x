using System.Collections.Generic;

namespace UnitySimulationX.Import
{
    public sealed class ImportOperationResult
    {
        public bool Succeeded { get; set; }
        public string ObjectId { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public IReadOnlyList<ImportWarning> Warnings { get; set; }
    }
}
