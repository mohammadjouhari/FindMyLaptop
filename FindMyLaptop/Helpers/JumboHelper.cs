using FindMyLaptop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace FindMyLaptop.Helpers
{
    public static class JumboHelper
    {
        public static async Task<List<ProductDto>> GetJumboProductsAsync()
        {
            using var client = new HttpClient();

            string appId = "MK4U402W4M";
            string apiKey = "b6eb0d98aeb4e792e05f0fe182dfbc0e";
            string indexName = "staging_en_products";
            string algoliaUrl = $"https://{appId.ToLower()}-dsn.algolia.net/1/indexes/{indexName}/query";

            using var request = new HttpRequestMessage(HttpMethod.Post, algoliaUrl);
            request.Headers.Add("X-Algolia-API-Key", apiKey);
            request.Headers.Add("X-Algolia-Application-Id", appId);

            // Payload sending clean parameters without broken facet string escaping
            var payload = new
            {
                @params = "query=laptops&hitsPerPage=1000&"
            };

            request.Content = JsonContent.Create(payload);

            try
            {
                using var response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();

                // Enable string-to-number parsing and case-insensitivity
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var result = await response.Content.ReadFromJsonAsync<AlgoliaResponse<ProductHit>>(options);

                if (result?.Hits != null)
                {
                    result.Hits = result.Hits
                        .Where(hit =>
                            // 1. Check if 'CategoriesWithoutPath' contains the string "Laptops" (exact match, case-insensitive)
                            hit.CategoriesWithoutPath?.Any(cat => string.Equals(cat, "Laptops", StringComparison.OrdinalIgnoreCase)) == true

                            // 2. Check if any category in 'Level1' contains "Laptops" (substring match, case-insensitive)
                            || hit.Categories?.Level1?.Any(c => c.Contains("Laptops", StringComparison.OrdinalIgnoreCase)) == true
                        )
                        .ToList();
                }
                var products = result.Hits
                    .Select(h => new ProductDto
                    {
                        Name = h.Name,
                        Price = h.ExtractPrice(),
                        Url = "https://www.jumbo.ae/"+h.Url,
                        Brand = !string.IsNullOrEmpty(h.Brand) ? h.Brand : h.Manufacturer,
                        ImageUrl = !string.IsNullOrEmpty(h.ImageUrl) ? h.ImageUrl : h.Image,
                        RetailerImage = "https://www.jumbo.ae/assets/apple-touch-icon.png",
                        Retailer = "Jumbo"
                    })
                    .Where(p => p.Price > 0)
                    .ToList();

                return products;
            }
            catch (Exception ex)
            {
                // Fallback on HTTP or Deserialization error
                return new List<ProductDto>();
            }
        }
    }
    public interface ICategoryItem
    {
        List<string>? CategoriesWithoutPath { get; set; }
        CategoryMap? Categories { get; set; }
    }

    public class AlgoliaResponse<T>
    {
        [JsonPropertyName("hits")]
        public List<T> Hits { get; set; } = new List<T>();
    }
    [JsonConverter(typeof(CategoryMapJsonConverter))]
    public class CategoryMap
    {
        [JsonPropertyName("level0")]
        public List<string>? Level0 { get; set; }

        [JsonPropertyName("level1")]
        public List<string>? Level1 { get; set; }
    }

    public class ProductHit: ICategoryItem
    {
        [JsonPropertyName("categories")]
        public CategoryMap? Categories { get; set; }

        [JsonPropertyName("categories_without_path")]
        public List<string>? CategoriesWithoutPath { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url_path")]
        public string Url { get; set; }

        [JsonPropertyName("brand")]
        public string Brand { get; set; }

        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }

        // Dynamic JsonElement to inspect raw Algolia price dictionary
        [JsonPropertyName("price")]
        public JsonElement RawPrice { get; set; }

        [JsonPropertyName("special_price")]
        public JsonElement RawSpecialPrice { get; set; }

        /// <summary>
        /// Safely extracts decimal price from Algolia's dynamic JSON structure
        /// </summary>
        public decimal ExtractPrice()
        {
            // 1. Check direct 'special_price' or 'price' if sent as raw numbers or strings
            decimal directPrice = ParseJsonValue(RawSpecialPrice);
            if (directPrice > 0) return directPrice;

            if (RawPrice.ValueKind == JsonValueKind.Number || RawPrice.ValueKind == JsonValueKind.String)
            {
                decimal parsed = ParseJsonValue(RawPrice);
                if (parsed > 0) return parsed;
            }

            // 2. Traverses nested object structure: price -> AED/aed -> default/default_formated/cost
            if (RawPrice.ValueKind == JsonValueKind.Object)
            {
                foreach (var currencyProp in RawPrice.EnumerateObject())
                {
                    if (currencyProp.Name.Equals("AED", StringComparison.OrdinalIgnoreCase))
                    {
                        var currencyValue = currencyProp.Value;

                        if (currencyValue.ValueKind == JsonValueKind.Number || currencyValue.ValueKind == JsonValueKind.String)
                        {
                            return ParseJsonValue(currencyValue);
                        }

                        if (currencyValue.ValueKind == JsonValueKind.Object)
                        {
                            if (currencyValue.TryGetProperty("default", out var defaultProp))
                            {
                                decimal val = ParseJsonValue(defaultProp);
                                if (val > 0) return val;
                            }

                            if (currencyValue.TryGetProperty("cost", out var costProp))
                            {
                                decimal val = ParseJsonValue(costProp);
                                if (val > 0) return val;
                            }
                        }
                    }
                }
            }

            return 0m;
        }

        private decimal ParseJsonValue(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number)
            {
                return element.GetDecimal();
            }
            if (element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString(), out decimal parsed))
            {
                return parsed;
            }
            return 0m;
        }
    }
}