import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MustChangePassword } from './must-change-password';

describe('MustChangePassword', () => {
  let component: MustChangePassword;
  let fixture: ComponentFixture<MustChangePassword>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MustChangePassword]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MustChangePassword);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
