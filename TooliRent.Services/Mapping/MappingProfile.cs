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
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Name, opt => opt.MapFrom(s => (s.Name ?? string.Empty).Trim()));

        CreateMap<ToolCategoryUpdateDto, ToolCategory>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Name, opt => opt.MapFrom(s => (s.Name ?? string.Empty).Trim()));

        // ----------------- Tool -----------------
        CreateMap<Tool, ToolDto>()
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty));

        CreateMap<ToolCreateDto, Tool>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Name, opt => opt.MapFrom(s => (s.Name ?? string.Empty).Trim()))
            .ForMember(d => d.Description, opt => opt.MapFrom(s => (s.Description ?? string.Empty).Trim()))
            .ForMember(d => d.IsAvailable, opt => opt.MapFrom(_ => true)); // ny-skapade tools är tillgängliga

        CreateMap<ToolUpdateDto, Tool>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Name, opt => opt.MapFrom(s => (s.Name ?? string.Empty).Trim()))
            .ForMember(d => d.Description, opt => opt.MapFrom(s => (s.Description ?? string.Empty).Trim()));

        // ----------------- Member -----------------
        CreateMap<Member, MemberDto>();

        CreateMap<MemberCreateDto, Member>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.FirstName, opt => opt.MapFrom(s => (s.FirstName ?? string.Empty).Trim()))
            .ForMember(d => d.LastName,  opt => opt.MapFrom(s => (s.LastName  ?? string.Empty).Trim()))
            .ForMember(d => d.Email,     opt => opt.MapFrom(s => (s.Email     ?? string.Empty).Trim()));

        CreateMap<MemberUpdateDto, Member>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.FirstName, opt => opt.MapFrom(s => (s.FirstName ?? string.Empty).Trim()))
            .ForMember(d => d.LastName,  opt => opt.MapFrom(s => (s.LastName  ?? string.Empty).Trim()))
            .ForMember(d => d.Email,     opt => opt.MapFrom(s => (s.Email     ?? string.Empty).Trim()));

        // ----------------- Reservation -----------------
        CreateMap<Reservation, ReservationDto>()
            .ForMember(d => d.ToolName,   opt => opt.MapFrom(s => s.Tool != null ? s.Tool.Name : string.Empty))
            .ForMember(d => d.MemberName, opt => opt.MapFrom(s =>
                (s.Member != null ? (s.Member.FirstName + " " + s.Member.LastName).Trim() : string.Empty)))
            .ForMember(d => d.Status,     opt => opt.MapFrom(s => (int)s.Status));

        CreateMap<ReservationCreateDto, Reservation>()
            .ForMember(d => d.Id,         opt => opt.Ignore())
            .ForMember(d => d.TotalPrice, opt => opt.Ignore()) // sätts i service
            .ForMember(d => d.IsPaid,     opt => opt.Ignore()) // sätts i service
            .ForMember(d => d.Status,     opt => opt.Ignore()); // sätts i service

        // ----------------- Loan -----------------
        CreateMap<Loan, LoanDto>()
            .ForMember(d => d.ToolName,   opt => opt.MapFrom(s => s.Tool != null ? s.Tool.Name : string.Empty))
            .ForMember(d => d.MemberName, opt => opt.MapFrom(s =>
                (s.Member != null ? (s.Member.FirstName + " " + s.Member.LastName).Trim() : string.Empty)))
            .ForMember(d => d.Status,     opt => opt.MapFrom(s => (int)s.Status));
        // CheckoutDto mappas i service, inte direkt
    }
}