namespace UnitySimulationX.Import
{
    public sealed class ImportSettings
    {
        public float UnitScale { get; set; } = 1f;
        public bool GenerateColliders { get; set; } = true;
        public bool PreserveHierarchy { get; set; } = true;
        public bool GenerateMaterials { get; set; } = true;
        public bool CenterOnImport { get; set; }
    }
}
