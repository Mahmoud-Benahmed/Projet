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
        public RoleEnum Libelle { get; private set; }

        private Role() { }

        public Role(RoleEnum libelle)
        {
            Id = Guid.NewGuid();
            Libelle = libelle;
        }

    }
}
