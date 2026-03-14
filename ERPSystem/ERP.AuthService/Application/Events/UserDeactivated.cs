using ERP.AuthService.Domain;

namespace ERP.AuthService.Application.Events
{
    public record UserDeactivated(string AuthUserId);
}
