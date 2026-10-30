namespace Template.Application.Common;

/// <summary>A page of results from a collection, using limit/offset pagination.</summary>
/// <param name="Items">The items on this page.</param>
/// <param name="Offset">Index of the first item on this page.</param>
/// <param name="Limit">Maximum number of items requested for this page.</param>
/// <param name="TotalCount">Total number of items that match the filter, across all pages.</param>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Offset, int Limit, int TotalCount);
