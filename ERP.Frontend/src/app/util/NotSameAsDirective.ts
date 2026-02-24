import { Directive, Input } from '@angular/core';
import { AbstractControl, NG_VALIDATORS, ValidationErrors, Validator } from '@angular/forms';

@Directive({
  selector: '[notSameAs]',
  standalone: true,
  providers: [
    {
      provide: NG_VALIDATORS,
      useExisting: NotSameAsDirective,
      multi: true,
    }
  ]
})
export class NotSameAsDirective implements Validator {
  @Input() notSameAs: string = '';

  validate(control: AbstractControl): ValidationErrors | null {
    if (!control.value || !this.notSameAs) return null;
    return control.value === this.notSameAs
      ? { notSameAs: true }
      : null;
  }
}
