import { Directive, Input } from '@angular/core';
import { AbstractControl, NG_VALIDATORS, ValidationErrors, Validator } from '@angular/forms';

@Directive({
  selector: '[notSameAs]',
  standalone: true,
  providers: [{ provide: NG_VALIDATORS, useExisting: NotSameAsDirective, multi: true }]
})
export class NotSameAsDirective implements Validator {
  @Input() notSameAs!: string; // sibling control name

  validate(control: AbstractControl): ValidationErrors | null {
    const sibling = control.parent?.get(this.notSameAs);
    if (!sibling || !control.value || !sibling.value) return null;
    return control.value === sibling.value ? { notSameAs: true } : null;
  }
}
