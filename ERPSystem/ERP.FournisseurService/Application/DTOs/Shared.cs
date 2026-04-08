namespace ERP.FournisseurService.Application.DTOs;


public class ErrorResponse
{
    public required string Code { get; set; }
    public required string Message { get; set; }
    public int StatusCode { get; set; }
}
public sealed class PagedResultDto<T>
{
    public List<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public PagedResultDto(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}