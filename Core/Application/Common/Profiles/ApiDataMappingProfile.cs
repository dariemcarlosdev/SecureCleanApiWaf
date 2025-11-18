using AutoMapper;
using SecureCleanApiWaf.Core.Domain.Entities;
using SecureCleanApiWaf.Core.Application.Common.Mapping;
using SecureCleanApiWaf.Core.Application.Common.DTOs;

namespace SecureCleanApiWaf.Core.Application.Common.Profiles
{
    /// <summary>
    /// AutoMapper profile for mapping between API DTOs and domain entities.
    /// </summary>
    /// <remarks>
    /// This profile handles mappings for:
    /// - Known API response DTOs to ApiDataItem domain entities
    /// - Domain entities to response DTOs for controllers
    /// - Internal layer-to-layer mappings
    /// 
    /// Benefits of AutoMapper:
    /// - Convention-based mapping (properties with same names map automatically)
    /// - Explicit configuration for complex mappings
    /// - Compile-time validation
    /// - Easy to test
    /// - Performance optimizations
    /// 
    /// When to Use:
    /// - Mapping **known/predictable** API responses
    /// - Internal application mappings
    /// - Entity to DTO conversions
    /// 
    /// When NOT to Use:
    /// - Dynamic API responses with unknown structure (use custom ApiDataMapper)
    /// - APIs with varying property names (use custom mapper with fallbacks)
    /// - Complex transformation logic (use custom methods)
    /// 
    /// Usage Example:
    /// ```csharp
    /// // In handler or service
    /// var domainEntity = _mapper.Map<ApiDataItem>(apiDto);
    /// var responseDto = _mapper.Map<ApiDataItemDto>(domainEntity);
    /// ```
    /// </remarks>
    public class ApiDataMappingProfile : Profile
    {
        public ApiDataMappingProfile()
        {
            // ===== API DTO to Domain Entity Mappings =====
            
            /// <summary>
            /// Maps ApiItemDto (known structure) to ApiDataItem domain entity.
            /// </summary>
            /// <remarks>
            /// This mapping assumes a predictable API response structure.
            /// Properties:
            /// - Id ? ExternalId
            /// - Name ? Name
            /// - Description ? Description
            /// - Category, Price, Rating, etc. ? Metadata
            /// 
            /// Use ForMember for custom mappings and ignore read-only properties.
            /// </remarks>
            CreateMap<ApiItemDto, ApiDataItem>()
                // Map Id from DTO to ExternalId in domain entity
                .ForMember(dest => dest.ExternalId, 
                    opt => opt.MapFrom(src => src.Id))
                
                // Name maps automatically (same property name)
                .ForMember(dest => dest.Name, 
                    opt => opt.MapFrom(src => src.Name))
                
                // Description maps automatically
                .ForMember(dest => dest.Description, 
                    opt => opt.MapFrom(src => src.Description ?? string.Empty))
                
                // Ignore properties set by factory method
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SourceUrl, opt => opt.Ignore())
                .ForMember(dest => dest.LastSyncedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.Metadata, opt => opt.Ignore())
                
                // Ignore base entity properties (set elsewhere)
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                
                // After mapping, add metadata from DTO
                .AfterMap((src, dest) =>
                {
                    // Add metadata from DTO properties
                    if (!string.IsNullOrWhiteSpace(src.Category))
                        dest.AddMetadata("category", src.Category);
                    
                    if (src.Price.HasValue)
                        dest.AddMetadata("price", src.Price.Value);
                    
                    if (src.Rating.HasValue)
                        dest.AddMetadata("rating", src.Rating.Value);
                    
                    if (src.Tags != null && src.Tags.Length > 0)
                        dest.AddMetadata("tags", string.Join(",", src.Tags));
                    
                    if (!string.IsNullOrWhiteSpace(src.Status))
                        dest.AddMetadata("status", src.Status);
                    
                    if (src.UpdatedAt.HasValue)
                        dest.AddMetadata("source_timestamp", src.UpdatedAt.Value);
                });

            // ===== Domain Entity to Response DTO Mappings =====

            /// <summary>
            /// Maps ApiDataItem domain entity to ApiDataItemDto for API responses.
            /// </summary>
            CreateMap<ApiDataItem, ApiDataItemDto>()
                .ForMember(dest => dest.Id,
                    opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ExternalId,
                    opt => opt.MapFrom(src => src.ExternalId))
                .ForMember(dest => dest.Name,
                    opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description,
                    opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.SourceUrl,
                    opt => opt.MapFrom(src => src.SourceUrl))
                .ForMember(dest => dest.LastSyncedAt,
                    opt => opt.MapFrom(src => src.LastSyncedAt))
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.IsFresh,
                    opt => opt.MapFrom(src => src.IsFresh(TimeSpan.FromHours(1))))
                .ForMember(dest => dest.Age,
                    opt => opt.MapFrom(src => src.GetAge()))

                // Extract metadata into DTO properties
                .ForMember(dest => dest.Category,
                    opt => opt.MapFrom(src => src.GetMetadata<string>("category")))
                .ForMember(dest => dest.Price,
                    opt => opt.MapFrom(src => src.GetMetadata<decimal?>("price")))
                .ForMember(dest => dest.Rating,
                    opt => opt.MapFrom(src => src.GetMetadata<double?>("rating")))
                .ForMember(dest => dest.Tags,
                    opt => opt.MapFrom(src =>
                        src.HasMetadata("tags") && src.GetMetadata<string>("tags") != null
                            ? src.GetMetadata<string>("tags").Split(',', StringSplitOptions.RemoveEmptyEntries)
                            : null));

            // ===== Collection Mappings =====
            CreateMap<List<ApiDataItem>, List<ApiDataItemDto>>();
            CreateMap<ApiCollectionResponseDto<ApiItemDto>, List<ApiDataItem>>()
                .ConvertUsing((src, dest, context) =>
                {
                    // Map collection from Data property
                    return src.Data.Select(item => context.Mapper.Map<ApiDataItem>(item)).ToList();
                });
        }
    }
}
