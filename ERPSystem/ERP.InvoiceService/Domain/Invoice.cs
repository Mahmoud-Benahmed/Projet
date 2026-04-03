using System;
using System.Collections.Generic;
using System.Linq;

namespace InvoiceService.Domain
{
    public class Invoice
    {
        // ────────────────────────────────────────────────────────────────────────
        // PROPERTIES
        // ────────────────────────────────────────────────────────────────────────

        public Guid Id { get; private set; }
        public string InvoiceNumber { get; private set; }
        public DateTime InvoiceDate { get; private set; }
        public DateTime DueDate { get; private set; }
        public decimal TotalHT { get; private set; }
        public decimal TotalTVA { get; private set; }
        public decimal TotalTTC { get; private set; }
        public InvoiceStatus Status { get; private set; }
        public Guid ClientId { get; private set; }
        public string ClientFullName { get; private set; }
        public string ClientAddress { get; private set; }
        public string? AdditionalNotes { get; private set; }
        public bool IsDeleted { get; private set; } = false;
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        private readonly List<InvoiceItem> _items = new();
        public IReadOnlyCollection<InvoiceItem> Items => _items.AsReadOnly();

        // ────────────────────────────────────────────────────────────────────────
        // CONSTRUCTORS
        // ────────────────────────────────────────────────────────────────────────

        private Invoice() { }

        public Invoice(
            string invoiceNumber,
            DateTime invoiceDate,
            DateTime dueDate,
            Guid clientId,
            string clientFullName,
            string clientAddress,
            string? additionalNotes = null)
        {
            Id = Guid.NewGuid();
            InvoiceNumber = invoiceNumber;
            InvoiceDate = invoiceDate;
            DueDate = dueDate;
            ClientId = clientId;
            ClientFullName = clientFullName;
            ClientAddress = clientAddress;
            AdditionalNotes = additionalNotes;
            Status = InvoiceStatus.DRAFT;
            IsDeleted = false;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        // ────────────────────────────────────────────────────────────────────────
        // BUSINESS METHODS
        // ────────────────────────────────────────────────────────────────────────

        /// <param name="item">The invoice item to add</param>
        /// <exception cref="InvoiceDomainException">Thrown if invoice is not in DRAFT status</exception>
        public void AddItem(InvoiceItem item)
        {
            if (Status != InvoiceStatus.DRAFT)
                throw new InvoiceDomainException("Items can only be added to DRAFT invoices.");

            _items.Add(item);
            CalculateTotals();
            UpdatedAt = DateTime.UtcNow;
        }
        /// <param name="itemId">ID of the item to remove</param>
        /// <exception cref="InvoiceDomainException">Thrown if invoice is not in DRAFT status or item not found</exception>
        public void RemoveItem(Guid itemId)
        {
            if (Status != InvoiceStatus.DRAFT)
                throw new InvoiceDomainException("Items can only be removed from DRAFT invoices.");

            var item = _items.FirstOrDefault(i => i.Id == itemId)
                ?? throw new InvoiceDomainException($"Item with id '{itemId}' not found.");

            _items.Remove(item);
            CalculateTotals();
            UpdatedAt = DateTime.UtcNow;
        }

        public void CalculateTotals()
        {
            // Calculate subtotal for each item
            foreach (var item in _items)
                item.CalculateSubtotal();

            // Sum up totals
            TotalHT = _items.Sum(i => i.TotalHT);
            TotalTVA = _items.Sum(i => i.TotalTTC - i.TotalHT);
            TotalTTC = _items.Sum(i => i.TotalTTC);
            UpdatedAt = DateTime.UtcNow;
        }
        /// <exception cref="InvoiceDomainException">Thrown if not in DRAFT status or has no items</exception>
        public void FinalizeInvoice()
        {
            if (Status != InvoiceStatus.DRAFT)
                throw new InvoiceDomainException("Only DRAFT invoices can be finalized.");

            if (!_items.Any())
                throw new InvoiceDomainException("Cannot finalize an invoice with no items.");

            CalculateTotals();
            Status = InvoiceStatus.UNPAID;
            UpdatedAt = DateTime.UtcNow;
        }
        /// <exception cref="InvoiceDomainException">Thrown if not in UNPAID status</exception>
        public void MarkAsPaid()
        {
            if (Status != InvoiceStatus.UNPAID)
                throw new InvoiceDomainException("Only UNPAID invoices can be marked as paid.");

            Status = InvoiceStatus.PAID;
            UpdatedAt = DateTime.UtcNow;
        }
        /// <exception cref="InvoiceDomainException">Thrown if PAID or already CANCELLED</exception>
        public void CancelInvoice()
        {
            if (Status == InvoiceStatus.PAID)
                throw new InvoiceDomainException("PAID invoices cannot be cancelled.");

            if (Status == InvoiceStatus.CANCELLED)
                throw new InvoiceDomainException("Invoice is already cancelled.");

            Status = InvoiceStatus.CANCELLED;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <exception cref="InvoiceDomainException">Thrown if already deleted</exception>
        public void Delete()
        {
            if (IsDeleted)
                throw new InvoiceDomainException("Invoice is already deleted.");

            IsDeleted = true;
            UpdatedAt = DateTime.UtcNow;
        }
        /// <exception cref="InvoiceDomainException">Thrown if not deleted</exception>
        public void Restore()
        {
            if (!IsDeleted)
                throw new InvoiceDomainException("Invoice is not deleted.");

            IsDeleted = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}