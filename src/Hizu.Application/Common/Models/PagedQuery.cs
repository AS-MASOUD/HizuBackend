namespace Hizu.Application.Common.Models;

public class PagedQuery
{
    private const int MaxPageSize = 50;
    private int _page = 1;
    private int _pageSize = 12;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value < 1 ? 1 : value;
    }
}
