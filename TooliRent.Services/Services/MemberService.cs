using AutoMapper;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.DTOs.Members;
using TooliRent.Services.Interfaces;

namespace TooliRent.Services.Services;

public class MemberService : IMemberService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public MemberService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    // =========================
    // Läsning (alla / en)
    // =========================

    /// <summary>
    /// Hämta alla medlemmar (respekterar soft delete via globalt filter).
    /// Avsedd mest för interna behov. För admin-listning: använd SearchAsync.
    /// </summary>
    public async Task<IEnumerable<MemberDto>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _uow.Members.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<MemberDto>>(list);
    }

    /// <summary>
    /// Hämta en specifik medlem.
    /// </summary>
    public async Task<MemberDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _uow.Members.GetByIdAsync(id, ct);
        return entity is null ? null : _mapper.Map<MemberDto>(entity);
    }

    // =========================
    // Skapa / Uppdatera / Radera (soft)
    // =========================

    /// <summary>
    /// Skapa medlem (domänpost – inte Identity-användare).
    /// </summary>
    public async Task<MemberDto> CreateAsync(MemberCreateDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email krävs.");

        var entity = _mapper.Map<Member>(dto);
        // Sätt standardvärden som inte bör lämnas åt klienten
        entity.IsActive = true;

        await _uow.Members.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return _mapper.Map<MemberDto>(entity);
    }

    /// <summary>
    /// Uppdatera medlem. Rör inte IdentityUserId här.
    /// </summary>
    public async Task<bool> UpdateAsync(Guid id, MemberUpdateDto dto, CancellationToken ct = default)
    {
        var existing = await _uow.Members.GetByIdAsync(id, ct);
        if (existing is null) return false;

        // Mappa in fält som är tillåtna att uppdatera
        _mapper.Map(dto, existing);

        // Skydda mot oavsiktliga förändringar
        existing.Id = id; // säkerställ
        // existing.IdentityUserId = existing.IdentityUserId; // lämna oförändrat

        await _uow.Members.UpdateAsync(existing, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }

    /// <summary>
    /// Soft delete – markerar som borttagen.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _uow.Members.GetByIdAsync(id, ct);
        if (existing is null) return false;

        existing.DeletedAtUtc = DateTime.UtcNow;
        await _uow.Members.UpdateAsync(existing, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }

    // =========================
    // Admin-funktioner
    // =========================

    /// <summary>
    /// Sök/paginera medlemmar (för admin). Sökning sker på för-/efternamn och email.
    /// </summary>
    public async Task<(IEnumerable<MemberDto> Items, int Total)> SearchAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        // Rimliga gränser för paginering
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        // Delegera till repository (antingen har du en MemberRepository.SearchAsync,
        // eller så kan du bygga den där likt Tool/Loan-search).
        var (items, total) = await _uow.Members.SearchAsync(query, page, pageSize, ct);

        var mapped = _mapper.Map<IEnumerable<MemberDto>>(items);
        return (mapped, total);
    }

    /// <summary>
    /// Sätt en medlems aktiva status (true = aktiv, false = inaktiv).
    /// Inaktiva medlemmar nekas inloggning (kollas i AuthController.Login).
    /// </summary>
    public async Task<bool> SetActiveAsync(Guid memberId, bool isActive, CancellationToken ct = default)
    {
        var member = await _uow.Members.GetByIdAsync(memberId, ct);
        if (member is null) return false;

        member.IsActive = isActive;
        await _uow.Members.UpdateAsync(member, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }
}