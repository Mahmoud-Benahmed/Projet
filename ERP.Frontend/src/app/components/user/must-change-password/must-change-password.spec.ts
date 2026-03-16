import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MustChangePasswordComponent } from './must-change-password';

describe('MustChangePasswordComponent', () => {
  let component: MustChangePasswordComponent;
  let fixture: ComponentFixture<MustChangePasswordComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MustChangePasswordComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MustChangePasswordComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
