import { Directive, Input } from '@angular/core';
import { AbstractControl, NG_VALIDATORS, ValidationErrors, Validator } from '@angular/forms';

@Directive({
  selector: '[sameAs]',
  standalone: true,
  providers: [
    {
      provide: NG_VALIDATORS,
      useExisting: SameAsDirective,
      multi: true,
    }
  ]
})
export class SameAsDirective implements Validator {
  @Input() sameAs: string = '';

  validate(control: AbstractControl): ValidationErrors | null {
    if (!control.value || !this.sameAs) return null;
    return control.value === this.sameAs
      ? { sameAs: true }
      : null;
  }
}
