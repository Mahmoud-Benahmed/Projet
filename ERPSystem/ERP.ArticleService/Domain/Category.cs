namespace ERP.ArticleService.Domain
{
    public class Category
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private Category() { }

        public Category(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name is required");

            Id = Guid.NewGuid();
            Name = name.Trim();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name is required");

            if (Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase))
                return;

            Name = name.Trim();
            UpdatedAt = DateTime.UtcNow;
        }
    }
}