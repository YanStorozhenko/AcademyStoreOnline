using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Pages.Catalog
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        
        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public Category? SelectedCategory { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public decimal? MinPrice { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public bool InStock { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "newest";
        
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        
        public int PageSize { get; set; } = 12;
        public int TotalProducts { get; set; }
        public int TotalPages { get; set; }
        
        public async Task OnGetAsync()
        {
            // Загружаем категории
            Categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .ToListAsync();
            
            // Начинаем запрос
            IQueryable<Product> query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();
            
            // Применяем фильтры
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(p => 
                    p.Name.Contains(SearchTerm) || 
                    p.Description.Contains(SearchTerm));
            }
            
            if (CategoryId.HasValue && CategoryId > 0)
            {
                query = query.Where(p => p.CategoryId == CategoryId.Value);
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == CategoryId.Value);
            }
            
            if (MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= MinPrice.Value);
            }
            
            if (MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= MaxPrice.Value);
            }
            
            if (InStock)
            {
                query = query.Where(p => p.Stock > 0);
            }
            
            // Применяем сортировку
            query = SortBy switch
            {
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };
            
            // Получаем общее количество товаров
            TotalProducts = await query.CountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalProducts / PageSize);
            
            // Применяем пагинацию
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages));
            var skipAmount = (CurrentPage - 1) * PageSize;
            
            // Получаем товары для текущей страницы
            Products = await query
                .Skip(skipAmount)
                .Take(PageSize)
                .ToListAsync();
        }
        
        public string GetQueryStringWithoutParam(string paramName)
        {
            var query = HttpContext.Request.Query;
            var queryParams = new Dictionary<string, string?>();
            
            foreach (var key in query.Keys)
            {
                if (key != paramName && !string.IsNullOrEmpty(query[key]))
                {
                    queryParams[key] = query[key];
                }
            }
            
            return ToQueryString(queryParams);
        }
        
        public string GetPaginationQueryString(int page)
        {
            var queryParams = new Dictionary<string, string?>
            {
                ["page"] = page.ToString()
            };
            
            if (!string.IsNullOrEmpty(SearchTerm))
                queryParams["search"] = SearchTerm;
            
            if (CategoryId.HasValue)
                queryParams["categoryId"] = CategoryId.Value.ToString();
            
            if (MinPrice.HasValue)
                queryParams["minPrice"] = MinPrice.Value.ToString("0.##");
            
            if (MaxPrice.HasValue)
                queryParams["maxPrice"] = MaxPrice.Value.ToString("0.##");
            
            if (InStock)
                queryParams["inStock"] = "true";
            
            if (!string.IsNullOrEmpty(SortBy))
                queryParams["sortBy"] = SortBy;
            
            return ToQueryString(queryParams);
        }
        
        private string ToQueryString(Dictionary<string, string?> queryParams)
        {
            if (!queryParams.Any())
                return "";
            
            return string.Join("&", queryParams
                .Where(x => !string.IsNullOrEmpty(x.Value))
                .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));
        }
    }
}