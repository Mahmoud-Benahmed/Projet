using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ERP.AuthService.Domain
{
    public class Privilege
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; private set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid RoleId { get; private set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid ControleId { get; private set; }

        public bool IsGranted { get; private set; }

        private Privilege() { }

        public Privilege(Guid roleId, Guid controleId, bool isGranted)
        {
            Id = Guid.NewGuid();
            RoleId = roleId;
            ControleId = controleId;
            IsGranted = isGranted;
        }

        public void SetGranted(bool isGranted)
        {
            IsGranted = isGranted;
        }
    }
}