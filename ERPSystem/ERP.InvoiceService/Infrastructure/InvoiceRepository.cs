using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InvoiceService.Application.Interfaces;
using InvoiceService.Domain;

namespace InvoiceService.Infrastructure
{
 
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly InvoiceDbContext _context;
        public InvoiceRepository(InvoiceDbContext context)
        {
            _context = context;
        }
        public async Task<Invoice?> GetByIdAsync(Guid id)
        {
            return await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id);
        }
        public async Task<Invoice?> GetByIdWithItemsAsync(Guid id)
        {
            return await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);
        }
        public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
        {
            return await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
        }

        public async Task<IEnumerable<Invoice>> GetAllAsync(bool includeDeleted = false)
        {
       
            var query = includeDeleted
                ? _context.Invoices.IgnoreQueryFilters()  
                : _context.Invoices.AsQueryable();         

            return await query
                .Include(i => i.Items)
                .ToListAsync();
        }
        public async Task<IEnumerable<Invoice>> GetByClientIdAsync(Guid clientId)
        {
            return await _context.Invoices
                .Include(i => i.Items)
                .Where(i => i.ClientId == clientId)
                .ToListAsync();
        }
        public async Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status)
        {
            return await _context.Invoices
                .Include(i => i.Items)
                .Where(i => i.Status == status)
                .ToListAsync();
        }
        public async Task AddAsync(Invoice invoice)
        {
            // Add the invoice 
            await _context.Invoices.AddAsync(invoice);

            // Save changes to the database
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Invoice invoice)
        {
            // Mark as modified
            _context.Invoices.Update(invoice);

            // Save changes to the database
            await _context.SaveChangesAsync();
        }
        public async Task<bool> ExistsByInvoiceNumberAsync(string invoiceNumber)
        {
            return await _context.Invoices
                .IgnoreQueryFilters() 
                .AnyAsync(i => i.InvoiceNumber == invoiceNumber);
        }
    }
}