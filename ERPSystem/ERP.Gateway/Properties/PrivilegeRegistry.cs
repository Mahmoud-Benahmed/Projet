namespace ERP.Gateway.Properties;
public record PrivilegeDefinition(
    string Code,
    string Category,
    string Description
);

public static class PrivilegeRegistry
{
    public static readonly List<PrivilegeDefinition> All = new()
    {
        // ── Auth
        new(Privileges.Users.VIEW_USERS,        "AUTH", "View users"),
        new(Privileges.Users.CREATE_USER,       "AUTH", "Create users"),
        new(Privileges.Users.UPDATE_USER,       "AUTH", "Update users"),
        new(Privileges.Users.DELETE_USER,       "AUTH", "Delete users"),
        new(Privileges.Users.RESTORE_USER,      "AUTH", "Restore users"),
        new(Privileges.Users.ACTIVATE_USER,     "AUTH", "Activate users"),
        new(Privileges.Users.DEACTIVATE_USER,   "AUTH", "Deactivate users"),
        new(Privileges.Users.ASSIGN_ROLES,      "AUTH", "Assign roles"),
        new(Privileges.Audit.MANAGE_AUDITLOGS,  "AUTH", "Manage audit logs"),

        // ── Clients
        new(Privileges.Clients.VIEW_CLIENTS,    "CLIENTS", "View clients"),
        new(Privileges.Clients.CREATE_CLIENT,   "CLIENTS", "Create client"),
        new(Privileges.Clients.UPDATE_CLIENT,   "CLIENTS", "Update client"),
        new(Privileges.Clients.DELETE_CLIENT,   "CLIENTS", "Delete client"),
        new(Privileges.Clients.RESTORE_CLIENT,  "CLIENTS", "Restore client"),

        // ── Articles
        new(Privileges.Articles.VIEW_ARTICLES,    "ARTICLES", "View articles"),
        new(Privileges.Articles.CREATE_ARTICLE,   "ARTICLES", "Create article"),
        new(Privileges.Articles.UPDATE_ARTICLE,   "ARTICLES", "Update article"),
        new(Privileges.Articles.DELETE_ARTICLE,   "ARTICLES", "Delete article"),
        new(Privileges.Articles.RESTORE_ARTICLE,  "ARTICLES", "Restore article"),

        // ── Invoices
        new(Privileges.Invoices.VIEW_INVOICES,     "FACTURATION", "View invoices"),
        new(Privileges.Invoices.CREATE_INVOICE,    "FACTURATION", "Create invoice"),
        new(Privileges.Invoices.VALIDATE_INVOICE,  "FACTURATION", "Validate invoice"),
        new(Privileges.Invoices.DELETE_INVOICE,    "FACTURATION", "Delete invoice"),
        new(Privileges.Invoices.RESTORE_INVOICE,   "FACTURATION", "Restore invoice"),

        // ── Payments
        new(Privileges.Payments.VIEW_PAYMENTS,     "PAIEMENT", "View payments"),
        new(Privileges.Payments.RECORD_PAYMENT,    "PAIEMENT", "Record payment"),
        new(Privileges.Payments.DELETE_PAYMENT,    "PAIEMENT", "Delete payment"),
        new(Privileges.Payments.RESTORE_PAYMENT,   "PAIEMENT", "Restore payment"),

        // ── Stock
        new(Privileges.Stock.VIEW_STOCK,     "STOCK", "View stock"),
        new(Privileges.Stock.UPDATE_STOCK,   "STOCK", "Update stock"),
        new(Privileges.Stock.ADD_ENTRY,      "STOCK", "Add stock entry"),

        // ── Reports
        new(Privileges.Reports.VIEW_REPORTS,   "REPORTING", "View reports"),
        new(Privileges.Reports.EXPORT_REPORTS, "REPORTING", "Export reports"),
    };
}