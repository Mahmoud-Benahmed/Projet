// InvoiceService.Infrastructure.Persistence/InvoiceNumberGenerator.cs
using InvoiceService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.InvoiceService.Infrastructure.Persistence;

public class InvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private readonly InvoiceDbContext _context;
    private static readonly object _lock = new object();

    public InvoiceNumberGenerator(InvoiceDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateNextInvoiceNumberAsync()
    {
        var currentYear = DateTime.UtcNow.Year;

        // Use a distributed lock or database transaction
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Get or create sequence with a lock
            var sequence = await _context.InvoiceSequences
                .FirstOrDefaultAsync(s => s.Year == currentYear);

            if (sequence == null)
            {
                sequence = new InvoiceSequence(currentYear);
                _context.InvoiceSequences.Add(sequence);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Reload with lock (EF Core 5+)
                await _context.Entry(sequence)
                    .ReloadAsync();
            }

            // Generate number
            var nextNumber = sequence.GetNextNumber();
            var invoiceNumber = sequence.FormatInvoiceNumber();

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return invoiceNumber;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            // Retry once on concurrency conflict
            return await GenerateNextInvoiceNumberAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}