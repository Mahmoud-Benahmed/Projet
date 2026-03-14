import { TestBed } from '@angular/core/testing';

import { CurrencyConfig } from './currency-config.service';

describe('CurrencyConfig', () => {
  let service: CurrencyConfig;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CurrencyConfig);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
