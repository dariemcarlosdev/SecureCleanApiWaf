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
        /// </remarks>
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
        /// <returns>List of ApiDataItem domain entities.</returns>
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
        /// <returns>True if updated successfully, false otherwise.</returns>
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
        /// <param name="apiItem">The API response item.</param>
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
        /// <returns>The property value if found, null otherwise.</returns>
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
