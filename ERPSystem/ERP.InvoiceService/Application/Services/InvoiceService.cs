using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvoiceService.Application.DTOs;
using InvoiceService.Application.Exceptions;
using InvoiceService.Application.Interfaces;
using InvoiceService.Domain;

namespace InvoiceService.Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        public InvoiceService(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }
        public async Task<InvoiceDto> GetByIdAsync(Guid id)
        {
            var invoice = await _invoiceRepository.GetByIdWithItemsAsync(id)
                ?? throw new InvoiceNotFoundException(id);

            return invoice.ToDto();
        }
        public async Task<IEnumerable<InvoiceDto>> GetAllAsync(bool includeDeleted = false)
        {
            var invoices = await _invoiceRepository.GetAllAsync(includeDeleted);
            return invoices.Select(i => i.ToDto());
        }
        public async Task<IEnumerable<InvoiceDto>> GetByClientIdAsync(Guid clientId)
        {
            var invoices = await _invoiceRepository.GetByClientIdAsync(clientId);
            return invoices.Select(i => i.ToDto());
        }
        public async Task<IEnumerable<InvoiceDto>> GetByStatusAsync(InvoiceStatus status)
        {
            var invoices = await _invoiceRepository.GetByStatusAsync(status);
            return invoices.Select(i => i.ToDto());
        }

        // ════════════════════════════════════════════════════════════════════════════
        // CREATE OPERATIONS
        // ════════════════════════════════════════════════════════════════════════════

        public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto)
        {
            // ──── VALIDATION ────
            // Check if invoice number already exists
            if (await _invoiceRepository.ExistsByInvoiceNumberAsync(dto.InvoiceNumber))
                throw new InvoiceAlreadyExistsException(dto.InvoiceNumber);

            // ──── CREATE INVOICE ────
            var invoice = new Invoice(
                dto.InvoiceNumber,
                dto.InvoiceDate,
                dto.DueDate,
                dto.ClientId,
                dto.ClientFullName,
                dto.ClientAddress,
                dto.AdditionalNotes);

            foreach (var itemDto in dto.Items)
            {
                var item = new InvoiceItem(
                    invoice.Id,
                    itemDto.ArticleId,
                    itemDto.ArticleName,
                    itemDto.ArticleBarCode,
                    itemDto.Quantity,
                    itemDto.UniPriceHT,
                    itemDto.TaxRate);

                invoice.AddItem(item);
            }

    
            await _invoiceRepository.AddAsync(invoice);
            return invoice.ToDto();
        }

        public async Task<InvoiceDto> AddItemAsync(Guid invoiceId, AddInvoiceItemDto dto)
        {
         
            var invoice = await _invoiceRepository.GetByIdWithItemsAsync(invoiceId)
                ?? throw new InvoiceNotFoundException(invoiceId);

       
            var item = new InvoiceItem(
                invoiceId,
                dto.ArticleId,
                dto.ArticleName,
                dto.ArticleBarCode,
                dto.Quantity,
                dto.UniPriceHT,
                dto.TaxRate);

            invoice.AddItem(item);

    
            await _invoiceRepository.UpdateAsync(invoice);
            return invoice.ToDto();
        }

    
        public async Task RemoveItemAsync(Guid invoiceId, Guid itemId)
        {
    
            var invoice = await _invoiceRepository.GetByIdWithItemsAsync(invoiceId)
                ?? throw new InvoiceNotFoundException(invoiceId);
            invoice.RemoveItem(itemId);

            await _invoiceRepository.UpdateAsync(invoice);
        }

        public async Task<InvoiceDto> FinalizeAsync(Guid id)
        {
  
            var invoice = await _invoiceRepository.GetByIdWithItemsAsync(id)
                ?? throw new InvoiceNotFoundException(id);
            invoice.FinalizeInvoice();

            await _invoiceRepository.UpdateAsync(invoice);
            return invoice.ToDto();
        }
        public async Task<InvoiceDto> MarkAsPaidAsync(Guid id)
        {
            // ──── GET INVOICE ────
            var invoice = await _invoiceRepository.GetByIdAsync(id)
                ?? throw new InvoiceNotFoundException(id);
            invoice.MarkAsPaid();
            await _invoiceRepository.UpdateAsync(invoice);
            return invoice.ToDto();
        }
        public async Task<InvoiceDto> CancelAsync(Guid id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id)
                ?? throw new InvoiceNotFoundException(id);
            invoice.CancelInvoice();

            await _invoiceRepository.UpdateAsync(invoice);
            return invoice.ToDto();
        }

        // ════════════════════════════════════════════════════════════════════════════
        // SOFT DELETE OPERATIONS
        // ════════════════════════════════════════════════════════════════════════════
        public async Task DeleteAsync(Guid id)
        {
            // ──── GET INVOICE ────
            var invoice = await _invoiceRepository.GetByIdAsync(id)
                ?? throw new InvoiceNotFoundException(id);

            invoice.Delete();

            // ──── PERSIST ────
            await _invoiceRepository.UpdateAsync(invoice);
        }
        public async Task RestoreAsync(Guid id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id)
                ?? throw new InvoiceNotFoundException(id);
            // ──── RESTORE ────
            invoice.Restore();
            // ──── PERSIST ────
            await _invoiceRepository.UpdateAsync(invoice);
        }
    }
}