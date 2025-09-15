using AutoMapper;
using TooliRent.Core.Enums;
using TooliRent.Core.Models;
using TooliRent.Core.Models.Admin;
using TooliRent.Services.DTOs.Admins;
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
        // =========================
        // ToolCategory
        // =========================
        CreateMap<ToolCategory, ToolCategoryDto>();

        CreateMap<ToolCategoryCreateDto, ToolCategory>()
            .ForMember(d => d.Id,   o => o.Ignore())
            .ForMember(d => d.Name, o => o.MapFrom(s => (s.Name ?? string.Empty).Trim()));

        CreateMap<ToolCategoryUpdateDto, ToolCategory>()
            .ForMember(d => d.Id,   o => o.Ignore())
            .ForMember(d => d.Name, o => o.MapFrom(s => (s.Name ?? string.Empty).Trim()));

        // =========================
        // Tool
        // =========================
        CreateMap<Tool, ToolDto>()
            .ForMember(d => d.CategoryName,
                o => o.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty));

        CreateMap<ToolCreateDto, Tool>()
            .ForMember(d => d.Id,          o => o.Ignore())
            .ForMember(d => d.Name,        o => o.MapFrom(s => (s.Name ?? string.Empty).Trim()))
            .ForMember(d => d.Description, o => o.MapFrom(s => (s.Description ?? string.Empty).Trim()))
            .ForMember(d => d.IsAvailable, o => o.MapFrom(_ => true)); // nya tools = tillgängliga

        CreateMap<ToolUpdateDto, Tool>()
            .ForMember(d => d.Id,          o => o.Ignore())
            .ForMember(d => d.Name,        o => o.MapFrom(s => (s.Name ?? string.Empty).Trim()))
            .ForMember(d => d.Description, o => o.MapFrom(s => (s.Description ?? string.Empty).Trim()));

        // =========================
        // Member
        // =========================
        CreateMap<Member, MemberDto>();

        CreateMap<MemberCreateDto, Member>()
            .ForMember(d => d.Id,        o => o.Ignore())
            .ForMember(d => d.FirstName, o => o.MapFrom(s => (s.FirstName ?? string.Empty).Trim()))
            .ForMember(d => d.LastName,  o => o.MapFrom(s => (s.LastName  ?? string.Empty).Trim()))
            .ForMember(d => d.Email,     o => o.MapFrom(s => (s.Email     ?? string.Empty).Trim()));

        CreateMap<MemberUpdateDto, Member>()
            .ForMember(d => d.Id,        o => o.Ignore())
            .ForMember(d => d.FirstName, o => o.MapFrom(s => (s.FirstName ?? string.Empty).Trim()))
            .ForMember(d => d.LastName,  o => o.MapFrom(s => (s.LastName  ?? string.Empty).Trim()))
            .ForMember(d => d.Email,     o => o.MapFrom(s => (s.Email     ?? string.Empty).Trim()));

        // =========================
        // Reservation
        // =========================
        CreateMap<Reservation, ReservationDto>()
            .ForMember(d => d.ToolName,
                o => o.MapFrom(s => s.Tool != null ? s.Tool.Name : string.Empty))
            .ForMember(d => d.MemberName,
                o => o.MapFrom(s => s.Member != null
                    ? $"{(s.Member.FirstName ?? string.Empty).Trim()} {(s.Member.LastName ?? string.Empty).Trim()}".Trim()
                    : string.Empty))
            .ForMember(d => d.Status, o => o.MapFrom(s => (int)s.Status));

        CreateMap<ReservationBatchCreateDto, Reservation>()
            .ForMember(d => d.Id,         o => o.Ignore())
            .ForMember(d => d.TotalPrice, o => o.Ignore()) // beräknas i service
            .ForMember(d => d.IsPaid,     o => o.Ignore()) // sätts i service
            .ForMember(d => d.Status,     o => o.Ignore()); // sätts i service

        CreateMap<ReservationUpdateDto, Reservation>()
            .ForMember(d => d.Status, o => o.MapFrom(s => (ReservationStatus)s.Status));

        // =========================
        // Loan
        // =========================
        CreateMap<Loan, LoanDto>()
            .ForMember(d => d.ToolName,
                o => o.MapFrom(s => s.Tool != null ? s.Tool.Name : string.Empty))
            .ForMember(d => d.MemberName,
                o => o.MapFrom(s => s.Member != null
                    ? $"{(s.Member.FirstName ?? string.Empty).Trim()} {(s.Member.LastName ?? string.Empty).Trim()}".Trim()
                    : string.Empty))
            .ForMember(d => d.Status, o => o.MapFrom(s => (int)s.Status));
        // Obs: Checkout-mappning sker i service (vi skapar Loan manuellt där)
        
        CreateMap<Loan, LoanOverviewDto>()
            .ForMember(d => d.ToolName, opt => opt.MapFrom(s => s.Tool != null ? s.Tool.Name : string.Empty))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => (int)s.Status));
        
        CreateMap<LoanDto, LoanOverviewDto>();

        // =========================
        // Admin / Statistik
        // =========================
        CreateMap<AdminStatsResult, AdminStatsDto>();
        CreateMap<TopToolItem, TopToolDto>();
        CreateMap<CategoryUtilizationItem, CategoryUtilizationDto>();
        CreateMap<MemberActivityItem, MemberActivityDto>();
    }
}