import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { JobPosting } from '../../../core/models/job-posting.model';
import { JobPostingsService } from '../../../core/services/job-postings.service';
import { SavedPostingsService } from '../../../core/services/saved-postings.service';
import { JobPostingCardComponent } from '../../../shared/components/job-posting-card/job-posting-card.component';

@Component({
  selector: 'app-saved-postings',
  standalone: true,
  imports: [CommonModule, RouterLink, JobPostingCardComponent],
  templateUrl: './saved-postings.component.html',
  styleUrl: './saved-postings.component.scss'
})
export class SavedPostingsComponent implements OnInit {
  postings: JobPosting[] = [];
  loading = signal(false);

  constructor(
    private jobPostingsService: JobPostingsService,
    public savedPostingsService: SavedPostingsService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const ids = this.savedPostingsService.ids();
    if (ids.length === 0) {
      this.postings = [];
      return;
    }

    this.loading.set(true);
    forkJoin(
      ids.map((id) => this.jobPostingsService.getById(id).pipe(catchError(() => of(null))))
    ).subscribe({
      next: (items) => {
        this.postings = items.filter((item): item is JobPosting => item !== null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
