using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Domain;
using ERP.AuthService.Infrastructure.Persistence.Seeder;
using ERP.AuthService.Properties;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;

namespace ERPrivileges.AuthService.Infrastructure.Persistence.Seeder
{
    public static class AuthServiceSeeder
    {
        public static async Task SeedAsync(
            IAuditLogRepository auditLogRepository,
            IAuthUserRepository userRepository,
            IRoleRepository roleRepository,
            IControleRepository controleRepository,
            IPrivilegeRepository privilegeRepository,
            IPasswordHasher<AuthUser> passwordHasher, // ← moved before configuration
            IConfiguration configuration)
        {
            await controleRepository.DeleteAllAsync();
            await roleRepository.DeleteAllAsync();
            await privilegeRepository.DeleteAllAsync();
            await userRepository.DeleteAllAsync();
            await auditLogRepository.ClearAsync();

            var controles = await SeedControlesAsync(controleRepository);
            var roles = await SeedRolesAsync(roleRepository);
            await SeedPrivilegesAsync(privilegeRepository, roles, controles);
            await SeedUsersAsync(userRepository, roleRepository, configuration, passwordHasher);
        }

        // ── 1. SEED CONTROLES ─────────────────────────────
        private static async Task<Dictionary<string, Controle>> SeedControlesAsync(
            IControleRepository controleRepository
        )
        {
            await controleRepository.DeleteAllAsync();

            var result = new Dictionary<string, Controle>();

            foreach (var def in PrivilegeRegistry.All)
            {
                var controle = new Controle(
                    def.Category,
                    def.Code,
                    def.Description
                );

                await controleRepository.AddAsync(controle);
                result[def.Code] = controle;
            }

            return result;
        }


        // ── 2. SEED ROLES ─────────────────────────────────
        private static async Task<Dictionary<string, Role>> SeedRolesAsync(
            IRoleRepository roleRepository)
        {
            if (await roleRepository.CountAsync() > 0)
            {
                await roleRepository.DeleteAllAsync();
            }

            var result = new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase);
            string[] roleNames = [Roles.SystemAdmin, Roles.Accountant, Roles.SalesManager, Roles.StockManager];

            foreach (string roleName in roleNames)
            {
                try
                {
                    var role = new Role(roleName);
                    await roleRepository.AddAsync(role);
                    result[roleName] = role;
                }
                catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
                {
                    // Already exists → fetch it instead
                    var existing = await roleRepository.GetByLibelleAsync(roleName.ToUpper());
                    result[roleName] = existing!;
                }
            }

            return result;
        }



        // ── 3. SEED PRIVILEGES ────────────────────────────
        private static async Task SeedPrivilegesAsync(
            IPrivilegeRepository privilegeRepository,
            Dictionary<string, Role> roles,
            Dictionary<string, Controle> controles
        )
        {
            if (await privilegeRepository.CountAsync() > 0)
                await privilegeRepository.DeleteAllAsync();

            foreach (var rolePair in roles)
            {
                var roleName = rolePair.Key;
                var role = rolePair.Value;

                foreach (var def in PrivilegeRegistry.All)
                {
                    if (!controles.TryGetValue(def.Code, out var controle))
                    {
                        Console.WriteLine($"Controle not found: {def.Code}");
                        continue;
                    }

                    bool isGranted = RoleHasPrivilege(roleName, def.Category, def.Code);

                    var existing = await privilegeRepository
                        .GetByRoleIdAndControleIdAsync(role.Id, controle.Id);

                    if (existing is null)
                    {
                        var privilege = new Privilege(role.Id, controle.Id, isGranted);
                        await privilegeRepository.AddAsync(privilege);
                    }
                }
            }
        }

        private static bool RoleHasPrivilege(string role, string category, string privilegeCode)
        {
            if (role == Roles.SystemAdmin) return true;

            return role switch
            {
                Roles.SalesManager => SalesManagerHas(privilegeCode),
                Roles.StockManager => StockManagerHas(privilegeCode),
                Roles.Accountant => AccountantHas(privilegeCode),
                _ => false
            };
        }

        private static bool SalesManagerHas(string code) => code switch
        {
            // Clients — full
            Privileges.Clients.MANAGE_CLIENTS => true,
            Privileges.Clients.VIEW_CLIENTS => true,
            Privileges.Clients.CREATE_CLIENT => true,
            Privileges.Clients.UPDATE_CLIENT => true,
            Privileges.Clients.DELETE_CLIENT => true,
            Privileges.Clients.RESTORE_CLIENT => true,
            Privileges.Clients.CREATE_CLIENT_CATEGORIES => true,
            Privileges.Clients.UPDATE_CLIENT_CATEGORIES => true,
            Privileges.Clients.DELETE_CLIENT_CATEGORIES => true,
            Privileges.Clients.RESTORE_CLIENT_CATEGORIES => true,

            // Articles — create/update/view only (no delete/restore/categories)
            Privileges.Articles.MANAGE_ARTICLES => true,
            Privileges.Articles.VIEW_ARTICLES => true,
            Privileges.Articles.CREATE_ARTICLE => true,
            Privileges.Articles.UPDATE_ARTICLE => true,

            // Invoices — create/view only
            Privileges.Invoices.MANAGE_INVOICES => true,
            Privileges.Invoices.VIEW_INVOICES => true,
            Privileges.Invoices.CREATE_INVOICE => true,

            // Payments — view only
            Privileges.Payments.VIEW_PAYMENTS => true,

            // Stock — read only
            Privileges.Stock.VIEW_STOCK => true,

            // Reports — view + export
            Privileges.Reports.VIEW_REPORTS => true,
            Privileges.Reports.EXPORT_REPORTS => true,

            _ => false
        };

        private static bool StockManagerHas(string code) => code switch
        {
            // Articles — full
            Privileges.Articles.MANAGE_ARTICLES => true,
            Privileges.Articles.VIEW_ARTICLES => true,
            Privileges.Articles.CREATE_ARTICLE => true,
            Privileges.Articles.UPDATE_ARTICLE => true,
            Privileges.Articles.DELETE_ARTICLE => true,
            Privileges.Articles.RESTORE_ARTICLE => true,
            Privileges.Articles.CREATE_ARTICLE_CATEGORIES => true,
            Privileges.Articles.UPDATE_ARTICLE_CATEGORIES => true,
            Privileges.Articles.DELETE_ARTICLE_CATEGORIES => true,
            Privileges.Articles.RESTORE_ARTICLE_CATEGORIES => true,

            // Stock — full
            Privileges.Stock.MANAGE_STOCK => true,
            Privileges.Stock.VIEW_STOCK => true,
            Privileges.Stock.UPDATE_STOCK => true,
            Privileges.Stock.ADD_ENTRY => true,

            // Reports — view only
            Privileges.Reports.VIEW_REPORTS => true,

            _ => false
        };

        private static bool AccountantHas(string code) => code switch
        {
            // Audit
            Privileges.Audit.MANAGE_AUDITLOGS => true,

            // Clients — view only
            Privileges.Clients.VIEW_CLIENTS => true,

            // Invoices — view + validate only
            Privileges.Invoices.MANAGE_INVOICES => true,
            Privileges.Invoices.VIEW_INVOICES => true,
            Privileges.Invoices.VALIDATE_INVOICE => true,

            // Payments — full
            Privileges.Payments.MANAGE_PAYMENTS => true,
            Privileges.Payments.VIEW_PAYMENTS => true,
            Privileges.Payments.RECORD_PAYMENT => true,

            // Reports — full
            Privileges.Reports.MANAGE_REPORTS => true,
            Privileges.Reports.VIEW_REPORTS => true,
            Privileges.Reports.EXPORT_REPORTS => true,

            _ => false
        };


        // ── 4. SEED USERS ─────────────────────────────────
        private static async Task SeedUsersAsync(
            IAuthUserRepository userRepository,
            IRoleRepository roleRepository,
            IConfiguration configuration,
            IPasswordHasher<AuthUser> passwordHasher)
        {
            if (await userRepository.CountAsync() > 0)
            {
                await userRepository.DeleteAllAsync();
            }
            List<Role> roles = await roleRepository.GetAllAsync();

            var adminRole = roles.Find(r => r.Libelle == Roles.SystemAdmin)  ?? throw new InvalidOperationException($"Role '{Roles.SystemAdmin}' not found. Ensure roles are seeded before users.");
            var salesRole = roles.Find(r => r.Libelle == Roles.SalesManager) ?? throw new InvalidOperationException($"Role '{Roles.SalesManager}' not found.");
            var stockRole = roles.Find(r => r.Libelle == Roles.StockManager) ?? throw new InvalidOperationException($"Role '{Roles.StockManager}' not found.");
            var accountRole = roles.Find(r => r.Libelle == Roles.Accountant) ?? throw new InvalidOperationException($"Role '{Roles.Accountant}' not found.");


            var seedUsers = new List<(string Login, string Email, string FullName, string Password, Guid roleId)>
            {
                ("admin_erp1234",   "admin@erPrivileges.com",    "John DOE",         "Admin@1234",   adminRole.Id),
                ("sales_erp1234",   "sales@erPrivileges.com",    "Sales Alex",       "Sales@1234",   salesRole.Id),
                ("stock_erp1234",   "stock@erPrivileges.com",    "Stock David",      "Stock@1234",   stockRole.Id),
                ("account_erp1234", "account@erPrivileges.com",  "Accountant Jane",  "Account@1234", accountRole.Id),
            };

            foreach (var (login, email, fullName, password, roleId) in seedUsers)
            {
                if (await userRepository.ExistsByEmailAsync(email))
                    continue;

                var user = new AuthUser(login, email, fullName, roleId);


                var hashedPassword = passwordHasher.HashPassword(user, password);
                user.SetPasswordHash(hashedPassword);

                await userRepository.AddAsync(user);
            }
        }
    }
}