export interface ModalInput {
  key: string;          // identifier to retrieve the value
  label: string;
  type?: 'text' | 'password' | 'email' | 'number';
  placeholder?: string;
  required?: boolean;
}
