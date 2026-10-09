using FindMyLaptop.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;


namespace FindMyLaptop.Helpers
{
    public static class EmaxHelper
    {
        public static async Task<List<ProductDto>> GetEmaxProductsAsync()
        {
            const string baseApiUrl = "https://core.dxpapi.com/api/v1/core/?account_id=7589&auth_key=ct09kf9vtlnet8tr&domain_key=emaxme&request_id=7545662630568&view_id=ae&catalog_views=emaxme%3Aae&_br_uid_2=uid%3D4686251611558%3Av%3D16.0%3Ats%3D1791491853866%3Ahc%3D4&url=https%3A%2F%2Fuae.emaxme.com%2Fshop-laptoptabletandcomputeraccessories-laptops%3Fdiscount%3D1%2C100%26p%3D2&ref_url=https%3A%2F%2Fuae.emaxme.com%2Fshop-laptoptabletandcomputeraccessories-laptops%3Fdiscount%3D1%2C100%26p%3D2&request_type=search&x-concept=emax&x-env=prod&x-lang=en&rows=48&start=0&fl=pid%2CsiblingItems%2CbaseProductId%2CskuCode%2Clow_price%2Clow_sale_price%2Clow_price_range%2Clow_sale_price_range%2CsizeAll%2Ctitle%2Cbrand%2Cprice%2Csale_price%2CemployeePrice%2Cpromotions%2Cthumb_image%2Csku%2Csku_thumb_images%2Csku_swatch_images%2CcolorAll%2CconceptAll%2CmanufacturerNameAll%2Csku_color_group%2Curl%2Cprice_range%2Csale_price_range%2CgalleryImages%2Cdescription%2Csibling%2CchildDetails%2Cbadge%2Cskuid%2Csku_price%2Csku_sale_price%2CskuList%2CcategoryFacetValue%2CproductType%2CbreadcrumbCategories%2CitemType%2CmaterialCompositionAll&stats.field=price%2Csale_price&facet.range=percentageDiscount&search_type=category&q=laptoptabletandcomputeraccessories-laptops&cid=&f.categoryFacetValue.facet.prefix=03%23laptoptabletandcomputeraccessories-laptops&fq=percentageDiscount:%20[1%20TO%20100]";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            var finalProducts = new List<ProductDoc>();
            const string baseUrl = "https://uae.emaxme.com";
            const int pageSize = 48;

            int start = 0;
            int totalItems = 0;
            int currentPage = 1;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                do
                {
                    string pagedUrl = UpdateQueryParameter(baseApiUrl, "start", start.ToString());
                    pagedUrl = UpdateQueryParameter(pagedUrl, "rows", pageSize.ToString());

                    using var responseStream = await client.GetStreamAsync(pagedUrl);
                    var result = await JsonSerializer.DeserializeAsync<DxpApiResponse>(responseStream, jsonOptions);

                    var docs = result?.Response?.Docs;
                    if (docs == null || docs.Count == 0)
                    {
                        break;
                    }

                    if (totalItems == 0 && result?.Response != null)
                    {
                        totalItems = result.Response.NumFound;
                    }

                    foreach (var product in docs)
                    {
                        if (!string.IsNullOrEmpty(product.Url))
                        {
                            product.Url = product.Url.StartsWith("http")
                                ? product.Url
                                : $"{baseUrl}{product.Url}";
                        }
                        finalProducts.Add(product);
                    }

                    start += pageSize;
                    currentPage++;

                    await Task.Delay(250);

                } while (start < totalItems);
                var products = finalProducts.Select(h => new ProductDto
                {
                    Name = h.Name,
                    Price = h.Price,
                    Url = h.Url,
                    Brand = h.Brand,
                    ImageUrl = h.ImageUrl,
                    RetailerImage = "https://cdn.media.amplience.net/i/emax/emaxLogo1",
                    Retailer = "Emax"
                }).ToList();
                return products;
            }
            catch (Exception ex)
            {
                return new List<ProductDto>();
            }

        }
        static string UpdateQueryParameter(string url, string key, string value)
        {
            var uriBuilder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query[key] = value;
            uriBuilder.Query = query.ToString();

            return uriBuilder.ToString();
        }

    }
    public class DxpApiResponse
    {
        [JsonPropertyName("response")]
        public ResponseData? Response { get; set; }
    }
    public class ResponseData
    {
        [JsonPropertyName("numFound")]
        public int NumFound { get; set; }

        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("docs")]
        public List<ProductDoc> Docs { get; set; } = new();
    }
    public class ProductDoc
    {
        [JsonPropertyName("title")]
        public string? Name { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("thumb_image")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("brand")]
        public string? Brand { get; set; }

    }




}
