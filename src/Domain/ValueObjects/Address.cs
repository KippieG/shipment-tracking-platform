using ShipmentTracking.Domain.Exceptions;

namespace ShipmentTracking.Domain.ValueObjects;

/// <summary>
/// Value object voor een gestructureerd adres.
/// </summary>
public sealed class Address : IEquatable<Address>
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    private Address(string street, string city, string postalCode, string country)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    public static Address Create(string street, string city, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))   throw new DomainException("Straat is verplicht.");
        if (string.IsNullOrWhiteSpace(city))     throw new DomainException("Stad is verplicht.");
        if (string.IsNullOrWhiteSpace(postalCode)) throw new DomainException("Postcode is verplicht.");
        if (string.IsNullOrWhiteSpace(country))  throw new DomainException("Land is verplicht.");

        return new Address(
            street.Trim(), city.Trim(),
            postalCode.Trim().ToUpperInvariant(),
            country.Trim().ToUpperInvariant());
    }

    public static Address FromString(string fullAddress)
    {
        // Fallback voor vrije-tekst adressen (legacy)
        if (string.IsNullOrWhiteSpace(fullAddress))
            throw new DomainException("Adres mag niet leeg zijn.");
        return new Address(fullAddress.Trim(), "", "", "BE");
    }

    public override string ToString() => $"{Street}, {PostalCode} {City}, {Country}";

    public bool Equals(Address? other) =>
        other is not null &&
        Street == other.Street &&
        City == other.City &&
        PostalCode == other.PostalCode &&
        Country == other.Country;

    public override bool Equals(object? obj) => obj is Address a && Equals(a);
    public override int GetHashCode() => HashCode.Combine(Street, City, PostalCode, Country);
}
