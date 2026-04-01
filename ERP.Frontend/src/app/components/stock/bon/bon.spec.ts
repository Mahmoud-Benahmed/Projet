import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Bon } from './bon';

describe('Bon', () => {
  let component: Bon;
  let fixture: ComponentFixture<Bon>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Bon]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Bon);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
