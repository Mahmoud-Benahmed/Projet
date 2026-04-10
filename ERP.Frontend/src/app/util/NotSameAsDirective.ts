// src/app/directives/not-same-as.directive.ts
import { Directive, Input } from '@angular/core';
import { AbstractControl, NG_VALIDATORS, ValidationErrors, Validator } from '@angular/forms';

@Directive({
  selector: '[notSameAs]',
  standalone: true,
  providers: [{ provide: NG_VALIDATORS, useExisting: NotSameAsDirective, multi: true }],
})
export class NotSameAsDirective implements Validator {

  @Input() notSameAs = '';

  validate(control: AbstractControl): ValidationErrors | null {
    const form = control.parent;
    if (!form) return null;

    const otherControl = form.get(this.notSameAs);
    if (!otherControl) return null;

    return control.value && control.value === otherControl.value
      ? { notSameAs: true }
      : null;
  }
}
