import { Directive, Input } from '@angular/core';
import { NG_VALIDATORS, Validator, AbstractControl, ValidationErrors } from '@angular/forms';

@Directive({
  selector: '[notSameAs]',
  standalone: true,
  providers: [{ provide: NG_VALIDATORS, useExisting: NotSameAsDirective, multi: true }]
})
export class NotSameAsDirective implements Validator {
  @Input('notSameAs') otherValue!: string;

  validate(control: AbstractControl): ValidationErrors | null {
    if (control.value && control.value === this.otherValue) {
      return { sameAsOld: true };
    }
    return null;
  }
}
