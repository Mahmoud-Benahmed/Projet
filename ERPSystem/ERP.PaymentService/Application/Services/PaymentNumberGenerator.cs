using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ERP.PaymentService.Infrastructure.Persistence;

public class PaymentNumberGenerator : IPaymentNumberGenerator
{
    private readonly PaymentDbContext _context;

    public PaymentNumberGenerator(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateNextPaymentNumberAsync()
    {
        int currentYear = DateTime.UtcNow.Year;

        await using IDbContextTransaction transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            PaymentSequence? sequence = await _context.PaymentSequences
                .FirstOrDefaultAsync(s => s.Year == currentYear);

            if (sequence is null)
            {
                sequence = new PaymentSequence(currentYear);
                _context.PaymentSequences.Add(sequence);
                await _context.SaveChangesAsync();
            }
            else
            {
                // reload with lock to get latest value
                // prevents two concurrent requests getting the same number
                await _context.Entry(sequence).ReloadAsync();
            }

            sequence.GetNextNumber();
            string paymentNumber = sequence.FormatPaymentNumber();

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return paymentNumber;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            // retry once on concurrency conflict
            return await GenerateNextPaymentNumberAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}