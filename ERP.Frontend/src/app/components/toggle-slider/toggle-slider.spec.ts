import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ToggleSlider } from './toggle-slider';

describe('ToggleSlider', () => {
  let component: ToggleSlider;
  let fixture: ComponentFixture<ToggleSlider>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToggleSlider]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ToggleSlider);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
