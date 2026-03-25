using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.RegularExpressions;

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
            Libelle = Regex.Replace(
                libelle.Trim().ToUpper(),
                @"\s+",
                "_"
            );
        }

        public void UpdateRole(string libelle)
        {
            Libelle = libelle;
        }

    }
}
