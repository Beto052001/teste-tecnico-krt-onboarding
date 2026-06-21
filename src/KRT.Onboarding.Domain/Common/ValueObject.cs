namespace KRT.Onboarding.Domain.Common;

/// <summary>
/// Base para objetos de valor: imutáveis e comparados por valor (não por referência).
/// Subclasses declaram quais componentes participam da igualdade.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null || other.GetType() != GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(default(HashCode), (hash, component) =>
            {
                hash.Add(component);
                return hash;
            })
            .ToHashCode();

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
