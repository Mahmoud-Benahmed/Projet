import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Fournisseur } from './fournisseur';

describe('Fournisseur', () => {
  let component: Fournisseur;
  let fixture: ComponentFixture<Fournisseur>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Fournisseur]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Fournisseur);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
