using System;
using System.Collections.Generic;

namespace ValidationAPI.Common.Models;

public class PaginatedList<T>
{
	public int PageNumber { get; }
	public int TotalPages { get; }
	public int TotalCount { get; }
	
	public bool HasPrev => PageNumber > 1;
	public bool HasNext => PageNumber < TotalPages;
	
	public IReadOnlyCollection<T> Items { get; }
	
	public PaginatedList(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
	{
		PageNumber = pageNumber;
		TotalPages = pageSize < 1 ? count : (int)Math.Ceiling(count / (double)pageSize);
		TotalCount = count;
		Items = items;
	}
}