using SecureCleanApiWaf.Core.Domain.Entities;
using SecureCleanApiWaf.Core.Application.Common.DTOs;
using Microsoft.Extensions.Logging;

namespace SecureCleanApiWaf.Core.Application.Common.Mapping
{
    /// <summary>
    /// Custom mapper for converting **dynamic/unknown** external API responses to ApiDataItem domain entities.
    /// </summary>
    /// <remarks>
    /// **WHEN TO USE THIS MAPPER:**
    /// - APIs with **unknown/dynamic** response structures at compile time
    /// - Third-party APIs that may change field names
    /// - APIs with inconsistent property naming
    /// - Complex nested structures requiring custom extraction logic
    /// 
    /// **WHEN TO USE AUTOMAPPER INSTEAD:**
    /// - Known/predictable API response structures (use AutoMapper with ApiItemDto)
    /// - Internal application mappings
    /// - Entity to DTO conversions for controllers
    /// - Standard property-to-property mappings
    /// 
    /// **HYBRID APPROACH:**
    /// This mapper complements AutoMapper by handling scenarios where AutoMapper
    /// cannot work due to dynamic types or unpredictable API structures.
    /// 
    /// Benefits:
    /// - Handles dynamic JSON deserialization (JsonElement, ExpandoObject)
    /// - Tries multiple property name variations ("id", "externalId", "itemId")
    /// - Works with reflection for strongly-typed objects
    /// - Flexible metadata extraction
    /// - No compile-time dependency on API structure
    /// 
    /// Usage Example:
    /// ```csharp
    /// // For unknown API structure
    /// var apiResponse = await _apiService.GetAllDataAsync<dynamic>(url);
    /// var domainEntities = _customMapper.MapToApiDataItems(apiResponse, url);
    /// 
    /// // For known API structure (use AutoMapper instead)
    /// var apiDto = await _apiService.GetAllDataAsync<ApiItemDto>(url);
    /// var domainEntity = _autoMapper.Map<ApiDataItem>(apiDto);
    /// ```
    /// </remarks>
    public class ApiDataMapper
    {
        private readonly ILogger<ApiDataMapper> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="ApiDataMapper"/> using the provided logger.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public ApiDataMapper(ILogger<ApiDataMapper> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Maps a single API response item to ApiDataItem domain entity.
        /// </summary>
        /// <param name="apiItem">The API response item.</param>
        /// <param name="sourceUrl">The source API URL.</param>
        /// <returns>ApiDataItem domain entity.</returns>
        /// <remarks>
        /// Handles common API response structures. Customize based on your actual API format.
        /// <summary>
        /// Maps a single dynamic API response item to an ApiDataItem domain entity.
        /// </summary>
        /// <param name="apiItem">The dynamic API response object containing fields such as id, name, description, and other metadata.</param>
        /// <param name="sourceUrl">The originating API URL to record as the data source.</param>
        /// <returns>An ApiDataItem populated from the API item, or `null` if required fields are missing or an error occurs during mapping.</returns>
        public ApiDataItem? MapToApiDataItem(dynamic apiItem, string sourceUrl)
        {
            try
            {
                // Extract required fields from API response
                // Adjust property names based on your actual API response structure
                string externalId = GetPropertyValue(apiItem, "id", "externalId", "itemId")?.ToString() ?? string.Empty;
                string name = GetPropertyValue(apiItem, "name", "title", "displayName")?.ToString() ?? string.Empty;
                string description = GetPropertyValue(apiItem, "description", "details", "summary")?.ToString() ?? string.Empty;

                // Validate required fields
                if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(name))
                {
                    _logger.LogWarning("API item missing required fields (id or name). Skipping.");
                    return null;
                }

                // Create domain entity
                var apiDataItem = ApiDataItem.CreateFromExternalSource(
                    externalId: externalId,
                    name: name,
                    description: description,
                    sourceUrl: sourceUrl);

                // Extract and add metadata
                AddMetadataFromApiItem(apiDataItem, apiItem);

                _logger.LogDebug("Mapped API item {ExternalId} to domain entity", externalId);
                
                return apiDataItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping API item to domain entity");
                return null;
            }
        }

        /// <summary>
        /// Maps a collection of API response items to ApiDataItem domain entities.
        /// </summary>
        /// <typeparam name="T">The type of API response collection.</typeparam>
        /// <param name="apiResponse">The API response containing items.</param>
        /// <param name="sourceUrl">The source API URL.</param>
        /// <summary>
        /// Map a dynamic API response into a list of ApiDataItem domain entities.
        /// </summary>
        /// <param name="apiResponse">The API response to map; may be an IEnumerable<dynamic>, an object containing a collection under `data`, `items`, or `results`, or a single item object.</param>
        /// <param name="sourceUrl">The source URL used to populate the ApiDataItem's external source information.</param>
        /// <returns>A list of ApiDataItem created from the response; the list will be empty if no mappable items are found.</returns>
        public List<ApiDataItem> MapToApiDataItems<T>(T apiResponse, string sourceUrl)
        {
            var results = new List<ApiDataItem>();

            try
            {
                // Handle different API response structures
                IEnumerable<dynamic>? items = null;

                // Case 1: Direct array/list response
                if (apiResponse is IEnumerable<dynamic> enumerable)
                {
                    items = enumerable;
                }
                // Case 2: Response with 'data' or 'items' property
                else if (apiResponse != null)
                {
                    dynamic response = apiResponse;
                    
                    // Try common property names for collections
                    items = GetPropertyValue(response, "data", "items", "results") as IEnumerable<dynamic>;
                    
                    // If response is a single object with properties, treat it as single item
                    if (items == null)
                    {
                        items = new[] { response };
                    }
                }

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var domainEntity = MapToApiDataItem(item, sourceUrl);
                        if (domainEntity != null)
                        {
                            results.Add(domainEntity);
                        }
                    }
                }

                _logger.LogInformation("Mapped {Count} API items to domain entities from {SourceUrl}", 
                    results.Count, sourceUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping API response to domain entities");
            }

            return results;
        }

        /// <summary>
        /// Updates an existing ApiDataItem with fresh data from API response.
        /// </summary>
        /// <param name="existingItem">The existing domain entity to update.</param>
        /// <param name="apiItem">The fresh API response item.</param>
        /// <summary>
        /// Update an existing ApiDataItem with values extracted from a dynamic API response item.
        /// </summary>
        /// <param name="existingItem">The domain entity to update; it will be modified in place.</param>
        /// <param name="apiItem">The dynamic API response object to extract values from (e.g., fields like "name"/"title", "description", and metadata).</param>
        /// <returns>`true` if the existingItem was updated and its metadata refreshed; `false` if required data was missing or an error occurred.</returns>
        public bool UpdateFromApiResponse(ApiDataItem existingItem, dynamic apiItem)
        {
            try
            {
                string name = GetPropertyValue(apiItem, "name", "title", "displayName")?.ToString() ?? string.Empty;
                string description = GetPropertyValue(apiItem, "description", "details", "summary")?.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name))
                {
                    _logger.LogWarning("API item missing name. Cannot update entity {ExternalId}", existingItem.ExternalId);
                    return false;
                }

                // Update entity using domain method
                existingItem.UpdateFromExternalSource(name, description);

                // Clear and re-add metadata
                existingItem.ClearMetadata();
                AddMetadataFromApiItem(existingItem, apiItem);

                _logger.LogDebug("Updated domain entity {ExternalId} from API response", existingItem.ExternalId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating domain entity {ExternalId} from API response", existingItem.ExternalId);
                return false;
            }
        }

        /// <summary>
        /// Extracts metadata from API item and adds to domain entity.
        /// </summary>
        /// <param name="apiDataItem">The domain entity to add metadata to.</param>
        /// <summary>
        /// Extracts common metadata fields from a dynamic API item and adds them to the provided ApiDataItem.
        /// </summary>
        /// <param name="apiDataItem">The domain entity to receive metadata entries.</param>
        /// <param name="apiItem">The dynamic API response item to extract metadata from.</param>
        /// <remarks>
        /// Attempts to extract and add the following metadata keys when present: "category", "price", "rating", "tags", "status", and "source_timestamp".
        /// Extraction failures are logged and do not prevent the method from completing.
        /// </remarks>
        private void AddMetadataFromApiItem(ApiDataItem apiDataItem, dynamic apiItem)
        {
            try
            {
                // Extract common metadata fields
                // Customize based on your API response structure
                
                var category = GetPropertyValue(apiItem, "category", "type", "categoryName");
                if (category != null)
                {
                    apiDataItem.AddMetadata("category", category.ToString()!);
                }

                var price = GetPropertyValue(apiItem, "price", "cost", "amount");
                if (price != null)
                {
                    apiDataItem.AddMetadata("price", price.ToString()!);
                }

                var rating = GetPropertyValue(apiItem, "rating", "score", "stars");
                if (rating != null)
                {
                    apiDataItem.AddMetadata("rating", rating.ToString()!);
                }

                var tags = GetPropertyValue(apiItem, "tags", "keywords", "labels");
                if (tags != null)
                {
                    apiDataItem.AddMetadata("tags", tags.ToString()!);
                }

                var status = GetPropertyValue(apiItem, "status", "state", "availability");
                if (status != null)
                {
                    apiDataItem.AddMetadata("status", status.ToString()!);
                }

                // Add source timestamp if available
                var timestamp = GetPropertyValue(apiItem, "timestamp", "updatedAt", "lastModified");
                if (timestamp != null)
                {
                    apiDataItem.AddMetadata("source_timestamp", timestamp.ToString()!);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting metadata from API item");
            }
        }

        /// <summary>
        /// Gets a property value from a dynamic object, trying multiple possible property names.
        /// </summary>
        /// <param name="obj">The dynamic object.</param>
        /// <param name="propertyNames">Possible property names to try.</param>
        /// <summary>
        /// Retrieve the first matching property value from a dynamic object using multiple candidate names.
        /// </summary>
        /// <param name="obj">The dynamic object or dictionary to read properties from.</param>
        /// <param name="propertyNames">Candidate property names to try in order (matching is case-insensitive).</param>
        /// <returns>The value of the first property found, or null if none is present or an error occurs.</returns>
        private static object? GetPropertyValue(dynamic obj, params string[] propertyNames)
        {
            if (obj == null) return null;

            try
            {
                // Try to access as dictionary first (JSON deserialization often creates dictionaries)
                if (obj is IDictionary<string, object> dict)
                {
                    foreach (var propName in propertyNames)
                    {
                        if (dict.TryGetValue(propName, out var value))
                        {
                            return value;
                        }
                    }
                }

                // Try reflection for strongly-typed objects
                var type = obj.GetType();
                foreach (var propName in propertyNames)
                {
                    var prop = type.GetProperty(propName, 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.IgnoreCase);
                    
                    if (prop != null)
                    {
                        return prop.GetValue(obj);
                    }
                }
            }
            catch
            {
                // Ignore errors in dynamic property access
            }

            return null;
        }
    }
}