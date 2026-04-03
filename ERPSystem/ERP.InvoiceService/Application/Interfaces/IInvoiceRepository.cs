using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InvoiceService.Domain;

namespace InvoiceService.Application.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByIdAsync(Guid id);
        Task<Invoice?> GetByIdWithItemsAsync(Guid id);
        Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);

        Task<IEnumerable<Invoice>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<Invoice>> GetByClientIdAsync(Guid clientId);
        Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status);
        Task AddAsync(Invoice invoice);
        Task UpdateAsync(Invoice invoice);
        Task<bool> ExistsByInvoiceNumberAsync(string invoiceNumber);
    }
}