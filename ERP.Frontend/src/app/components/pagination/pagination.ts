import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pagination.html',
  styleUrl: './pagination.scss'
})
export class PaginationComponent {
  @Input() pageNumber = 1;
  @Input() totalPages = 1;
  @Input() pageSize = 10;
  @Input() pageSizeOptions: number[] = [5, 10, 25, 50];

  @Output() pageChange     = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  prev(): void  { if (this.pageNumber > 1) this.pageChange.emit(this.pageNumber - 1); }
  next(): void  { if (this.pageNumber < this.totalPages) this.pageChange.emit(this.pageNumber + 1); }

  onSizeChange(size: number): void {
    this.pageSizeChange.emit(Number(size));
  }
}
