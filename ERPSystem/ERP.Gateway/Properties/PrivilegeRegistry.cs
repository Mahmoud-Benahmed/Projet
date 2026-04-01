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
        new(Privileges.Users.MANAGE_USERS,      "AUTH", "Manage users"),
        new(Privileges.Users.ASSIGN_ROLES,      "AUTH", "Assign roles"),
        new(Privileges.Users.CREATE_ROLE,       "AUTH", "Create roles"),
        new(Privileges.Users.UPDATE_ROLE,       "AUTH", "Update roles"),
        new(Privileges.Users.DELETE_ROLE,       "AUTH", "Delete roles"),
        new(Privileges.Users.CREATE_CONTROLE,   "AUTH", "Create controles"),
        new(Privileges.Users.UPDATE_CONTROLE,   "AUTH", "Update controles"),
        new(Privileges.Users.DELETE_CONTROLE,   "AUTH", "Delete controles"),

        new(Privileges.Audit.MANAGE_AUDITLOGS,  "AUTH", "Manage audit logs"),

        // ── Clients
        new(Privileges.Clients.VIEW_CLIENTS,    "CLIENTS", "View clients"),
        new(Privileges.Clients.CREATE_CLIENT,   "CLIENTS", "Create client"),
        new(Privileges.Clients.UPDATE_CLIENT,   "CLIENTS", "Update client"),
        new(Privileges.Clients.DELETE_CLIENT,   "CLIENTS", "Delete client"),
        new(Privileges.Clients.RESTORE_CLIENT,  "CLIENTS", "Restore client"),
        new(Privileges.Clients.MANAGE_CLIENTS,  "CLIENTS", "Manage clients"),
        new(Privileges.Clients.CREATE_CLIENT_CATEGORIES ,  "CLIENTS", "Create client categories"),
        new(Privileges.Clients.UPDATE_CLIENT_CATEGORIES ,  "CLIENTS", "Update client categories"),
        new(Privileges.Clients.DELETE_CLIENT_CATEGORIES ,  "CLIENTS", "Delete client categories"),
        new(Privileges.Clients.RESTORE_CLIENT_CATEGORIES,  "CLIENTS", "Restore client categories"),

        // ── Articles
        new(Privileges.Articles.VIEW_ARTICLES,    "ARTICLES", "View articles"),
        new(Privileges.Articles.CREATE_ARTICLE,   "ARTICLES", "Create article"),
        new(Privileges.Articles.UPDATE_ARTICLE,   "ARTICLES", "Update article"),
        new(Privileges.Articles.DELETE_ARTICLE,   "ARTICLES", "Delete article"),
        new(Privileges.Articles.RESTORE_ARTICLE,  "ARTICLES", "Restore article"),
        new(Privileges.Articles.MANAGE_ARTICLES,  "ARTICLES", "Manage articles"),
        new(Privileges.Articles.CREATE_ARTICLE_CATEGORIES ,  "ARTICLES", "Create categories for articles"),
        new(Privileges.Articles.UPDATE_ARTICLE_CATEGORIES ,  "ARTICLES", "Update categories for articles"),
        new(Privileges.Articles.DELETE_ARTICLE_CATEGORIES ,  "ARTICLES", "Delete categories for articles"),
        new(Privileges.Articles.RESTORE_ARTICLE_CATEGORIES,  "ARTICLES", "Restore categories for articles"),

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

        // ── Stocknew(Privileges.Stock.MANAGE_STOCK,   "STOCK", "Full stock management"),
        new(Privileges.Stock.VIEW_STOCK,     "STOCK", "View stock"),
        new(Privileges.Stock.UPDATE_STOCK,   "STOCK", "Update stock"),
        new(Privileges.Stock.ADD_ENTRY,      "STOCK", "Add stock entry"),

        // Fournisseurs
        new(Privileges.Stock.MANAGE_STOCK,   "FOURNISSEURS", "Manage fournisseurs"),

        // Bons d'Entrée
        new(Privileges.Stock.ADD_ENTRY,      "BON_ENTRES",   "Create bon entrée"),
        new(Privileges.Stock.VIEW_STOCK,     "BON_ENTRES",   "View bons entrée"),
        new(Privileges.Stock.UPDATE_STOCK,   "BON_ENTRES",   "Update bon entrée"),
        new(Privileges.Stock.MANAGE_STOCK,   "BON_ENTRES",   "Delete bon entrée"),

        // Bons de Sortie
        new(Privileges.Stock.ADD_ENTRY,      "BON_SORTIES",  "Create bon sortie"),
        new(Privileges.Stock.VIEW_STOCK,     "BON_SORTIES",  "View bons sortie"),
        new(Privileges.Stock.UPDATE_STOCK,   "BON_SORTIES",  "Update bon sortie"),
        new(Privileges.Stock.MANAGE_STOCK,   "BON_SORTIES",  "Delete bon sortie"),

        // Bons de Retour
        new(Privileges.Stock.ADD_ENTRY,      "BON_RETOURS",  "Create bon retour"),
        new(Privileges.Stock.VIEW_STOCK,     "BON_RETOURS",  "View bons retour"),
        new(Privileges.Stock.UPDATE_STOCK,   "BON_RETOURS",  "Update bon retour"),
        new(Privileges.Stock.MANAGE_STOCK,   "BON_RETOURS",  "Delete bon retour"),

        // ── Reports
        new(Privileges.Reports.VIEW_REPORTS,   "REPORTING", "View reports"),
        new(Privileges.Reports.EXPORT_REPORTS, "REPORTING", "Export reports"),
    };
}