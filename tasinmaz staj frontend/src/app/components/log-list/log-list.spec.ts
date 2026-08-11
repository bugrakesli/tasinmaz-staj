import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { LogList } from './log-list';

describe('LogList', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LogList],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(LogList);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
