using CSharpFunctionalExtensions;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects.Errors;
using DirectoryService.Shared.Errors;

namespace DirectoryService.Domain.ValueObjects;

/// <summary>
/// Represents a physical address as a value object.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Address"/> value object encapsulates all the geographical information 
/// needed to identify a physical location. It is an immutable value object (a <c>record</c>), 
/// meaning that once created, its properties cannot be changed.
/// </para>
/// <para>
/// This value object is used by the <see cref="Location"/> entity to represent 
/// the physical address of offices, warehouses, and other organizational sites.
/// </para>
/// <para>
/// All address components are required and cannot be null or whitespace. This is 
/// enforced during construction to ensure data integrity.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var address = new Address(
///     country: "United States",
///     region: "California",
///     city: "San Francisco",
///     district: "Financial District",
///     street: "Market Street",
///     houseNumber: "100",
///     postalCode: "94105"
/// );
/// </code>
/// </example>
/// <seealso cref="Location"/>
public record Address
{
    /// <summary>
    /// Gets the country of the address.
    /// </summary>
    /// <value>A string representing the country name.</value>
    /// <remarks>This field is required and cannot be null or whitespace.</remarks>
    public string Country { get; }
    
    /// <summary>
    /// Gets the region, state, or province of the address.
    /// </summary>
    /// <value>A string representing the region, state, or province name.</value>
    /// <remarks>This field is required and cannot be null or whitespace.</remarks>
    public string? Region { get; }
    
    /// <summary>
    /// Gets the city of the address.
    /// </summary>
    /// <value>A string representing the city name.</value>
    /// <remarks>This field is required and cannot be null or whitespace.</remarks>
    public string City { get; }
    
    /// <summary>
    /// Gets the district or neighborhood of the address.
    /// </summary>
    /// <value>A string representing the district or neighborhood name.</value>
    /// <remarks>This field is required and cannot be null or whitespace.</remarks>
    public string? District { get; }
    
    /// <summary>
    /// Gets the street name of the address.
    /// </summary>
    /// <value>A string representing the street name.</value>
    /// <remarks>This field is required and cannot be null or whitespace.</remarks>
    public string Street { get; }
    
    /// <summary>
    /// Gets the house or building number of the address.
    /// </summary>
    /// <value>A string representing the house or building number.</value>
    /// <remarks>This field is required and cannot be null or whitespace.</remarks>
    public string HouseNumber { get; }
    
    /// <summary>
    /// Gets the postal or ZIP code of the address.
    /// </summary>
    /// <value>A string representing the postal or ZIP code.</value>
    /// <remarks>This field is required and cannot be null or whitespace.</remarks>
    public string? PostalCode { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Address"/> value object.
    /// </summary>
    /// <param name="country">The country of the address. Cannot be null or whitespace.</param>
    /// <param name="region">The region, state, or province. Cannot be null or whitespace.</param>
    /// <param name="city">The city of the address. Cannot be null or whitespace.</param>
    /// <param name="district">The district or neighborhood. Cannot be null or whitespace.</param>
    /// <param name="street">The street name. Cannot be null or whitespace.</param>
    /// <param name="houseNumber">The house or building number. Cannot be null or whitespace.</param>
    /// <param name="postalCode">The postal or ZIP code. Cannot be null or whitespace.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when any of the parameters is null or whitespace.
    /// </exception>
    private Address(
        string country, 
        string? region,
        string city, 
        string? district,
        string street, 
        string houseNumber,
        string? postalCode)
    {
        Country = country;
        Region = region;
        City = city;
        District = district;
        Street = street;
        HouseNumber = houseNumber;
        PostalCode = postalCode;
    }

    public static Result<Address, Error> Create(
        string country, string? region, string city, string? district,
        string street, string houseNumber, string? postalCode)
    {
        var errors = new List<ErrorMessage>();
        
        if (string.IsNullOrWhiteSpace(country))
            errors.Add(ValueObjectErrors.Address.CountryEmptyMessage);
        
        if (string.IsNullOrWhiteSpace(city))
            errors.Add(ValueObjectErrors.Address.CityEmptyMessage);
        
        if (string.IsNullOrWhiteSpace(street))
            errors.Add(ValueObjectErrors.Address.StreetEmptyMessage);
        
        if (string.IsNullOrWhiteSpace(houseNumber))
            errors.Add(ValueObjectErrors.Address.HouseNumberEmptyMessage);

        if (errors.Count > 0)
        {
            return ValueObjectErrors.Address.Invalid(errors);
        }
        
        return new Address(country, region, city, district, street, houseNumber, postalCode);
    }
}
