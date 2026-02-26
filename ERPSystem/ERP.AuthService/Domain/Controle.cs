using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ERP.AuthService.Domain
{
    public class Controle
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; private set; }

        public string Category { get; private set; } = default!;
        public string Libelle { get; private set; } = default!;
        public string Description { get; private set; } = default!;

        private Controle() { }

        public Controle(string category, string libelle, string description)
        {
            Id = Guid.NewGuid();
            Category = category;
            Libelle = libelle;
            Description = description;
        }
    }
}
