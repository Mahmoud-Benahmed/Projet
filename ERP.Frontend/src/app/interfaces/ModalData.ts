import { ModalInput } from "./ModalInput";

export interface ModalData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  showCancel?: boolean;
  icon?: string;
  iconColor?: 'primary' | 'warn' | 'accent';
  inputs?: ModalInput[];
}
