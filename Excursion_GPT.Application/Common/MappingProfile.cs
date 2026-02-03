using AutoMapper;
using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Domain.Entities;

namespace Excursion_GPT.Application.Common;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User Mappings (keep existing for authentication)
        CreateMap<User, UserDto>();
        CreateMap<UserCreateDto, User>();
        CreateMap<UserUpdateDto, User>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Note: The new API uses different DTO structures that don't directly map to entities
        // Most of the new endpoints use mock data or custom service logic
        // This profile is kept minimal for backward compatibility with existing authentication

        // Building mappings are handled in BuildingService with custom logic
        // Model mappings are handled in ModelService with custom logic
        // Track and Point mappings are handled in TrackService and PointService with custom logic
    }
}
