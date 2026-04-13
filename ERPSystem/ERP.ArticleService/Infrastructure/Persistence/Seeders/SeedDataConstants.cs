namespace ERP.ArticleService.Infrastructure.Persistence.Seeders;

public static class SeedDataConstants
{
    public static class Categories
    {
        public static readonly (string Name, int TVA)[] All = new[]
        {
            ("Électronique", 19),
            ("Informatique", 19),
            ("Fournitures de bureau", 19),
            ("Mobilier", 19),
            ("Consommables", 19),
            ("Logiciels", 19),
            ("Réseaux & Télécommunications", 19),
            ("Outillage", 19),
            ("Alimentation", 7),
            ("Services", 19),
        };
    }

    public static class Articles
    {
        public static readonly (string Libelle, decimal Prix, string CategoryName, UnitEnum Unit, int TVA)[] All = new[]
        {
            // Électronique (TVA 19%)
            ("Écran 27 pouces Full HD",               1299.99m, "Électronique", UnitEnum.Piece, 19),
            ("Clavier mécanique sans fil",              349.99m, "Électronique", UnitEnum.Piece, 19),
            ("Souris ergonomique Bluetooth",            199.99m, "Électronique", UnitEnum.Piece, 19),

            // Informatique (TVA 19%)
            ("Laptop Core i7 16Go RAM",               5499.99m, "Informatique", UnitEnum.Piece, 19),
            ("SSD 1To NVMe",                           599.99m, "Informatique", UnitEnum.Piece, 19),
            ("Station d'accueil USB-C",                799.99m, "Informatique", UnitEnum.Piece, 19),

            // Fournitures de bureau (TVA 19%)
            ("Ramette papier A4 500 feuilles",          49.99m, "Fournitures de bureau", UnitEnum.Piece, 19),
            ("Stylos bille lot de 10",                  29.99m, "Fournitures de bureau", UnitEnum.Piece, 19),
            ("Classeur à levier A4",                    19.99m, "Fournitures de bureau", UnitEnum.Piece, 19),

            // Mobilier (TVA 19%)
            ("Bureau réglable en hauteur",            2999.99m, "Mobilier", UnitEnum.Piece, 19),
            ("Chaise ergonomique de bureau",          1899.99m, "Mobilier", UnitEnum.Piece, 19),
            ("Étagère modulable 5 niveaux",            699.99m, "Mobilier", UnitEnum.Piece, 19),

            // Consommables (TVA 19%)
            ("Cartouche d'encre noire HP",              89.99m, "Consommables", UnitEnum.Piece, 19),
            ("Toner laser Brother",                    149.99m, "Consommables", UnitEnum.Piece, 19),
            ("Papier photo brillant A4 x50",            59.99m, "Consommables", UnitEnum.Piece, 19),

            // Logiciels (TVA 19%)
            ("Licence Microsoft Office 2024",         1199.99m, "Logiciels", UnitEnum.Piece, 19),
            ("Antivirus Pro 1 an",                     199.99m, "Logiciels", UnitEnum.Piece, 19),
            ("Suite Adobe Creative Cloud",            2999.99m, "Logiciels", UnitEnum.Piece, 19),

            // Réseaux & Télécommunications (TVA 19%)
            ("Switch 24 ports Gigabit",               1499.99m, "Réseaux & Télécommunications", UnitEnum.Piece, 19),
            ("Routeur Wi-Fi 6 AX3000",                 899.99m, "Réseaux & Télécommunications", UnitEnum.Piece, 19),
            ("Câble RJ45 Cat6 10m",                     49.99m, "Réseaux & Télécommunications", UnitEnum.Meter, 19),
                
            // Outillage (TVA 19%)
            ("Tournevis électrique sans fil",          299.99m, "Outillage", UnitEnum.Piece, 19),
            ("Multimètre numérique",                   149.99m, "Outillage", UnitEnum.Piece, 19),
            ("Kit d'outils informatiques",              99.99m, "Outillage", UnitEnum.Piece, 19),

            // Food items (TVA 7% - reduced rate)
            ("Café en grains 1kg",                      89.99m, "Alimentation", UnitEnum.Kilogram, 7),
            ("Thé vert 500g",                           49.99m, "Alimentation", UnitEnum.Gram, 7),
            ("Eau minérale 1.5L x 6",                   19.99m, "Alimentation", UnitEnum.Liter, 7),

            // Services (TVA 19%)
            ("Heure de consulting IT",                 150.00m, "Services", UnitEnum.Hour,  19),
            ("Journée de formation",                   800.00m, "Services", UnitEnum.Day,   19),
        };
    }
}