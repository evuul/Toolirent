using AutoMapper;
using TooliRent.Core.Models;
using TooliRent.Services.DTOs.ToolCategories;
using TooliRent.Services.DTOs.Tools;
using TooliRent.Services.DTOs.Members;
using TooliRent.Services.DTOs.Reservations;
using TooliRent.Services.DTOs.Loans;

namespace TooliRent.Services.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ----------------- ToolCategory -----------------
        CreateMap<ToolCategory, ToolCategoryDto>();
        CreateMap<ToolCategoryCreateDto, ToolCategory>()
            .ForMember(d => d.Id, opt => opt.Ignore());
        CreateMap<ToolCategoryUpdateDto, ToolCategory>()
            .ForMember(d => d.Id, opt => opt.Ignore());

        // ----------------- Tool -----------------
        CreateMap<Tool, ToolDto>()
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category.Name));
        CreateMap<ToolCreateDto, Tool>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Description ?? string.Empty))
            .ForMember(d => d.IsAvailable, opt => opt.MapFrom(_ => true));
        CreateMap<ToolUpdateDto, Tool>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Description ?? string.Empty));

        // ----------------- Member -----------------
        CreateMap<Member, MemberDto>();
        CreateMap<MemberCreateDto, Member>()
            .ForMember(d => d.Id, opt => opt.Ignore());
        CreateMap<MemberUpdateDto, Member>()
            .ForMember(d => d.Id, opt => opt.Ignore());

        // ----------------- Reservation -----------------
        CreateMap<Reservation, ReservationDto>()
            .ForMember(d => d.ToolName,   opt => opt.MapFrom(s => s.Tool.Name))
            .ForMember(d => d.MemberName, opt => opt.MapFrom(s => s.Member.FirstName + " " + s.Member.LastName))
            .ForMember(d => d.Status,     opt => opt.MapFrom(s => (int)s.Status));
        CreateMap<ReservationCreateDto, Reservation>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.TotalPrice, opt => opt.Ignore()) // sätts i service
            .ForMember(d => d.IsPaid,     opt => opt.Ignore()) // sätts i service
            .ForMember(d => d.Status,     opt => opt.Ignore()); // sätts i service

        // ----------------- Loan -----------------
        CreateMap<Loan, LoanDto>()
            .ForMember(d => d.ToolName,   opt => opt.MapFrom(s => s.Tool.Name))
            .ForMember(d => d.MemberName, opt => opt.MapFrom(s => s.Member.FirstName + " " + s.Member.LastName))
            .ForMember(d => d.Status,     opt => opt.MapFrom(s => (int)s.Status));
        // CheckoutDto mappas inte direkt till Loan – skapas i service
    }
}