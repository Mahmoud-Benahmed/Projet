export const environment = {
  production: false,
  docker: false,
  apiUrl: '' as string,
  authUrl: '/auth',
  usersUrl: '/users'
};

environment.apiUrl = environment.docker ? 'http://localhost:5000' : 'http://localhost:5031';
