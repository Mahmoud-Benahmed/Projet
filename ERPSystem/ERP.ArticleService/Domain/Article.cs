namespace ERP.ArticleService.Domain
{
    public class Article
    {
        public Guid Id { get; private set; }

        public Guid CategoryId { get; private set; }  
        public Category Category { get; private set; }
        
        public string Code { get; init; }

        public string Libelle { get; private set; }
        public decimal Prix { get; private set; }

        public bool IsActive { get; private set; } = true;
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }


        private Article() { }

        public Article(string code, string libelle, decimal prix, Category category)
        {
            if (string.IsNullOrWhiteSpace(libelle))
                throw new ArgumentException("Libelle is required");

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code is required");

            if (prix <= 0)
                throw new ArgumentException("Prix must be positive");

            Id = Guid.NewGuid();
            Code = code;
            Libelle = libelle.Trim(); ;
            Prix = Math.Round(prix, 2);
            Category = category ?? throw new ArgumentException("Category is required");
            CategoryId = category.Id;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Update(string libelle, decimal prix, Category category)
        {
            if (string.IsNullOrEmpty(libelle))
                throw new ArgumentException("Libelle is required");

            if (prix <= 0)
                throw new ArgumentException("Prix must be positive");

            var hasChanged = !string.Equals(libelle, Libelle, StringComparison.OrdinalIgnoreCase) 
                            || prix != Prix
                            || category.Id != CategoryId;

            if (!hasChanged) return;

            Libelle = libelle.Trim();
            Prix = Math.Round(prix, 2);
            Category = category;
            CategoryId = category.Id;

            UpdatedAt = DateTime.UtcNow;
        }


        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;

        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            
        }
    }
}