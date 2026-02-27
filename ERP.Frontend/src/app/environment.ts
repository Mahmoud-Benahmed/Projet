export const environment = {
  production: false,
  docker: false,
  apiUrl:'' as string,
  routes:{
    auth:   '/auth',
    roles:  '/auth/roles',
    privileges: '/auth/privileges',
    controles: '/auth/controles',
    users:'/users'
  }
};

environment.apiUrl = environment.docker ? 'http://localhost:5000' : 'http://localhost:5031';
