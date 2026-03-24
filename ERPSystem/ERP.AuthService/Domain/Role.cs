using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ERP.AuthService.Domain
{
    public class Role
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; private set; }

        [BsonRepresentation(BsonType.String)]
        public string Libelle { get; private set; }

        private Role() { }

        public Role(string libelle)
        {
            Id = Guid.NewGuid();
            Libelle = libelle.Trim().ToUpper();
        }

        public void UpdateRole(string libelle)
        {
            Libelle = libelle;
        }

    }
}
