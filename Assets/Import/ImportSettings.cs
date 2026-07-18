namespace UnitySimulationX.Import
{
    public sealed class ImportSettings
    {
        public float UnitScale { get; set; } = 1f;
        public bool GenerateColliders { get; set; } = true;
        public bool PreserveHierarchy { get; set; } = true;
        public bool GenerateMaterials { get; set; } = true;
        public bool CenterOnImport { get; set; }
        public long MaxSourceBytes { get; set; } = 512L * 1024L * 1024L;
        public int MaxVertices { get; set; } = 5_000_000;
        public int MaxIndices { get; set; } = 15_000_000;
    }
}
