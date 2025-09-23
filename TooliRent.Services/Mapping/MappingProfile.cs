using System.Linq;
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
            .ForMember(d => d.IsAvailable, o => o.MapFrom(_ => true));

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
        // Reservation (MULTI-ITEM)
        // =========================
        CreateMap<ReservationItem, ReservationItemDto>()
            .ConstructUsing(s => new ReservationItemDto(
                s.ToolId,
                s.Tool != null ? s.Tool.Name : string.Empty,
                s.PricePerDay
            ));

        CreateMap<Reservation, ReservationDto>()
            .ForMember(d => d.MemberName, o => o.MapFrom(s =>
                s.Member != null
                    ? $"{(s.Member.FirstName ?? string.Empty).Trim()} {(s.Member.LastName ?? string.Empty).Trim()}".Trim()
                    : string.Empty))
            .ForMember(d => d.Status,    o => o.MapFrom(s => (int)s.Status))
            .ForMember(d => d.Items,     o => o.MapFrom(s => s.Items ?? new List<ReservationItem>()))
            // ✅ Räkna ut direkt från src – ingen AfterMap → inga set-problem på dest
            .ForMember(d => d.ItemCount, o => o.MapFrom(s => s.Items != null ? s.Items.Count : 0))
            .ForMember(d => d.FirstToolName, o => o.MapFrom(s =>
                s.Items != null && s.Items.Count > 0
                    ? s.Items
                        .OrderBy(i => i.CreatedAtUtc)
                        .Select(i => i.Tool != null ? i.Tool.Name : null)
                        .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? string.Empty
                    : string.Empty));

        CreateMap<ReservationUpdateDto, Reservation>()
            .ForMember(d => d.Status, o => o.MapFrom(s => (ReservationStatus)s.Status));

        // =========================
        // Loan (MULTI-ITEM)
        // =========================
        CreateMap<LoanItem, LoanItemDto>()
            .ConstructUsing(s => new LoanItemDto(
                s.ToolId,
                s.Tool != null ? s.Tool.Name : string.Empty,
                s.PricePerDay
            ));

        // Loan -> LoanDto (med fallback till Reservation.Items om Loan saknar Items)
        CreateMap<Loan, LoanDto>()
            .ForMember(d => d.MemberName, o => o.MapFrom(s =>
                s.Member != null
                    ? $"{(s.Member.FirstName ?? string.Empty).Trim()} {(s.Member.LastName ?? string.Empty).Trim()}".Trim()
                    : string.Empty))
            .ForMember(d => d.Status, o => o.MapFrom(s => (int)s.Status))
            .ForMember(d => d.Items,  o => o.MapFrom(s => s.Items ?? new List<LoanItem>()))
            .AfterMap((src, dest) =>
            {
                // Fallback om Loans.Items saknas (t.ex. direkt efter checkout via reservation):
                if ((dest.Items == null || !dest.Items.Any()) &&
                    src.Reservation?.Items != null && src.Reservation.Items.Count > 0)
                {
                    dest.Items = src.Reservation.Items
                        .Select(ri => new LoanItemDto(
                            ri.ToolId,
                            ri.Tool != null ? ri.Tool.Name : string.Empty,
                            ri.PricePerDay))
                        .ToList();
                }

                var items = dest.Items?.ToList() ?? new List<LoanItemDto>();
                dest.ItemCount = items.Count;
                dest.FirstToolName = items
                    .Select(i => i.ToolName)
                    .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? string.Empty;
            });

        // Lättviktsvy från ENTITY (om du använder den någonstans)
        CreateMap<Loan, LoanOverviewDto>()
            .ForMember(d => d.ToolName, o => o.MapFrom(s =>
                s.Items != null && s.Items.Count > 0
                    ? string.Join(", ",
                        s.Items
                            .OrderBy(i => i.CreatedAtUtc)
                            .Select(i => i.Tool != null ? i.Tool.Name : null)
                            .Where(n => !string.IsNullOrWhiteSpace(n)))
                    : (s.Reservation != null && s.Reservation.Items != null && s.Reservation.Items.Count > 0
                        ? string.Join(", ",
                            s.Reservation.Items
                                .OrderBy(i => i.CreatedAtUtc)
                                .Select(i => i.Tool != null ? i.Tool.Name : null)
                                .Where(n => !string.IsNullOrWhiteSpace(n)))
                        : string.Empty)))
            .ForMember(d => d.Status, o => o.MapFrom(s => (int)s.Status));

        // Lättviktsvy från DTO (det är denna din controller använder i /api/loans/my)
        CreateMap<LoanDto, LoanOverviewDto>()
            .ForMember(d => d.ToolName, o => o.MapFrom(s =>
                s.Items != null && s.Items.Any()
                    ? string.Join(", ",
                        s.Items
                            .Select(i => i.ToolName)
                            .Where(n => !string.IsNullOrWhiteSpace(n)))
                    : string.Empty))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status));

        // =========================
        // Admin / Statistik
        // =========================
        CreateMap<AdminStatsResult, AdminStatsDto>();
        CreateMap<TopToolItem, TopToolDto>();
        CreateMap<CategoryUtilizationItem, CategoryUtilizationDto>();
        CreateMap<MemberActivityItem, MemberActivityDto>();
    }
}