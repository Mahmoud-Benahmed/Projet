import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListAll } from './home';

describe('ListAll', () => {
  let component: ListAll;
  let fixture: ComponentFixture<ListAll>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListAll]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ListAll);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
