namespace ERP.Gateway.Properties;
public static class Privileges
{
    // ── Auth
    public static class Users
    {
        public const string VIEW_USERS = "VIEW_USERS";
        public const string CREATE_USER = "CREATE_USER";
        public const string ACTIVATE_USER = "ACTIVATE_USER";
        public const string DEACTIVATE_USER = "DEACTIVATE_USER";
        public const string UPDATE_USER = "UPDATE_USER";
        public const string DELETE_USER = "DELETE_USER";
        public const string RESTORE_USER = "RESTORE_USER";
        public const string MANAGE_USERS = "MANAGE_USERS";
        public const string ASSIGN_ROLES = "ASSIGN_ROLES";
    }

    // ── Audit
    public static class Audit
    {
        public const string MANAGE_AUDITLOGS = "MANAGE_AUDITLOGS";
    }

    // ── Clients
    public static class Clients
    {
        public const string VIEW_CLIENTS = "VIEW_CLIENTS";
        public const string CREATE_CLIENT = "CREATE_CLIENT";
        public const string UPDATE_CLIENT = "UPDATE_CLIENT";
        public const string DELETE_CLIENT = "DELETE_CLIENT";
        public const string RESTORE_CLIENT = "RESTORE_CLIENT";
        public const string MANAGE_CLIENTS = "MANAGE_CLIENTS";
    }

    // ── Articles
    public static class Articles
    {
        public const string VIEW_ARTICLES = "VIEW_ARTICLES";
        public const string CREATE_ARTICLE = "CREATE_ARTICLE";
        public const string UPDATE_ARTICLE = "UPDATE_ARTICLE";
        public const string DELETE_ARTICLE = "DELETE_ARTICLE";
        public const string RESTORE_ARTICLE = "RESTORE_ARTICLE";
        public const string MANAGE_ARTICLES = "MANAGE_ARTICLES";
    }

    // ── Invoices
    public static class Invoices
    {
        public const string VIEW_INVOICES = "VIEW_INVOICES";
        public const string CREATE_INVOICE = "CREATE_INVOICE";
        public const string VALIDATE_INVOICE = "VALIDATE_INVOICE";
        public const string DELETE_INVOICE = "DELETE_INVOICE";
        public const string RESTORE_INVOICE = "RESTORE_INVOICE";
        public const string MANAGE_INVOICES = "MANAGE_INVOICES";
    }

    // ── Payments
    public static class Payments
    {
        public const string VIEW_PAYMENTS = "VIEW_PAYMENTS";
        public const string RECORD_PAYMENT = "RECORD_PAYMENT";
        public const string DELETE_PAYMENT = "DELETE_PAYMENT";
        public const string RESTORE_PAYMENT = "RESTORE_PAYMENT";
        public const string MANAGE_PAYMENTS = "MANAGE_PAYMENTS";
    }

    // ── Stock
    public static class Stock
    {
        public const string VIEW_STOCK = "VIEW_STOCK";
        public const string UPDATE_STOCK = "UPDATE_STOCK";
        public const string ADD_ENTRY = "ADD_ENTRY";
        public const string MANAGE_STOCK = "MANAGE_STOCK";
    }

    // Reports
    public static class Reports
    {
        public const string VIEW_REPORTS = "VIEW_REPORTS";
        public const string EXPORT_REPORTS = "EXPORT_REPORTS";
    }
}