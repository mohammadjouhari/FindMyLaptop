using FindMyLaptop.Models;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FindMyLaptop.Helpers
{
    public static class SharafDGHelper
    {

        public static async Task<List<ProductDto>> GetSharafdgProductsAsync()
        {
            using var client = new HttpClient();
            string appId = "9KHJLG93J1";
            string apiKey = "e81d5b30a712bb28f0f1d2a52fc92dd0";
            string indexName = "products_index";
            string algoliaUrl = $"https://{appId.ToLower()}-dsn.algolia.net/1/indexes/{indexName}/query";
            using var request = new HttpRequestMessage(HttpMethod.Post, algoliaUrl);
            request.Headers.Add("X-Algolia-API-Key", apiKey);
            request.Headers.Add("X-Algolia-Application-Id", appId);
            var payload = new
            {
                @params = $"query=laptop&hitsPerPage=10000&facetFilters=[[\"taxonomies.product_tag:aiproducts\"]]"
            };
            request.Content = JsonContent.Create(payload);
            try
            {
                using var response = await client.SendAsync(request);

                string jsonString = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<AlgoliaResponse<Product>>();
                var products = result.Hits.Select(h => new ProductDto
                {
                    Name = h.Name,
                    Price = h.Price,
                    Url = h.Url,
                    Brand = h.Brand,
                    ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTv9xvEsNe-LtVTOl6Yvcf_yNshKUEDaZ9kYgAoK5jtVw&s",
                    Retailer = "SharafDG",
                    RetailerImage = "https://s.sdgcdn.com/7/2018/05/MobileApp_Icon_200x200.png"

                }).ToList();
                return products;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching data: {ex.Message}");
                return new List<ProductDto>();
            }
        }

    }


// -----------------------------------------------------------------------------
// Models
// -----------------------------------------------------------------------------


public class Product
{

    [JsonPropertyName("post_title")]
    public string? Name { get; set; }


    [JsonPropertyName("store_price")]
    public decimal Price { get; set; }

    [JsonPropertyName("permalink")]
    public string? Url { get; set; }


    [JsonPropertyName("images")]
    public string? ImageUrl { get; set; }


    [JsonPropertyName("taxonomies")]
    public ProductTaxonomies? Taxonomies { get; set; }


    /// <summary>
    /// Helper property to safely retrieve the brand name from root or taxonomy fields.
    /// </summary>
    [JsonIgnore]
    public string? Brand =>
        Taxonomies?.PaBrand?.FirstOrDefault() ??
        Taxonomies?.ProductBrand?.FirstOrDefault() ??
        Taxonomies?.BrandTaxonomy?.FirstOrDefault();
}

//public class ProductDto
//{
//    public string Name { get; set; }
//    public string Brand { get; set; }
//    public decimal Price { get; set; }
//    public string Url { get; set; }
//    public string ImageUrl { get; set; }
//}

public class ProductTaxonomies
{
    [JsonPropertyName("product_tag")]
    public List<string>? ProductTags { get; set; }

    [JsonPropertyName("product_cat")]
    public List<string>? ProductCategories { get; set; }

    // WooCommerce product attribute taxonomy for brand (pa_brand)
    [JsonPropertyName("pa_brand")]
    public List<string>? PaBrand { get; set; }

    // Standard custom taxonomy for product_brand
    [JsonPropertyName("product_brand")]
    public List<string>? ProductBrand { get; set; }

    // Generic brand taxonomy fallback
    [JsonPropertyName("brand")]
    public List<string>? BrandTaxonomy { get; set; }
}


}
