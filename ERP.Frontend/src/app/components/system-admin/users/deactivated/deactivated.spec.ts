import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Deactivated } from './deactivated';

describe('Deactivated', () => {
  let component: Deactivated;
  let fixture: ComponentFixture<Deactivated>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Deactivated]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Deactivated);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
