using FindMyLaptop.Helpers;
using FindMyLaptop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FindMyLaptop.Pages
{
    public class IndexModel : PageModel
    {
        public List<ProductDto> Products { get; set; } = new List<ProductDto>();

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        public async Task OnGetAsync()
        {
            decimal targetPrice = MaxPrice ?? 4000m;
            Products = await FetchAndSortProductsAsync(targetPrice);

        }

        public async Task<IActionResult> OnGetSearchAsync([FromQuery] string? maxPrice)
        {
            decimal targetPrice = 4000m;

            if (!string.IsNullOrWhiteSpace(maxPrice) &&
                decimal.TryParse(maxPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedPrice))
            {
                targetPrice = parsedPrice;
            }

            var products = await FetchAndSortProductsAsync(targetPrice);
            return new JsonResult(products);
        }

        private async Task<List<ProductDto>> FetchAndSortProductsAsync(decimal maxPrice)
        {
            var sharafdgTask = SharafDGHelper.GetSharafdgProductsAsync();
            var jumboTask = JumboHelper.GetJumboProductsAsync();
            var emaxTask = EmaxHelper.GetEmaxProductsAsync();

            await Task.WhenAll(sharafdgTask, jumboTask, emaxTask);

            // 1. Filter each store individually & sort by price
            var sharafdg = FilterAndSort(await sharafdgTask, maxPrice);
            var jumbo = FilterAndSort(await jumboTask, maxPrice);
            var emax = FilterAndSort(await emaxTask, maxPrice);

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "alershad.json");
            List<ProductDto> alershadRaw = new List<ProductDto>();
            var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                alershadRaw = JsonSerializer.Deserialize<List<ProductDto>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<ProductDto>();

            // 2. Filter and sort by price
            var alershad = FilterAndSort(alershadRaw, maxPrice);

            var filePath2 = Path.Combine(Directory.GetCurrentDirectory(), "data", "carrefouruae.json");
            var jsonContent2 = await System.IO.File.ReadAllTextAsync(filePath2);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                
            };

            List<ProductDto> carrefouruaeRaw = JsonSerializer.Deserialize<List<ProductDto>>(jsonContent2, options) ?? new List<ProductDto>();

            // 1. Filter and sort by unique names
            var distinctRaw = carrefouruaeRaw
                 .DistinctBy(p => p.Name?.Trim())
                 .Where(p => p.Price > 1000)
                 .ToList();

            // 2. Filter and sort on the unique list
            var carrefouruae = FilterAndSort(distinctRaw, maxPrice);

            // 3. Map to final DTOs
            var carrefouruaeProducts = carrefouruae.Select(p => new ProductDto
            {
                Name = p.Name,
                Brand = p.Brand,
                Price = p.Price,
                Url = p.Url,
                ImageUrl = p.ImageUrl,
                RetailerImage = "/images/carrefour.png",
                Retailer = "Carrefour"
            }).ToList();

            // 4. Interleave products from all stores
            var interleavedProducts = new List<ProductDto>();

            // Calculate max count across all 5 stores
            int maxCount = new[]
            {
                sharafdg.Count,
                jumbo.Count,
                emax.Count,
                alershad.Count,
                carrefouruaeProducts.Count
            }.Max();

            // Take 1 item from each store per loop iteration
            for (int i = 0; i < maxCount; i++)
            {
                if (i < sharafdg.Count) interleavedProducts.Add(sharafdg[i]);
                if (i < jumbo.Count) interleavedProducts.Add(jumbo[i]);
                if (i < emax.Count) interleavedProducts.Add(emax[i]);
                if (i < alershad.Count) interleavedProducts.Add(alershad[i]);
                if (i < carrefouruaeProducts.Count) interleavedProducts.Add(carrefouruaeProducts[i]);
            }

            return interleavedProducts.Where(p => p.Price > 1000).ToList();
        }

        private List<ProductDto> FilterAndSort(List<ProductDto>? rawList, decimal maxPrice)
        {
            if (rawList == null) return new List<ProductDto>();

            return rawList
                .Where(p => p != null && p.Price > 0 && p.Price <= maxPrice)
                .OrderBy(p => p.Price)
                .ToList();
        }
    }
}