namespace OpenGameVerse.Core.Abstractions;

/// <summary>
/// Data access for game category management
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Get all categories with game counts
    /// </summary>
    IAsyncEnumerable<(string Name, int GameCount)> GetAllCategoriesAsync(CancellationToken ct);

    /// <summary>
    /// Add a new category
    /// </summary>
    Task<bool> AddCategoryAsync(string categoryName, CancellationToken ct);

    /// <summary>
    /// Delete a category by name
    /// </summary>
    Task<bool> DeleteCategoryAsync(string categoryName, CancellationToken ct);

    /// <summary>
    /// Check if a category exists
    /// </summary>
    Task<bool> CategoryExistsAsync(string categoryName, CancellationToken ct);
}
