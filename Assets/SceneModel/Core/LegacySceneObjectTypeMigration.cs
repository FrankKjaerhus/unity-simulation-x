using System.Collections.Generic;
using System.Text;

namespace UnitySimulationX.SceneModel
{
    public static class LegacySceneObjectTypeMigration
    {
        public static SceneObjectTypeId FromV1Type(string legacy)
        {
            if (string.IsNullOrWhiteSpace(legacy))
                return new SceneObjectTypeId("com.unitysimulationx.legacy.unknown");

            return legacy switch
            {
                "Primitive" => SceneObjectTypeIds.Primitive,
                "ImportedAsset" => SceneObjectTypeIds.ImportedModel,
                "MachineFrame" => SceneObjectTypeIds.Group,
                "TrackSegment" => new SceneObjectTypeId("com.unitysimulationx.legacy.track-segment"),
                _ => new SceneObjectTypeId($"com.unitysimulationx.legacy.{ToKebab(legacy)}")
            };
        }

        static string ToKebab(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";

            var chars = new List<char>(value.Length + 8);
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (char.IsUpper(c) && i > 0)
                    chars.Add('-');

                chars.Add(char.ToLowerInvariant(c));
            }

            return new string(chars.ToArray());
        }
    }
}
