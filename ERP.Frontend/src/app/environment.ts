export const environment = {
  production: false,
  docker: true,
  apiUrl:'' as string,
  routes:{
    auth:   '/auth',
    roles:  '/auth/roles',
    privileges: '/auth/privileges',
    controles: '/auth/controles',
    articles: '/articles',
    clients: '/clients'
  }
};

environment.apiUrl = environment.docker ? 'http://localhost:5000' : 'http://localhost:5031';
