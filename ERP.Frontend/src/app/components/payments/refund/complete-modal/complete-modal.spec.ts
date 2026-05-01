import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CompleteForm } from './complete.form';

describe('CompleteForm', () => {
  let component: CompleteForm;
  let fixture: ComponentFixture<CompleteForm>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CompleteForm]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CompleteForm);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
