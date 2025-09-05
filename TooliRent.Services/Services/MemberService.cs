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

    public async Task<IEnumerable<MemberDto>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _uow.Members.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<MemberDto>>(list);
    }

    public async Task<MemberDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _uow.Members.GetByIdAsync(id, ct);
        return entity is null ? null : _mapper.Map<MemberDto>(entity);
    }

    public async Task<MemberDto> CreateAsync(MemberCreateDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email is required.");

        var entity = _mapper.Map<Member>(dto);
        await _uow.Members.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return _mapper.Map<MemberDto>(entity);
    }

    public async Task<bool> UpdateAsync(Guid id, MemberUpdateDto dto, CancellationToken ct = default)
    {
        var existing = await _uow.Members.GetByIdAsync(id, ct);
        if (existing is null) return false;

        _mapper.Map(dto, existing);
        existing.Id = id;

        await _uow.Members.UpdateAsync(existing, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _uow.Members.GetByIdAsync(id, ct);
        if (existing is null) return false;

        existing.DeletedAtUtc = DateTime.UtcNow; // soft
        await _uow.Members.UpdateAsync(existing, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }
}