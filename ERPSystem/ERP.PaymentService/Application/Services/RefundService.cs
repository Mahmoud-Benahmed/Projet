using ERP.PaymentService.Application.DTO;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain;
using static ERP.PaymentService.Properties.ApiRoutes;

namespace ERP.PaymentService.Application.Services;

public class RefundService : IRefundService
{
    private readonly IRefundRequestRepository _refundRepo;
    private readonly IPaymentInvoiceRepository _allocationRepo;

    public RefundService(
        IRefundRequestRepository refundRepo,
        IPaymentInvoiceRepository allocationRepo)
    {
        _refundRepo = refundRepo;
        _allocationRepo = allocationRepo;
    }


    public async Task<RefundStatsDto> GetStatsAsync()
    {
        return await _refundRepo.GetStatsAsync();
    }

    public async Task<RefundRequestDto> CreateRefundAsync(Guid clientId,
        Guid invoiceId, CancellationToken ct = default)
    {
        var allocations = await _allocationRepo.GetByInvoiceIdAsync(invoiceId);

        if (allocations == null || !allocations.Any())
            throw new InvalidOperationException("No refundable allocations found.");

        var refund = new RefundRequest(clientId, invoiceId);

        foreach (var alloc in allocations)
        {
            refund.AddLine(
                alloc.PaymentId,
                alloc.Id,
                Math.Round(alloc.AmountAllocated, 2)  // ← round on input
            );
        }

        if (!refund.Lines.Any())
            throw new InvalidOperationException("Refund has no valid lines.");

        await _refundRepo.AddAsync(refund, ct);
        await _refundRepo.SaveChangesAsync(ct);

        return ToDto(refund);
    }

    public async Task CompleteRefundAsync(Guid refundId, string externalReference, CancellationToken ct = default)
    {
        var refund = await _refundRepo.GetByIdAsync(refundId, ct)
            ?? throw new KeyNotFoundException($"Refund '{refundId}' not found.");

        refund.Complete();

        foreach (var line in refund.Lines)
        {
            var allocation = await _allocationRepo.GetByIdAsync(line.PaymentAllocationId)
                ?? throw new InvalidOperationException($"Allocation '{line.PaymentAllocationId}' not found.");

            allocation.Refund(Math.Round(line.Amount, 2));  // ← round before domain call
        }

        _refundRepo.Update(refund);
        await _refundRepo.SaveChangesAsync(ct);
    }

    public async Task<RefundRequestDto?> GetByIdAsync(Guid refundId, CancellationToken ct = default)
    {
        var result= await _refundRepo.GetByIdAsync(refundId, ct);
        return result is not null ? ToDto(result) : null;
    }

    public async Task<List<RefundRequestDto>> GetByClientIdAsync(Guid refundId,CancellationToken ct = default)
    {
        var result = await _refundRepo.GetByClientIdAsync(refundId);
        var dtos = result.Select(ToDto).ToList();
        return dtos;
    }


    private static RefundRequestDto ToDto(RefundRequest request) => new(
        Id: request.Id,
        ClientId: request.ClientId,
        InvoiceId: request.InvoiceId,
        Status: request.Status.ToString(),
        Lines: request.Lines.Select(l => new RefundLineDto(
            PaymentId: l.PaymentId,
            PaymentAllocationId: l.PaymentAllocationId,
            Amount: Math.Round(l.Amount, 2)  // ← round on output
        )).ToList()
    );
}