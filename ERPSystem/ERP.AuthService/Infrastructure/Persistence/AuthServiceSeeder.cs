using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Domain;
using ERP.AuthService.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;

namespace ERP.AuthService.Infrastructure.Persistence
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
            IControleRepository controleRepository)
        {
            if (await controleRepository.CountAsync() > 0)
            {
                await controleRepository.DeleteAllAsync();
            }
            var controles = new List<(string Category, string Libelle, string Description)>
            {
                // Auth
                ("AUTH", "ViewUsers",       "View users"),
                ("AUTH", "CreateUser",      "Register/Create users"),
                ("AUTH", "UpdateUser",      "Update/Modify users"),
                ("AUTH", "DeleteUser",      "Delete/Remove users"),
                ("AUTH", "RestoreUser",     "Restore deleted users"),
                ("AUTH", "ActivateUser",    "Activate users to allow access"),
                ("AUTH", "DeactivateUser",  "Deactivate users to deny access"),
                ("AUTH", "AssignRoles",     "Assign roles and manage privileges"),
                ("AUTH", "ManageAuditLogs", "View, clear Authentication-related audit logs"),

                // Client
                ("CLIENTS", "ViewClients",   "View client list and details"),
                ("CLIENTS", "CreateClient",  "Create a new client"),
                ("CLIENTS", "UpdateClient",  "Edit an existing client"),
                ("CLIENTS", "DeleteClient",  "Delete a client"),
                ("CLIENTS", "RestoreClient",  "Restore deleted client"),

                // Article
                ("ARTICLES", "ViewArticles",  "View article list and details"),
                ("ARTICLES", "CreateArticle", "Create a new article"),
                ("ARTICLES", "UpdateArticle",   "Edit an existing article"),
                ("ARTICLES", "DeleteArticle", "Delete an article"),
                ("ARTICLES", "RestoreArticle", "Restore deleted article"),

                // Facturation
                ("FACTURATION", "ViewInvoices",    "View invoice list and details"),
                ("FACTURATION", "CreateInvoice",   "Create a new invoice"),
                ("FACTURATION", "ValidateInvoice", "Validate an invoice"),
                ("FACTURATION", "DeleteInvoice",   "Delete an invoice"),
                ("FACTURATION", "RestoreInvoice",   "Restore deleted invoice"),

                // Paiement
                ("PAIEMENT", "ViewPayments",   "View payment list and details"),
                ("PAIEMENT", "RecordPayment",  "Record a new payment"),
                ("PAIEMENT", "DeletePayment",  "Delete a payment"),
                ("PAIEMENT", "RestorePayment",  "Restore deleted payment"),

                // Stock
                ("STOCK", "ViewStock",    "View stock levels"),
                ("STOCK", "UpdateStock",  "Update stock quantities"),
                ("STOCK", "AddEntry",     "Add a stock entry"),

                // Reporting
                ("REPORTING", "ViewReports",   "View reports"),
                ("REPORTING", "ExportReports", "Export reports"),
            };

            // Use case-insensitive dictionary
            var result = new Dictionary<string, Controle>(StringComparer.OrdinalIgnoreCase);

            foreach (var (category, libelle, description) in controles)
            {
                var existing = await controleRepository.GetByLibelleAsync(libelle.ToUpper());
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
        private static async Task<Dictionary<string, Role>> SeedRolesAsync(
            IRoleRepository roleRepository)
        {
            if (await roleRepository.CountAsync() > 0)
            {
                await roleRepository.DeleteAllAsync();
            }

            var result = new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase);
            string[] roleNames = ["SYSTEMADMIN", "ACCOUNTANT", "SALESMANAGER", "STOCKMANAGER"];

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
            Dictionary<string, Controle> controles)
        {
            if (await privilegeRepository.CountAsync() > 0)
            {
                await privilegeRepository.DeleteAllAsync();
            }

            // Format: (RoleEnum, ControleName, IsGranted)
            var matrix = new List<(string RoleName, string Controle, bool IsGranted)>
            {
                // ── SystemAdmin — full access ──────────────
                ("SYSTEMADMIN", "ViewUsers",        true),
                ("SYSTEMADMIN", "CreateUser",       true),
                ("SYSTEMADMIN", "UpdateUser",       true),
                ("SYSTEMADMIN", "DeleteUser",       true),
                ("SYSTEMADMIN", "AssignRoles",      true),
                ("SYSTEMADMIN", "ManageAuditLogs",  true),
                ("SYSTEMADMIN", "ViewArticles",     true),
                ("SYSTEMADMIN", "ViewClients",      true),
                ("SYSTEMADMIN", "ViewStock",        true),
                ("SYSTEMADMIN", "ViewInvoices",     true),
                ("SYSTEMADMIN", "ViewPayments",     true),
                ("SYSTEMADMIN", "ViewReports",      true),
                ("SYSTEMADMIN", "AddEntry",         true),
                ("SYSTEMADMIN", "ValidateInvoice",  true),
                ("SYSTEMADMIN", "CreateArticle",    true),
                ("SYSTEMADMIN", "CreateClient",     true),
                ("SYSTEMADMIN", "CreateInvoice",    true),
                ("SYSTEMADMIN", "RecordPayment",    true),
                ("SYSTEMADMIN", "UpdateArticle",    true),
                ("SYSTEMADMIN", "UpdateClient",     true),
                ("SYSTEMADMIN", "UpdateStock",      true),
                ("SYSTEMADMIN", "DeleteClient",     true),
                ("SYSTEMADMIN", "DeleteArticle",    true),
                ("SYSTEMADMIN", "DeleteInvoice",    true),
                ("SYSTEMADMIN", "DeletePayment",    true),
                ("SYSTEMADMIN", "ExportReports",    true),
                ("SYSTEMADMIN", "ActivateUser",     true),
                ("SYSTEMADMIN", "DeactivateUser",   true),
                ("SYSTEMADMIN", "RestoreUser",      true),
                ("SYSTEMADMIN", "RestoreArticle",   true),
                ("SYSTEMADMIN", "RestoreClient",    true),
                ("SYSTEMADMIN", "RestoreInvoice",   true),
                ("SYSTEMADMIN", "RestorePayment",   true),

                // ── SalesManager ───────────────────────────
                ("SALESMANAGER", "ViewClients",      true),
                ("SALESMANAGER", "CreateClient",     true),
                ("SALESMANAGER", "UpdateClient",     true),
                ("SALESMANAGER", "DeleteClient",     true),
                ("SALESMANAGER", "ViewInvoices",     true),
                ("SALESMANAGER", "CreateInvoice",    true),
                ("SALESMANAGER", "DeleteInvoice",    true),
                ("SALESMANAGER", "ViewArticles",     true),
                ("SALESMANAGER", "ViewStock",        true),
                ("SALESMANAGER", "ViewReports",      true),
                ("SALESMANAGER", "ViewUsers",        false),
                ("SALESMANAGER", "CreateUser",       false),
                ("SALESMANAGER", "UpdateUser",       false),
                ("SALESMANAGER", "DeleteUser",       false),
                ("SALESMANAGER", "AssignRoles",      false),
                ("SALESMANAGER", "ManageAuditLogs",  false),
                ("SALESMANAGER", "ViewPayments",     false),
                ("SALESMANAGER", "AddEntry",         false),
                ("SALESMANAGER", "CreateArticle",    false),
                ("SALESMANAGER", "UpdateArticle",    false),
                ("SALESMANAGER", "DeleteArticle",    false),
                ("SALESMANAGER", "ValidateInvoice",  false),
                ("SALESMANAGER", "RecordPayment",    false),
                ("SALESMANAGER", "DeletePayment",    false),
                ("SALESMANAGER", "UpdateStock",      false),
                ("SALESMANAGER", "ExportReports",    false),
                ("SALESMANAGER",  "ActivateUser",    false),
                ("SALESMANAGER",  "DeactivateUser",  false),
                ("SALESMANAGER", "RestoreUser",      false),
                ("SALESMANAGER", "RestoreArticle",   false),
                ("SALESMANAGER", "RestoreClient",    false),
                ("SALESMANAGER", "RestoreInvoice",   false),
                ("SALESMANAGER", "RestorePayment",   false),


                // ── StockManager ───────────────────────────
                ("STOCKMANAGER", "ViewArticles",     true),
                ("STOCKMANAGER", "CreateArticle",    true),
                ("STOCKMANAGER", "UpdateArticle",    true),
                ("STOCKMANAGER", "DeleteArticle",    true),
                ("STOCKMANAGER", "AddEntry",         true),
                ("STOCKMANAGER", "ViewStock",        true),
                ("STOCKMANAGER", "UpdateStock",      true),
                ("STOCKMANAGER", "ViewReports",      true),
                ("STOCKMANAGER", "ViewUsers",        false),
                ("STOCKMANAGER", "CreateUser",       false),
                ("STOCKMANAGER", "UpdateUser",       false),
                ("STOCKMANAGER", "DeleteUser",       false),
                ("STOCKMANAGER", "AssignRoles",      false),
                ("STOCKMANAGER", "ManageAuditLogs",  false),
                ("STOCKMANAGER", "ViewClients",      false),
                ("STOCKMANAGER", "CreateClient",     false),
                ("STOCKMANAGER", "UpdateClient",     false),
                ("STOCKMANAGER", "DeleteClient",     false),
                ("STOCKMANAGER", "ViewInvoices",     false),
                ("STOCKMANAGER", "CreateInvoice",    false),
                ("STOCKMANAGER", "ValidateInvoice",  false),
                ("STOCKMANAGER", "DeleteInvoice",    false),
                ("STOCKMANAGER", "ViewPayments",     false),
                ("STOCKMANAGER", "RecordPayment",    false),
                ("STOCKMANAGER", "DeletePayment",    false),
                ("STOCKMANAGER", "ExportReports",    false),
                ("STOCKMANAGER", "ActivateUser",     false),
                ("STOCKMANAGER", "DeactivateUser",   false),
                ("STOCKMANAGER", "RestoreUser",      false),
                ("STOCKMANAGER", "RestoreArticle",   false),
                ("STOCKMANAGER", "RestoreClient",    false),
                ("STOCKMANAGER", "RestoreInvoice",   false),
                ("STOCKMANAGER", "RestorePayment",   false),


                // ── Accountant ─────────────────────────────
                ("ACCOUNTANT", "ViewInvoices",       true),
                ("ACCOUNTANT", "ViewClients",        true),
                ("ACCOUNTANT", "ViewReports",        true),
                ("ACCOUNTANT", "ExportReports",      true),
                ("ACCOUNTANT", "ValidateInvoice",    true),
                ("ACCOUNTANT", "ViewPayments",       true),
                ("ACCOUNTANT", "RecordPayment",      true),
                ("ACCOUNTANT", "ViewUsers",          false),
                ("ACCOUNTANT", "CreateUser",         false),
                ("ACCOUNTANT", "UpdateUser",         false),
                ("ACCOUNTANT", "DeleteUser",         false),
                ("ACCOUNTANT", "AssignRoles",        false),
                ("ACCOUNTANT", "ManageAuditLogs",    false),
                ("ACCOUNTANT", "CreateClient",       false),
                ("ACCOUNTANT", "UpdateClient",       false),
                ("ACCOUNTANT", "DeleteClient",       false),
                ("ACCOUNTANT", "ViewArticles",       false),
                ("ACCOUNTANT", "CreateArticle",      false),
                ("ACCOUNTANT", "UpdateArticle",      false),
                ("ACCOUNTANT", "DeleteArticle",      false),
                ("ACCOUNTANT", "CreateInvoice",      false),
                ("ACCOUNTANT", "DeleteInvoice",      false),
                ("ACCOUNTANT", "DeletePayment",      false),
                ("ACCOUNTANT", "ViewStock",          false),
                ("ACCOUNTANT", "UpdateStock",        false),
                ("ACCOUNTANT", "AddEntry",           false),
                ("ACCOUNTANT", "ActivateUser",       false),
                ("ACCOUNTANT", "DeactivateUser",     false),
                ("ACCOUNTANT", "RestoreUser",        false),
                ("ACCOUNTANT", "RestoreArticle",     false),
                ("ACCOUNTANT", "RestoreClient",      false),
                ("ACCOUNTANT", "RestoreInvoice",     false),
                ("ACCOUNTANT", "RestorePayment",     false),

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
            IConfiguration configuration,
            IPasswordHasher<AuthUser> passwordHasher)
        {
            if (await userRepository.CountAsync() > 0)
            {
                await userRepository.DeleteAllAsync();
            }
            List<Role> roles = await roleRepository.GetAllAsync();

            var adminRole = roles.Find(r => r.Libelle == "SYSTEMADMIN")  ?? throw new InvalidOperationException($"Role 'SYSTEMADMIN' not found. Ensure roles are seeded before users.");
            var salesRole = roles.Find(r => r.Libelle == "SALESMANAGER") ?? throw new InvalidOperationException($"Role 'SALESMANAGER' not found.");
            var stockRole = roles.Find(r => r.Libelle == "STOCKMANAGER") ?? throw new InvalidOperationException($"Role 'STOCKMANAGER' not found.");
            var accountRole = roles.Find(r => r.Libelle == "ACCOUNTANT") ?? throw new InvalidOperationException($"Role 'ACCOUNTANT' not found.");


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