using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Domain;

namespace ERP.AuthService.Infrastructure.Persistence
{
    public static class AuthServiceSeeder
    {
        public static async Task SeedAsync(
            IAuthUserRepository userRepository,
            IRoleRepository roleRepository,
            IControleRepository controleRepository,
            IPrivilegeRepository privilegeRepository,
            IAuthUserService authUserService,
            IConfiguration configuration,
            IEventPublisher eventPublisher)
        {
            var controles = await SeedControlesAsync(controleRepository);
            var roles = await SeedRolesAsync(roleRepository);
            await SeedPrivilegesAsync(privilegeRepository, roles, controles);
            await SeedUsersAsync(userRepository, roleRepository, authUserService, configuration, eventPublisher);
        }

        // ── 1. SEED CONTROLES ─────────────────────────────
        private static async Task<Dictionary<string, Controle>> SeedControlesAsync(
            IControleRepository controleRepository)
        {
            if (await controleRepository.CountAsync() > 0)
                await controleRepository.DeleteAllAsync();

            var controles = new List<(string Category, string Libelle, string Description)>
            {
                // Auth
                ("Auth", "ManageUsers",     "Create, update, deactivate users"),
                ("Auth", "AssignRoles",     "Assign roles and manage privileges"),

                // Clients
                ("Clients", "ViewClients",   "View client list and details"),
                ("Clients", "CreateClient",  "Create a new client"),
                ("Clients", "UpdateClient",    "Edit an existing client"),
                ("Clients", "DeleteClient",  "Delete a client"),

                // Articles
                ("Articles", "ViewArticles",  "View article list and details"),
                ("Articles", "CreateArticle", "Create a new article"),
                ("Articles", "UpdateArticle",   "Edit an existing article"),
                ("Articles", "DeleteArticle", "Delete an article"),

                // Facturation
                ("Facturation", "ViewInvoices",    "View invoice list and details"),
                ("Facturation", "CreateInvoice",   "Create a new invoice"),
                ("Facturation", "ValidateInvoice", "Validate an invoice"),
                ("Facturation", "DeleteInvoice",   "Delete an invoice"),

                // Paiements
                ("Paiements", "ViewPayments",   "View payment list and details"),
                ("Paiements", "RecordPayment",  "Record a new payment"),
                ("Paiements", "DeletePayment",  "Delete a payment"),

                // Stocks
                ("Stocks", "ViewStock",    "View stock levels"),
                ("Stocks", "UpdateStock",  "Update stock quantities"),
                ("Stocks", "AddEntry",     "Add a stock entry"),

                // Reporting
                ("Reporting", "ViewReports",   "View reports"),
                ("Reporting", "ExportReports", "Export reports"),
            };

            // Use case-insensitive dictionary
            var result = new Dictionary<string, Controle>();

            foreach (var (category, libelle, description) in controles)
            {
                var existing = await controleRepository.GetByLibelleAsync(libelle);
                if (existing is null)
                {
                    var controle = new Controle(category, libelle, description);
                    await controleRepository.AddAsync(controle);
                    result[libelle] = controle;
                }
                else
                {
                    result[libelle] = existing;  // ← make sure this is present
                }
            }

            return result;
        }



        // ── 2. SEED ROLES ─────────────────────────────────
        private static async Task<Dictionary<RoleEnum, Role>> SeedRolesAsync(
            IRoleRepository roleRepository)
        {
            if (await roleRepository.CountAsync() > 0)
                await roleRepository.DeleteAllAsync();


            var result = new Dictionary<RoleEnum, Role>();

            foreach (RoleEnum roleEnum in Enum.GetValues<RoleEnum>())
            {
                var existing = await roleRepository.GetByLibelleAsync(roleEnum);
                if (existing is null)
                {
                    var role = new Role(roleEnum);
                    await roleRepository.AddAsync(role);
                    result[roleEnum] = role;
                }
                else
                {
                    result[roleEnum] = existing;
                }
            }

            return result;
        }



        // ── 3. SEED PRIVILEGES ────────────────────────────
        private static async Task SeedPrivilegesAsync(
            IPrivilegeRepository privilegeRepository,
            Dictionary<RoleEnum, Role> roles,
            Dictionary<string, Controle> controles)
        {
            if (await privilegeRepository.CountAsync() > 0)
                await privilegeRepository.DeleteAllAsync();

            // Format: (RoleEnum, ControleName, IsGranted)
            var matrix = new List<(RoleEnum Role, string Controle, bool IsGranted)>
            {
                // ── SystemAdmin — full access ──────────────
                (RoleEnum.SystemAdmin, "ManageUsers",     true),
                (RoleEnum.SystemAdmin, "AssignRoles",     true),
                (RoleEnum.SystemAdmin, "ViewClients",     true),
                (RoleEnum.SystemAdmin, "CreateClient",    true),
                (RoleEnum.SystemAdmin, "UpdateClient",    true),
                (RoleEnum.SystemAdmin, "DeleteClient",    true),
                (RoleEnum.SystemAdmin, "ViewArticles",    true),
                (RoleEnum.SystemAdmin, "CreateArticle",   true),
                (RoleEnum.SystemAdmin, "UpdateArticle",     true),
                (RoleEnum.SystemAdmin, "DeleteArticle",   true),
                (RoleEnum.SystemAdmin, "ViewInvoices",    true),
                (RoleEnum.SystemAdmin, "CreateInvoice",   true),
                (RoleEnum.SystemAdmin, "ValidateInvoice", true),
                (RoleEnum.SystemAdmin, "DeleteInvoice",   true),
                (RoleEnum.SystemAdmin, "ViewPayments",    true),
                (RoleEnum.SystemAdmin, "RecordPayment",   true),
                (RoleEnum.SystemAdmin, "DeletePayment",   true),
                (RoleEnum.SystemAdmin, "ViewStock",       true),
                (RoleEnum.SystemAdmin, "UpdateStock",     true),
                (RoleEnum.SystemAdmin, "AddEntry",        true),
                (RoleEnum.SystemAdmin, "ViewReports",     true),
                (RoleEnum.SystemAdmin, "ExportReports",   true),

                // ── SalesManager ───────────────────────────
                (RoleEnum.SalesManager, "ManageUsers",     false),
                (RoleEnum.SalesManager, "AssignRoles",     false),
                (RoleEnum.SalesManager, "ViewClients",     true),
                (RoleEnum.SalesManager, "CreateClient",    true),
                (RoleEnum.SalesManager, "UpdateClient",      true),
                (RoleEnum.SalesManager, "DeleteClient",    false),
                (RoleEnum.SalesManager, "ViewArticles",    true),
                (RoleEnum.SalesManager, "CreateArticle",   false),
                (RoleEnum.SalesManager, "UpdateArticle",     false),
                (RoleEnum.SalesManager, "DeleteArticle",   false),
                (RoleEnum.SalesManager, "ViewInvoices",    true),
                (RoleEnum.SalesManager, "CreateInvoice",   true),
                (RoleEnum.SalesManager, "ValidateInvoice", false),
                (RoleEnum.SalesManager, "DeleteInvoice",   false),
                (RoleEnum.SalesManager, "ViewPayments",    false),
                (RoleEnum.SalesManager, "RecordPayment",   false),
                (RoleEnum.SalesManager, "DeletePayment",   false),
                (RoleEnum.SalesManager, "ViewStock",       true),
                (RoleEnum.SalesManager, "UpdateStock",     false),
                (RoleEnum.SalesManager, "AddEntry",        false),
                (RoleEnum.SalesManager, "ViewReports",     true),
                (RoleEnum.SalesManager, "ExportReports",   false),

                // ── StockManager ───────────────────────────
                (RoleEnum.StockManager, "ManageUsers",     false),
                (RoleEnum.StockManager, "AssignRoles",     false),
                (RoleEnum.StockManager, "ViewClients",     false),
                (RoleEnum.StockManager, "CreateClient",    false),
                (RoleEnum.StockManager, "UpdateClient",      false),
                (RoleEnum.StockManager, "DeleteClient",    false),
                (RoleEnum.StockManager, "ViewArticles",    true),
                (RoleEnum.StockManager, "CreateArticle",   true),
                (RoleEnum.StockManager, "UpdateArticle",     true),
                (RoleEnum.StockManager, "DeleteArticle",   false),
                (RoleEnum.StockManager, "ViewInvoices",    false),
                (RoleEnum.StockManager, "CreateInvoice",   false),
                (RoleEnum.StockManager, "ValidateInvoice", false),
                (RoleEnum.StockManager, "DeleteInvoice",   false),
                (RoleEnum.StockManager, "ViewPayments",    false),
                (RoleEnum.StockManager, "RecordPayment",   false),
                (RoleEnum.StockManager, "DeletePayment",   false),
                (RoleEnum.StockManager, "ViewStock",       true),
                (RoleEnum.StockManager, "UpdateStock",     true),
                (RoleEnum.StockManager, "AddEntry",        true),
                (RoleEnum.StockManager, "ViewReports",     true),
                (RoleEnum.StockManager, "ExportReports",   false),

                // ── Accountant ─────────────────────────────
                (RoleEnum.Accountant, "ManageUsers",     false),
                (RoleEnum.Accountant, "AssignRoles",     false),
                (RoleEnum.Accountant, "ViewClients",     true),
                (RoleEnum.Accountant, "CreateClient",    false),
                (RoleEnum.Accountant, "UpdateClient",      false),
                (RoleEnum.Accountant, "DeleteClient",    false),
                (RoleEnum.Accountant, "ViewArticles",    false),
                (RoleEnum.Accountant, "CreateArticle",   false),
                (RoleEnum.Accountant, "UpdateArticle",     false),
                (RoleEnum.Accountant, "DeleteArticle",   false),
                (RoleEnum.Accountant, "ViewInvoices",    true),
                (RoleEnum.Accountant, "CreateInvoice",   false),
                (RoleEnum.Accountant, "ValidateInvoice", true),
                (RoleEnum.Accountant, "DeleteInvoice",   false),
                (RoleEnum.Accountant, "ViewPayments",    true),
                (RoleEnum.Accountant, "RecordPayment",   true),
                (RoleEnum.Accountant, "DeletePayment",   false),
                (RoleEnum.Accountant, "ViewStock",       false),
                (RoleEnum.Accountant, "UpdateStock",     false),
                (RoleEnum.Accountant, "AddEntry",        false),
                (RoleEnum.Accountant, "ViewReports",     true),
                (RoleEnum.Accountant, "ExportReports",   true),
            };

            foreach (var (roleEnum, controleName, isGranted) in matrix)
            {
                if (!roles.ContainsKey(roleEnum))
                {
                    Console.WriteLine($"Role not found: {roleEnum}");
                    continue;
                }

                if (!controles.ContainsKey(controleName))
                {
                    Console.WriteLine($"Controle not found: {controleName}");
                    continue;
                }

                var role = roles[roleEnum];
                var controle = controles[controleName];

                var existing = await privilegeRepository
                    .GetByRoleIdAndControleIdAsync(role.Id, controle.Id);

                if (existing is null)
                {
                    var privilege = new Privilege(role.Id, controle.Id, isGranted);
                    await privilegeRepository.AddAsync(privilege);
                }
            }
        }

        // ── 4. SEED USERS ─────────────────────────────────
        private static async Task SeedUsersAsync(
            IAuthUserRepository userRepository,
            IRoleRepository roleRepository,
            IAuthUserService authUserService,
            IConfiguration configuration,
            IEventPublisher eventPublisher)
        {
            if (await userRepository.CountAsync() > 0)
                await userRepository.DeleteAllAsync();

            var seedUsers = new List<(string Login, string Email, string Password, RoleEnum Role)>
            {
                (
                configuration["SeedUser:Login"]    ?? "admin_erp1234",
                    configuration["SeedUser:Email"]    ?? "admin@erp.com",
                    configuration["SeedUser:Password"] ?? "Admin@1234",
                    Enum.Parse<RoleEnum>(configuration["SeedUser:Role"] ?? "SystemAdmin")
                ),
                ("sales_erp1234","sales@erp.com",   "Sales@1234",   RoleEnum.SalesManager),
                ("stock_erp1234","stock@erp.com",   "Stock@1234",   RoleEnum.StockManager),
                ("account_erp1234","account@erp.com", "Account@1234", RoleEnum.Accountant),
            };

            foreach (var (login, email, password, roleEnum) in seedUsers)
            {
                if (await userRepository.ExistsByEmailAsync(email))
                    continue;

                var role = await roleRepository.GetByLibelleAsync(roleEnum)
                           ?? throw new InvalidOperationException(
                               $"Role {roleEnum} not found.");

                var user = await authUserService.RegisterAsync(new RegisterRequestDto(
                     Login: login,
                     Email: email,
                     Password: password,
                     RoleId: role.Id
                 ));
            }
        }
    }
}