namespace ERP.ArticleService.Domain
{
    public class Article
    {
        public Guid Id { get; private set; }

        public Guid CategoryId { get; private set; }  
        public Category Category { get; private set; }
        
        public string CodeRef { get; init; }
        public string BarCode { get; private set; }

        public string Libelle { get; private set; }
        public decimal Prix { get; private set; }
        public decimal TVA { get; private set; }

        public bool IsDeleted { get; private set; } = false;
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }


        private Article() { }

        public Article(string code, string libelle, decimal prix, Category category, string barCode, decimal? tva)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code is required");

            if (string.IsNullOrWhiteSpace(libelle))
                throw new ArgumentException("Libelle is required");

            if (prix <= 0)
                throw new ArgumentException("Prix must be positive");

            var resolvedTVA = tva ?? category.TVA;
            if (resolvedTVA <= 0)
                throw new ArgumentException("TVA must be greater than zero.");

            Id = Guid.NewGuid();
            CodeRef = code;
            Libelle = libelle.Trim(); ;
            Prix = Math.Round(prix, 2);
            Category = category ?? throw new ArgumentException("Category is required");
            CategoryId = category.Id;
            BarCode= barCode;
            TVA = resolvedTVA;
            CreatedAt = DateTime.UtcNow;
        }

        public void Update(string libelle, decimal prix, Category category, string barCode, decimal? tva)
        {
            if (string.IsNullOrWhiteSpace(libelle))
                throw new ArgumentException("Libelle is required");

            if (string.IsNullOrWhiteSpace(barCode))
                throw new ArgumentException("Code is required");

            if (prix <= 0)
                throw new ArgumentException("Prix must be positive");

            var resolvedTVA = tva ?? category.TVA;
            if (resolvedTVA <= 0)
                throw new ArgumentException("TVA must be greater than zero.");

            var hasChanged = !string.Equals(libelle, Libelle, StringComparison.OrdinalIgnoreCase)
                            || prix != Prix
                            || category.Id != CategoryId
                            || !string.Equals(barCode, BarCode, StringComparison.OrdinalIgnoreCase)
                            || resolvedTVA != TVA;

            if (!hasChanged) return;

            Libelle = libelle.Trim();
            Prix = Math.Round(prix, 2);
            Category = category;
            CategoryId = category.Id;
            BarCode = barCode;
            TVA = resolvedTVA;
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