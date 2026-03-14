import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Deleted } from './deleted';

describe('Deleted', () => {
  let component: Deleted;
  let fixture: ComponentFixture<Deleted>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Deleted]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Deleted);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
