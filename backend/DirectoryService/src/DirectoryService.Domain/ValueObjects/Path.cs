using DirectoryService.Domain.Models;

namespace DirectoryService.Domain.ValueObjects;

/// <summary>
/// Represents a hierarchical path as a value object.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Path"/> value object represents a hierarchical path composed of 
/// <see cref="Slug"/> segments separated by forward slashes (e.g., "company/division/team").
/// It is an immutable value object, meaning that once created, its value cannot be changed.
/// </para>
/// <para>
/// Paths are constructed by starting with a root <see cref="Slug"/> and appending 
/// additional <see cref="Slug"/> segments using the <see cref="Append"/> method.
/// Each append operation returns a new <see cref="Path"/> instance with the combined path.
/// </para>
/// <para>
/// This value object is used by the <see cref="Department"/> entity to represent 
/// the full hierarchical path from the root department to the current department.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating a root path from a slug
/// var rootSlug = new Slug("acme-corp");
/// var rootPath = new Path(rootSlug);
/// // rootPath.Value == "acme-corp"
/// 
/// // Appending child slugs to build a hierarchical path
/// var engineeringSlug = new Slug("engineering");
/// var engineeringPath = rootPath.Append(engineeringSlug);
/// // engineeringPath.Value == "acme-corp/engineering"
/// 
/// var backendSlug = new Slug("backend");
/// var backendPath = engineeringPath.Append(backendSlug);
/// // backendPath.Value == "acme-corp/engineering/backend"
/// </code>
/// </example>
/// <seealso cref="Slug"/>
/// <seealso cref="Department"/>
public record Path
{
    /// <summary>
    /// Gets the string value of the path.
    /// </summary>
    /// <value>
    /// A string representing the full hierarchical path with segments separated by forward slashes
    /// (e.g., "company/division/team").
    /// </value>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Path"/> value object from a string value.
    /// </summary>
    /// <param name="value">The string value of the path.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    private Path(string value)
    {
        Value = value;
    }

    public static Path Create(Slug slug) => new(slug.Value);

    /// <summary>
    /// Appends a slug segment to the current path, creating a new path.
    /// </summary>
    /// <param name="slug">The <see cref="Slug"/> to append to this path.</param>
    /// <returns>A new <see cref="Path"/> instance with the appended slug segment.</returns>
    /// <example>
    /// <code>
    /// var path = new Path(new Slug("company"));
    /// var childPath = path.Append(new Slug("division"));
    /// // childPath.Value == "company/division"
    /// </code>
    /// </example>
    public Path Append(Slug slug)
    {
        return new Path($"{Value}/{slug.Value}");
    }
    
    public override string ToString() => Value;
}