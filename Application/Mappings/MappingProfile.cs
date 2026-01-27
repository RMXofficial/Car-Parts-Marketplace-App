using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>();

        // Category mappings
        CreateMap<Category, CategoryDto>();

        // Listing mappings
        CreateMap<Listing, ListingDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : ""))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserId)); // Using UserId as UserName for now

        CreateMap<CreateListingDto, Listing>();

        // Order mappings
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserId)); // Using UserId as UserName for now

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.ListingTitle, opt => opt.MapFrom(src => src.Listing.Title));

        // Price transformation is handled separately in the service
    }
}
