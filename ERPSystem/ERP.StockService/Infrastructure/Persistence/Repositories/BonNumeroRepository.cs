namespace ERP.StockService.Infrastructure.Persistence.Repositories
{
    // Infrastructure/Persistence/Repositories/DocumentNumberRepository.cs
    using ERP.StockService.Domain;
    using Microsoft.EntityFrameworkCore;

    public class BonNumeroRepository : IBonNumeroRepository
    {
        private readonly StockDbContext _context;

        // Document type to prefix mapping
        private static readonly Dictionary<string, string> DocumentPrefixes = new()
    {
        { "BON_ENTRE", "BE" },
        { "BON_SORTIE", "BS" },
        { "BON_RETOUR", "BR" }
    };

        public BonNumeroRepository(StockDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetNextDocumentNumberAsync(string documentType)
        {
            // Use a raw SQL query with row lock to ensure uniqueness across concurrent requests
            BonNumber? sequence = await _context.BonNumber
                .FromSqlRaw(
                    @"SELECT * FROM BonNumbers WITH (UPDLOCK, ROWLOCK)
                  WHERE DocumentType = {0}",
                    documentType)
                .FirstOrDefaultAsync();

            int year = DateTime.UtcNow.Year;

            if (sequence == null)
            {
                // Create new sequence for this document type
                string prefix = DocumentPrefixes[documentType];
                sequence = new BonNumber(documentType, prefix);
                _context.BonNumber.Add(sequence);

                // First number is 1
                sequence.Increment();
                await _context.SaveChangesAsync();

                return sequence.FormatNumber(year);
            }

            // Increment the sequence
            sequence.Increment();
            await _context.SaveChangesAsync();

            return sequence.FormatNumber(year);
        }

        public async Task<BonNumber?> GetSequenceAsync(string documentType)
        {
            return await _context.BonNumber
                .FirstOrDefaultAsync(s => s.DocumentType == documentType);
        }
    }
}
