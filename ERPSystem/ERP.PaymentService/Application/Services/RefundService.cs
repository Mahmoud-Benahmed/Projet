using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain;

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

    public async Task<Guid> CreateRefundAsync(Guid clientId,Guid invoiceId,CancellationToken ct = default)
    {
        // 🔥 1. Get allocations for the invoice
        var allocations = await _allocationRepo.GetByInvoiceIdAsync(invoiceId);

        if (allocations == null || !allocations.Any())
            throw new InvalidOperationException("No refundable allocations found.");



        // 🔥 2. Create aggregate
        var refund = new RefundRequest(clientId, invoiceId);

        foreach (var alloc in allocations)
        {
            refund.AddLine(
                alloc.PaymentId,
                alloc.Id,
                alloc.AmountAllocated
            );
        }

        if (!refund.Lines.Any())
            throw new InvalidOperationException("Refund has no valid lines.");

        // 🔥 4. Persist
        await _refundRepo.AddAsync(refund, ct);
        await _refundRepo.SaveChangesAsync(ct);

        return refund.Id;
    }

    public async Task CompleteRefundAsync(Guid refundId, string externalReference, CancellationToken ct = default)
    {
        var refund = await _refundRepo.GetByIdAsync(refundId, ct)
            ?? throw new KeyNotFoundException("Refund not found.");

        // 🔥 Domain transition
        refund.Complete();

        // 🔥 Update allocations (critical!)
        foreach (var line in refund.Lines)
        {
            var allocation = await _allocationRepo.GetByIdAsync(line.PaymentAllocationId);

            if (allocation == null)
                throw new InvalidOperationException("Allocation not found.");

            allocation.Refund(line.Amount);
        }

        _refundRepo.Update(refund);
        await _refundRepo.SaveChangesAsync(ct);
    }

    public async Task<RefundRequest?> GetByIdAsync(Guid refundId, CancellationToken ct = default)
    {
        return await _refundRepo.GetByIdAsync(refundId, ct);
    }
}