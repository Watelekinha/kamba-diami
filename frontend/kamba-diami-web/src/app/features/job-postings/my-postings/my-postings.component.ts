import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JobPostingsService } from '../../../core/services/job-postings.service';
import { JobPosting } from '../../../core/models/job-posting.model';
import { JobPostingCardComponent } from '../../../shared/components/job-posting-card/job-posting-card.component';

@Component({
  selector: 'app-my-postings',
  standalone: true,
  imports: [CommonModule, RouterLink, JobPostingCardComponent],
  templateUrl: './my-postings.component.html',
  styleUrl: './my-postings.component.scss'
})
export class MyPostingsComponent implements OnInit {
  postings: JobPosting[] = [];
  loading = signal(false);

  constructor(private jobPostingsService: JobPostingsService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.jobPostingsService.getMine().subscribe({
      next: (postings) => {
        this.postings = postings;
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onDelete(id: number): void {
    if (!confirm('Tens a certeza que queres remover esta publicação?')) return;
    this.jobPostingsService.delete(id).subscribe(() => this.load());
  }
}
