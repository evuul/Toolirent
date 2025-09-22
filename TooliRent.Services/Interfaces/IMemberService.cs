using TooliRent.Services.DTOs.Members;

namespace TooliRent.Services.Interfaces;

public interface IMemberService
{
    // ===== Vanliga CRUD =====
    Task<IEnumerable<MemberDto>> GetAllAsync(CancellationToken ct = default);
    Task<MemberDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<MemberDto> CreateAsync(MemberCreateDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, MemberUpdateDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default); // soft

    // ===== Admin-funktioner =====

    /// <summary>
    /// Sök och lista medlemmar, med enkel filtrering (namn, e-post).
    /// Paginering används för att admin ska kunna bläddra i listan.
    /// </summary>
    Task<(IEnumerable<MemberDto> Items, int Total)> SearchAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Sätt om en medlem ska vara aktiv eller inaktiv.
    /// Inaktiva medlemmar kan inte logga in.
    /// </summary>
    Task<bool> SetActiveAsync(Guid memberId, bool isActive, CancellationToken ct = default);
    
    /// <summary>
    /// Sätt aktiv/inaktiv. Bump:ar TokenVersion och returnerar IdentityUserId
    /// så att refresh-tokens kan revokeras av anroparen (controller).
    /// </summary>
    Task<(bool Success, string? IdentityUserId)> SetActiveAndBumpAsync(
        Guid memberId, bool isActive, CancellationToken ct = default);
}