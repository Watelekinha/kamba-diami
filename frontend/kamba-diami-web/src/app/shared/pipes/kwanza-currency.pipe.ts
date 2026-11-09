import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'kwanza', standalone: true })
export class KwanzaCurrencyPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value === null || value === undefined) return '';
    return new Intl.NumberFormat('pt-AO', { maximumFractionDigits: 0 }).format(value) + ' Kzs';
  }
}
