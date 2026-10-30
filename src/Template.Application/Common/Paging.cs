namespace Template.Application.Common;

/// <summary>Shared pagination limits, so every collection endpoint behaves the same.</summary>
public static class Paging
{
    public const int DefaultLimit = 25;

    /// <summary>Upper bound on page size, to protect the API and database from very large requests.</summary>
    public const int MaxLimit = 100;
}
