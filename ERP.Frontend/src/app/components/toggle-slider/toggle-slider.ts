// file: custom-toggle.component.ts
import { Component, forwardRef, Input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-toggle-slider',
  template: `
  <div class="toggle-slider">
    <div class="toggle-wrapper" [class.checked]="value" (click)="toggle()">
      <div class="toggle-handle"></div>
    </div>
    <span class="toggle-label">{{ label }}</span>
  </div>
  `,
  styleUrls: ['./toggle-slider.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CustomToggleComponent),
      multi: true
    }
  ]
})
export class CustomToggleComponent implements ControlValueAccessor {
  @Input() label = '';

  value = false;
  disabled = false;

  onChange = (value: boolean) => {};
  onTouched = () => {};

  toggle() {
    if (this.disabled) return;
    this.value = !this.value;
    this.onChange(this.value);
    this.onTouched();
  }

  // ControlValueAccessor methods
  writeValue(value: boolean): void {
    this.value = value;
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
