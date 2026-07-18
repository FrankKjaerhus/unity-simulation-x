using System;
using System.Text.RegularExpressions;

namespace UnitySimulationX.SceneModel
{
    [Serializable]
    public readonly struct SceneObjectTypeId : IEquatable<SceneObjectTypeId>
    {
        static readonly Regex Pattern = new(
            "^[a-z0-9]+(?:[.-][a-z0-9]+)*$",
            RegexOptions.CultureInvariant);

        public SceneObjectTypeId(string value)
        {
            if (!IsValid(value))
                throw new ArgumentException($"Invalid scene object type id: '{value}'.", nameof(value));
            Value = value;
        }

        public string Value { get; }

        public static bool IsValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Split('.').Length >= 3 &&
                   Pattern.IsMatch(value);
        }

        public bool Equals(SceneObjectTypeId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is SceneObjectTypeId other && Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }
}
