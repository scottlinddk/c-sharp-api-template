using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Template.Domain.Orders;

namespace Template.Api.Endpoints.Orders;

/// <summary>
/// Query string for GET /orders. Unset values get defaults in the endpoint. Names are pinned to
/// camelCase so the OpenAPI document (and generated clients) match the JSON naming.
/// </summary>
internal sealed record ListOrdersRequest(
    [property: FromQuery(Name = "limit"), Description("Page size. Default 25, maximum 100.")] int? Limit,
    [property: FromQuery(Name = "offset"), Description("Number of items to skip. Default 0.")] int? Offset,
    [property: FromQuery(Name = "status"), Description("Only orders with this status: Pending, Confirmed or Cancelled (case-insensitive).")] string? Status,
    [property: FromQuery(Name = "minTotal"), Description("Only orders with a total amount of at least this value.")] decimal? MinTotal,
    [property: FromQuery(Name = "sort"), Description("Sort order: createdAt, -createdAt (default), totalAmount or -totalAmount. '-' means descending.")] string? Sort)
{
    /// <summary>
    /// Parses <see cref="Status"/> case-insensitively, matching how JSON bodies are read.
    /// Numeric values are rejected so clients cannot depend on enum ordinals.
    /// </summary>
    public bool TryGetStatus(out OrderStatus? status)
    {
        status = null;
        if (string.IsNullOrEmpty(Status))
        {
            return true;
        }

        if (!int.TryParse(Status, out _)
            && Enum.TryParse<OrderStatus>(Status, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            status = parsed;
            return true;
        }

        return false;
    }
}
