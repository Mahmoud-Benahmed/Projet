using ERP.PaymentService.Application.DTOs.Payment;
using ERP.PaymentService.Application.Exceptions;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain.Entities;
using ERP.PaymentService.Domain.Enums;
using ERP.PaymentService.Infrastructure.Messaging;
using ERP.PaymentService.Infrastructure.Messaging.Events.InvoiceEvents;

namespace ERP.PaymentService.Application.Services
{
    public class PaymentsService : IPaymentsService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IInvoiceCacheRepository _invoiceCacheRepository;
        private readonly ILateFeePolicyRepository _lateFeePolicyRepository;
        private readonly IKafkaEventPublisher _kafkaEventPublisher;
        private readonly ILogger<PaymentsService> _logger;

        public PaymentsService(
            ILogger<PaymentsService> logger,
            IPaymentRepository paymentRepository,
            IInvoiceCacheRepository invoiceCacheRepository,
            ILateFeePolicyRepository lateFeePolicyRepository,
            IKafkaEventPublisher kafkaEventPublisher)
        {
            _logger = logger;
            _paymentRepository = paymentRepository;
            _invoiceCacheRepository = invoiceCacheRepository;
            _lateFeePolicyRepository = lateFeePolicyRepository;
            _kafkaEventPublisher = kafkaEventPublisher;
        }

        // ════════════════════════════════════════════════════════════════════════════
        // QUERY OPERATIONS
        // ════════════════════════════════════════════════════════════════════════════

        public async Task<List<PaymentDto>> GetAllAsync()
        {
            var payments = await _paymentRepository.GetAllAsync();
            return payments.Select(p => p.ToDto()).ToList();
        }

        public async Task<PaymentDto> GetByIdAsync(Guid id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id)
                ?? throw new PaymentNotFoundException(id);

            return payment.ToDto();
        }

        public async Task<List<PaymentDto>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            var payments = await _paymentRepository.GetByInvoiceIdAsync(invoiceId);
            return payments.Select(p => p.ToDto()).ToList();
        }

        public async Task<List<PaymentDto>> GetByClientIdAsync(Guid clientId)
        {
            var payments = await _paymentRepository.GetByClientIdAsync(clientId);
            return payments.Select(p => p.ToDto()).ToList();
        }

        public async Task<List<PaymentDto>> GetByStatusAsync(PaymentStatus status)
        {
            var payments = await _paymentRepository.GetByStatusAsync(status);
            return payments.Select(p => p.ToDto()).ToList();
        }

        public async Task<PaymentStatsDto> GetStatsAsync()
        {
            var all = await _paymentRepository.GetAllAsync();

            var totalCompleted = all.Count(p => p.Status == PaymentStatus.COMPLETED);
            var totalPending = all.Count(p => p.Status == PaymentStatus.PENDING);
            var totalFailed = all.Count(p => p.Status == PaymentStatus.FAILED);
            var totalRevenue = all
                .Where(p => p.Status == PaymentStatus.COMPLETED)
                .Sum(p => p.Amount);

            return new PaymentStatsDto(
                TotalPayments: all.Count,
                TotalCompleted: totalCompleted,
                TotalPending: totalPending,
                TotalFailed: totalFailed,
                TotalRevenue: totalRevenue);
        }

        public async Task<PaymentSummaryDto> GetPaymentSummaryAsync(Guid invoiceId)
        {
            var invoice = await _invoiceCacheRepository.GetByIdAsync(invoiceId)
                ?? throw new InvoiceNotFoundException(invoiceId);

            var payments = await _paymentRepository.GetByInvoiceIdAsync(invoiceId);

            _logger.LogInformation(
                "\n\nFetching payment summary for invoice {InvoiceId}. TotalTTC={TotalTTC}, TotalPaid={TotalPaid}\n\n",
                invoiceId, invoice.TotalTTC, invoice.TotalPaid);

            return new PaymentSummaryDto(
                InvoiceId: invoice.InvoiceId,
                TotalTTC: invoice.TotalTTC,
                TotalPaid: invoice.TotalPaid,
                RemainingAmount: invoice.TotalTTC - invoice.TotalPaid,
                InvoiceStatus: invoice.Status,
                LateFeeAmount: invoice.LateFeeAmount,
                Payments: payments.Select(p => p.ToDto()).ToList());
        }

        // ════════════════════════════════════════════════════════════════════════════
        // CREATE OPERATION
        // ════════════════════════════════════════════════════════════════════════════

        public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto)
        {
            // ──── 1. Fetch invoice from cache ────
            var invoice = await _invoiceCacheRepository.GetByIdAsync(dto.InvoiceId)
                ?? throw new InvoiceNotFoundException(dto.InvoiceId);

            // ──── 2. Validate invoice state ────
            if (invoice.Status == "CANCELLED")
                throw new InvoiceCancelledException(dto.InvoiceId);

            if (invoice.Status == "PAID")
                throw new InvoiceAlreadyPaidException(dto.InvoiceId);

            _logger.LogInformation(
                "\n\nCreating payment for invoice {InvoiceId}. Amount={Amount}, Method={Method}\n\n",
                dto.InvoiceId, dto.Amount, dto.Method);

            // ──── 3. Check and apply late fee ────
            var activePolicy = await _lateFeePolicyRepository.GetActivePolicyAsync();

            if (activePolicy is not null && activePolicy.IsOverdue(invoice.DueDate) && !invoice.LateFeeApplied)
            {
                var daysOverdue = (int)(DateTime.UtcNow - invoice.DueDate.AddDays(activePolicy.GracePeriodDays)).TotalDays;
                var fee = activePolicy.CalculateTotalFee(invoice.TotalTTC, daysOverdue);

                _logger.LogInformation(
                    "\n\nApplying late fee of {Fee} to invoice {InvoiceId}. DaysOverdue={DaysOverdue}\n\n",
                    fee, dto.InvoiceId, daysOverdue);

                invoice.LateFeeAmount = fee;
                invoice.LateFeeApplied = true;
                invoice.TotalTTC += fee;

                await _invoiceCacheRepository.UpsertAsync(invoice);
            }

            // ──── 4. Create payment record with Status = COMPLETED ────
            var payment = new Payment(
                invoiceId: dto.InvoiceId,
                clientId: invoice.ClientId,
                amount: dto.Amount,
                method: dto.Method,
                paymentDate: dto.PaymentDate);

            // ──── 5. Save payment to database ────
            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            // ──── 6. Fetch ALL completed payments for this invoiceId ────
            var completedPayments = await _paymentRepository.GetCompletedByInvoiceIdAsync(dto.InvoiceId);

            // ──── 7. Calculate totalPaid ────
            var totalPaid = completedPayments.Sum(p => p.Amount);

            // ──── 8. Update invoice.TotalPaid in cache ────
            invoice.TotalPaid = totalPaid;

            // ──── 9. Determine invoice status ────
            if (totalPaid >= invoice.TotalTTC)
            {
                invoice.Status = "PAID";

                _logger.LogInformation(
                    "\n\nInvoice {InvoiceId} fully paid. Publishing InvoicePaidEvent. TotalTTC={TotalTTC}, TotalPaid={TotalPaid}\n\n",
                    dto.InvoiceId, invoice.TotalTTC, totalPaid);

                var paidEvent = new InvoicePaidEvent
                {
                    InvoiceId = invoice.InvoiceId,
                    ClientId = invoice.ClientId,
                    TotalTTC = invoice.TotalTTC,
                    TotalPaid = totalPaid,
                    PaidAt = DateTime.UtcNow
                };

                await _kafkaEventPublisher.PublishAsync(PaymentTopics.InvoicePaid, paidEvent);
            }
            else
            {
                // ──── 10. Invoice remains UNPAID ────
                invoice.Status = "UNPAID";

                _logger.LogInformation(
                    "\n\nInvoice {InvoiceId} partially paid. Remaining={Remaining}\n\n",
                    dto.InvoiceId, invoice.TotalTTC - totalPaid);
            }

            // ──── 11. Update invoice in cache ────
            await _invoiceCacheRepository.UpsertAsync(invoice);

            // ──── 12. Return PaymentDto ────
            return payment.ToDto();
        }

        // ════════════════════════════════════════════════════════════════════════════
        // UPDATE OPERATION
        // ════════════════════════════════════════════════════════════════════════════

        public async Task<PaymentDto> UpdateAsync(Guid id, UpdatePaymentDto dto)
        {
            var payment = await _paymentRepository.GetByIdAsync(id)
                ?? throw new PaymentNotFoundException(id);

            _logger.LogInformation(
                "\n\nUpdating payment {PaymentId}. Amount={Amount}, Method={Method}\n\n",
                id, dto.Amount, dto.Method);

            payment.Update(dto.Amount, dto.Method, dto.PaymentDate);

            await _paymentRepository.UpdateAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            return payment.ToDto();
        }

        // ════════════════════════════════════════════════════════════════════════════
        // SOFT DELETE OPERATIONS
        // ════════════════════════════════════════════════════════════════════════════

        public async Task DeleteAsync(Guid id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id)
                ?? throw new PaymentNotFoundException(id);

            payment.Delete();

            await _paymentRepository.UpdateAsync(payment);
            await _paymentRepository.SaveChangesAsync();
        }

        public async Task RestoreAsync(Guid id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id)
                ?? throw new PaymentNotFoundException(id);

            payment.Restore();

            await _paymentRepository.UpdateAsync(payment);
            await _paymentRepository.SaveChangesAsync();
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // MAPPING EXTENSIONS
    // ════════════════════════════════════════════════════════════════════════════

    internal static class PaymentMappingExtensions
    {
        internal static PaymentDto ToDto(this Domain.Entities.Payment p) => new(
            Id: p.Id,
            InvoiceId: p.InvoiceId,
            ClientId: p.ClientId,
            Amount: p.Amount,
            Method: p.Method,
            Status: p.Status,
            LateFeeApplied: p.LateFeeApplied,
            PaymentDate: p.PaymentDate,
            CreatedAt: p.CreatedAt);
    }
}
