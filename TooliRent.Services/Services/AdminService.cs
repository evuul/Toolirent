using AutoMapper;
using TooliRent.Core.Interfaces;
using TooliRent.Services.DTOs.Admins;

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
}

public class AdminService : IAdminService
{
    private readonly ILoanRepository _loans;
    private readonly IMapper _mapper;

    public AdminService(ILoanRepository loans, IMapper mapper)
    {
        _loans = loans;
        _mapper = mapper;
    }

    public async Task<AdminStatsDto> GetStatsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var result = await _loans.GetAdminStatsAsync(fromUtc, toUtc, ct);
        return _mapper.Map<AdminStatsDto>(result);
    }
}