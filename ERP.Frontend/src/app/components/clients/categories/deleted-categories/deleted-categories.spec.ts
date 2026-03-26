import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DeletedCategories } from './deleted-categories';

describe('DeletedCategories', () => {
  let component: DeletedCategories;
  let fixture: ComponentFixture<DeletedCategories>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DeletedCategories]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DeletedCategories);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
