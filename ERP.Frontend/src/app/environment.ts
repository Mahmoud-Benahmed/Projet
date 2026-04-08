export const environment = {
  production: false,
  docker: false,

  get apiUrl(): string {
    return this.docker ? 'http://localhost:5000' : 'http://localhost:5031';
  },

  routes: {
    auth:        '/auth',
    roles:       '/auth/roles',
    controles:   '/auth/controles',
    privileges:  '/auth/privileges',
    articles:    '/articles',
    clients:     '/clients',
    stock:       '/stock',
    fournisseurs:'/fournisseurs',
    invoices:    '/invoices',
  },
} as const;