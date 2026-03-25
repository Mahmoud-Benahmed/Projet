using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Domain;
using ERP.AuthService.Infrastructure.Persistence.Repositories;
using ERP.AuthService.Properties;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;

namespace ERP.AuthService.Infrastructure.Persistence.Seeder
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
        ){
            if (await privilegeRepository.CountAsync() > 0)
                await privilegeRepository.DeleteAllAsync();

            // ── Build matrix dynamically ─────────────────────────
            var matrix = PrivilegeRegistry.All
                .SelectMany(p =>
                    roles.Keys.Select(role => (RoleName: role, PrivilegeCode: p.Code, IsGranted: RoleHasPrivilege(role, p.Code)))
                )
                .ToList();

            foreach (var (roleEnum, controleName, isGranted) in matrix)
            {
                if (!roles.TryGetValue(roleEnum, out var role))
                {
                    Console.WriteLine($"Role not found: {roleEnum}");
                    continue;
                }

                if (!controles.TryGetValue(controleName, out var controle))
                {
                    Console.WriteLine($"Controle not found: {controleName}");
                    continue;
                }

                var existing = await privilegeRepository
                    .GetByRoleIdAndControleIdAsync(role.Id, controle.Id);

                if (existing is null)
                {
                    var privilege = new Privilege(role.Id, controle.Id, isGranted);
                    await privilegeRepository.AddAsync(privilege);
                }
            }
        }

        // ── Helper to check if a role should have the privilege ──
        private static bool RoleHasPrivilege(string role, string privilegeCode)
        {
            // SystemAdmin gets everything
            if (role == Roles.SystemAdmin) return true;

            return role switch
            {
                Roles.SalesManager => privilegeCode.StartsWith("Clients") ||
                                     privilegeCode.StartsWith("Invoices") ||
                                     privilegeCode.StartsWith("Articles") ||
                                     privilegeCode.StartsWith("Stock") ||
                                     privilegeCode.StartsWith("Reports"),

                Roles.StockManager => privilegeCode.StartsWith("Articles") ||
                                      privilegeCode.StartsWith("Stock") ||
                                      privilegeCode.StartsWith("Reports"),

                Roles.Accountant => privilegeCode.StartsWith("Invoices") ||
                                    privilegeCode.StartsWith("Clients") ||
                                    privilegeCode.StartsWith("Payments") ||
                                    privilegeCode.StartsWith("Reports"),

                _ => false
            };
        }

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
                ("admin_erp1234",   "admin@erp.com",    "John DOE",         "Admin@1234",   adminRole.Id),
                ("sales_erp1234",   "sales@erp.com",    "Sales Alex",       "Sales@1234",   salesRole.Id),
                ("stock_erp1234",   "stock@erp.com",    "Stock David",      "Stock@1234",   stockRole.Id),
                ("account_erp1234", "account@erp.com",  "Accountant Jane",  "Account@1234", accountRole.Id),
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