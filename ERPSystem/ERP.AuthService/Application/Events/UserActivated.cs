using ERP.AuthService.Domain;

namespace ERP.AuthService.Application.Events
{
    public record UserActivated(string AuthUserId);
}
