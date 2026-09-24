using System.Text.Json;
using DMP.BL.Constants;
using DMP.BL.Models;
using DMP.BL.Models.Enumerations;
using DMP.BL.Models.Search;
using DMP.Crosscutting.Models;
using Microsoft.Extensions.Logging;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Aggregation;
using NRedisStack.Search.Literals.Enums;
using StackExchange.Redis;

namespace DMP.BL.Services;

public class RedisService : IRedisService
{
    private const string SysNotificationQueue = "sys_notify_queue";

    private readonly ILogger<RedisService> _logger;
    private readonly IDatabase _redis;
    private readonly RedisSettings _redisSettings;
    private readonly JsonCommands _json;
    private readonly SearchCommands _ft;

    public RedisService(ILogger<RedisService> logger, IConnectionMultiplexer muxer, RedisSettings redisSettings)
    {
        _logger = logger;
        _redis = muxer.GetDatabase();
        _redisSettings = redisSettings;
        _json = _redis.JSON();
        _ft = _redis.FT();
    }

    public void EnsureProductCatIndex(string catIdx)
    {
        var indexName = string.Format(BlConstants.ProductCatIndex, catIdx);
        if (IndexExists(indexName))
        {
            return;
        }

        var schema = new Schema()
            .AddTagField(new FieldName("$.menuCategoryId", "menuCategoryId"))
            .AddNumericField(new FieldName("$.price", "price"), true)
            .AddNumericField(new FieldName("$.createdAt", "createdAt"), true)
            .AddNumericField(new FieldName("$.duration", "duration"))
            .AddNumericField(new FieldName("$.year", "year"))
            .AddTagField(new FieldName("$.features[*].key", "features_key"))
            .AddTagField(new FieldName("$.features[*].value", "features_value"));

        _ft.Create(indexName, new FTCreateParams().On(IndexDataType.JSON).Prefix($"product:{catIdx}:"), schema);
    }

    public void EnsureProductIndex()
    {
        if (IndexExists(BlConstants.ProductIndex))
        {
            return;
        }

        var schema = new Schema()
            .AddNumericField(new FieldName("$.productId", "productId"));

        _ft.Create(BlConstants.ProductIndex, new FTCreateParams().On(IndexDataType.JSON).Prefix("product:"), schema);
    }

    public void EnsureProductSearchIndex()
    {
        if (IndexExists(BlConstants.ProductSearchIndex))
        {
            return;
        }

        var schema = new Schema()
            .AddTextField(new FieldName("$.userFeatures.name", "name"), 10);

        _ft.Create(BlConstants.ProductSearchIndex, new FTCreateParams().On(IndexDataType.JSON).Prefix("product:"), schema);
    }

    private void EnsureMenuCategorySearchIndex()
    {
        if (IndexExists(BlConstants.CategorySearchIndexIndex))
        {
            return;
        }

        var schema = new Schema()
            .AddTextField(new FieldName("$.title", "title"), 10)
            .AddTextField(new FieldName("$.language", "language"), 10);

        _ft.Create(BlConstants.CategorySearchIndexIndex,
            new FTCreateParams().On(IndexDataType.JSON).Prefix("search:category:"), schema);
    }

    // FT.INFO throws for an unknown index; there is no dedicated "exists" command.
    private bool IndexExists(string indexName)
    {
        try
        {
            _ft.Info(indexName);
            return true;
        }
        catch (RedisServerException)
        {
            return false;
        }
    }

    public async Task<T?> GetCacheValue<T>(string key)
    {
        if (!_redisSettings.Enabled)
        {
            _logger.LogDebug("Redis is disabled.");
            return default;
        }

        var cachedValue = await _redis.StringGetAsync(key);

        return cachedValue.HasValue
            ? JsonSerializer.Deserialize<T>((string)cachedValue!, BlConstants.RedisJsonSerializerOptions)
            : default;
    }

    public async Task<bool> SetCacheValue<T>(string key, T value, TimeSpan? expiry = null)
    {
        if (!_redisSettings.Enabled)
        {
            _logger.LogDebug("Redis is disabled.");
            return false;
        }

        var json = JsonSerializer.Serialize(value, BlConstants.RedisJsonSerializerOptions);
        var result = await _redis.StringSetAsync(key, json, expiry is { } ttl ? ttl : Expiration.Default);

        if (!result)
        {
            _logger.LogError("Cannot set value to redis. Key: {Key}", key);
        }

        return result;
    }

    public async Task<bool> DeleteCacheValue(string key)
    {
        if (!_redisSettings.Enabled)
        {
            _logger.LogDebug("Redis is disabled.");
            return false;
        }

        var result = await _redis.KeyDeleteAsync(key);

        if (!result)
        {
            _logger.LogError("Cannot delete the key: {Key}.", key);
        }

        return result;
    }

    public async Task<int[]?> GetProductIds(string category, int page, short pageSize, ProductSorting sorting)
    {
        if (!_redisSettings.Enabled)
        {
            _logger.LogDebug("Redis is disabled.");
            return null;
        }

        var start = (page - 1) * pageSize;
        var end = start + pageSize - 1;

        var sortedProductIds = await _redis.SortedSetRangeByRankAsync(
            $"product_ids_{category}_by_{sorting.ToString().ToLower()}", start, end, Order.Ascending);

        return [.. sortedProductIds.Select(i => JsonSerializer.Deserialize<int>((string)i!))];
    }

    public async Task<IEnumerable<Product?>> SearchProducts(int[] ids)
    {
        var query = string.Join("|", ids.Select(id => $"@productId:[{id} {id}]"));

        var searchResult = await _ft.SearchAsync(BlConstants.ProductIndex, new Query(query).Limit(0, ids.Length));

        return searchResult.ToJson()
            .Select(productStr => JsonSerializer.Deserialize<Product>(productStr, BlConstants.RedisJsonSerializerOptions))
            .ToList();
    }

    public async Task<(Product? product, string? rootId)> SearchProduct(int productId)
    {
        var searchResult = await _ft.SearchAsync(BlConstants.ProductIndex,
            new Query($"@productId:[{productId} {productId}]").Limit(0, 1));
        var productStr = searchResult.ToJson().FirstOrDefault();

        if (productStr == null)
        {
            return (null, null);
        }

        // Document ids look like "product:{rootCategoryId}:{productId}".
        var rootId = searchResult.Documents[0].Id.Split(':')[1];
        var product = JsonSerializer.Deserialize<Product>(productStr, BlConstants.RedisJsonSerializerOptions);

        return (product, rootId);
    }

    public async Task<(IEnumerable<Product>? products, int totalCount)> SearchProducts(ProductSearchParams searchParams)
    {
        var queryParts = new List<string>();

        if (!string.IsNullOrEmpty(searchParams.CategoryId))
        {
            queryParts.Add($"@menuCategoryId:{{{searchParams.CategoryId}}}");
        }

        if (searchParams.Ranges != null)
        {
            foreach (var (key, (min, max)) in searchParams.Ranges)
            {
                queryParts.Add($"@{key}:[{min} {max}]");
            }
        }

        if (searchParams.Filters != null)
        {
            foreach (var (key, value) in searchParams.Filters)
            {
                // Multiple values of one filter are separated by ';' and combined with OR.
                var filterValue = value.Contains(';')
                    ? string.Join('|', value.Split(';').Where(s => !string.IsNullOrEmpty(s)).Select(s => key + "_" + s))
                    : key + "_" + value;

                queryParts.Add($"@features_key:{{{key}}} @features_value:{{{filterValue}}}");
            }
        }

        var queryString = queryParts.Count > 0 ? string.Join(" ", queryParts) : "*";

        var offset = searchParams.PageSize * (searchParams.Page - 1);
        // Fetch several pages ahead so the client can render the pager without knowing the exact total.
        var takeCount = searchParams.PageSize * searchParams.PagingSize;

        var sorting = searchParams.SortBy switch
        {
            ProductSorting.New => SortedField.Desc("@createdAt"),
            ProductSorting.Price => SortedField.Asc("@price"),
            ProductSorting.PriceDesc => SortedField.Desc("@price"),
            _ => throw new ArgumentOutOfRangeException(nameof(searchParams), searchParams.SortBy, "Unsupported sorting.")
        };

        var request = new AggregationRequest(queryString)
            .LoadAll()
            .SortBy(sorting)
            .Limit(offset, takeCount);

        var results = await _ft.AggregateAsync(
            string.Format(BlConstants.ProductCatIndex, searchParams.RootCategoryId), request);

        // GetResults is marked obsolete, but AggregationResult exposes no other way to get the row count.
#pragma warning disable CS0618
        var rows = results.GetResults();
#pragma warning restore CS0618
        var totalCount = offset + rows.Count;

        var products = rows
            .Take(searchParams.PageSize)
            .Select(row => JsonSerializer.Deserialize<Product>(row["$"].ToString(), BlConstants.RedisJsonSerializerOptions)!)
            .ToList();

        return (products, totalCount);
    }

    public async Task<FullTextSearchProductsResponse> FullTextSearch(string query, string lng)
    {
        const int maxResults = 10;

        var searchQueryCat = new Query($"@title: {query} @language: {lng}").SetWithScores().Limit(0, maxResults);
        var searchResultCat = await _ft.SearchAsync(BlConstants.CategorySearchIndexIndex, searchQueryCat);

        var categories = searchResultCat.ToJson()
            .Select(categoryStr => JsonSerializer.Deserialize<object>(categoryStr)!)
            .ToList();

        var result = new FullTextSearchProductsResponse { Categories = categories };

        var freeSlots = maxResults - categories.Count;
        if (freeSlots > 0)
        {
            result.Products = (await FullTextSearchProducts(query, 0, freeSlots)).products;
        }

        return result;
    }

    public async Task<(List<Product>? products, long totalCount)> FullTextSearchProducts(string query, int offset,
        int pageSize)
    {
        var searchQuery = new Query($"@name: {query}").SetWithScores().Limit(offset, pageSize);
        var searchResult = await _ft.SearchAsync(BlConstants.ProductSearchIndex, searchQuery);

        var products = searchResult.ToJson()
            .Select(productStr => JsonSerializer.Deserialize<Product>(productStr, BlConstants.RedisJsonSerializerOptions)!)
            .ToList();

        return (products, searchResult.TotalResults);
    }

    public bool RemoveProduct(int productId, string rootCategoryId)
    {
        if (!_redisSettings.Enabled)
        {
            _logger.LogDebug("Redis is disabled.");
            return false;
        }

        _json.Del($"product:{rootCategoryId}:{productId}", "$");

        return true;
    }

    public bool SetProducts(List<Product> products, string rootCategoryId)
    {
        if (!_redisSettings.Enabled)
        {
            _logger.LogDebug("Redis is disabled.");
            return false;
        }

        foreach (var product in products)
        {
            var json = JsonSerializer.Serialize(product, BlConstants.RedisJsonSerializerOptions);
            _json.Set($"product:{rootCategoryId}:{product.ProductId}", "$", json);
        }

        return true;
    }

    public bool SetSearchCategories(Dictionary<(int, string), SearchCategory> searchCategories)
    {
        if (!_redisSettings.Enabled)
        {
            _logger.LogDebug("Redis is disabled.");
            return false;
        }

        foreach (var ((categoryId, language), searchCategory) in searchCategories)
        {
            var json = JsonSerializer.Serialize(searchCategory, BlConstants.RedisJsonSerializerOptions);
            _json.Set(string.Format(CacheKeys.SearchCategoryJsonKey, categoryId, language), "$", json);
        }

        EnsureMenuCategorySearchIndex();

        return true;
    }

    public async Task AddSysNotification(string message)
    {
        await _redis.ListLeftPushAsync(SysNotificationQueue, message);
    }
}
