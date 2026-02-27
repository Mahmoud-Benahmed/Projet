import { Directive, Input } from '@angular/core';
import { AbstractControl, NG_VALIDATORS, ValidationErrors, Validator } from '@angular/forms';

@Directive({
  selector: '[sameAs]',
  standalone: true,
  providers: [{ provide: NG_VALIDATORS, useExisting: SameAsDirective, multi: true }]
})
export class SameAsDirective implements Validator {
  @Input() sameAs!: string; // sibling control name

  validate(control: AbstractControl): ValidationErrors | null {
    const sibling = control.parent?.get(this.sameAs);
    if (!sibling || !control.value || !sibling.value) return null;
    return control.value === sibling.value ? { sameAs: true } : null;
  }
}
