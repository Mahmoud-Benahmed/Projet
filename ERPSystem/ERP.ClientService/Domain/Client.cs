namespace ERP.ClientService.Domain
{
    public class Client
    {
        public Guid Id { get; set; }
        public ClientType Type { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string? Phone { get; set; }
        public string? TaxNumber { get; set; }
        public bool IsDeleted { get; private set; } = false;
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private Client() { }
        public Client(ClientType type, string name, string email, string address, string? phone, string? taxNumber)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address is required");

            Id = Guid.NewGuid();
            Type = type;
            Name = name;
            Email = email;
            Address = address;
            Phone = phone;
            TaxNumber = taxNumber;
            CreatedAt = DateTime.UtcNow;
        }

        public void Update(ClientType type, string name, string email, string address, string? phone, string? taxNumber)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");
            
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address is required");

            Type = type;
            Name = name;
            Email = email;
            Address = address;
            Phone = phone;
            TaxNumber = taxNumber;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Delete()
        {
            if (IsDeleted) return;
            IsDeleted = true;
        }

        public void Restore()
        {
            if (!IsDeleted) return;
            IsDeleted = false;
        }
    }
}
