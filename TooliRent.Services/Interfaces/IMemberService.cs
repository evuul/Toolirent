using TooliRent.Services.DTOs.Members;

namespace TooliRent.Services.Interfaces;

public interface IMemberService
{
    Task<IEnumerable<MemberDto>> GetAllAsync(CancellationToken ct = default);
    Task<MemberDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<MemberDto> CreateAsync(MemberCreateDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, MemberUpdateDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default); // soft
}