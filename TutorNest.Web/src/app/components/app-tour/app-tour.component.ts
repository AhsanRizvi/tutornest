import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, HostListener, signal, effect, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface TourStep {
  targetSelector?: string;
  title: string;
  content: string;
  position?: 'top' | 'bottom' | 'left' | 'right' | 'center';
}

@Component({
  selector: 'app-tour',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app-tour.component.html',
  styleUrls: ['./app-tour.component.scss']
})
export class AppTourComponent implements OnInit, OnDestroy {
  @Input() steps: TourStep[] = [];
  @Input() tourId: string = 'app';
  @Input() userEmail: string = '';
  
  @Output() tourCompleted = new EventEmitter<void>();
  @Output() tourSkipped = new EventEmitter<void>();

  currentStepIndex = signal<number>(0);
  
  highlightTop = signal<number>(0);
  highlightLeft = signal<number>(0);
  highlightWidth = signal<number>(0);
  highlightHeight = signal<number>(0);
  hasTarget = signal<boolean>(false);

  tooltipTop = signal<number>(0);
  tooltipLeft = signal<number>(0);
  tooltipPosition = signal<string>('center');

  private resizeObserver: ResizeObserver | null = null;

  constructor() {
    effect(() => {
      this.positionTourStep();
    });
  }

  ngOnInit(): void {
    window.addEventListener('scroll', this.onScrollResize, true);
    window.addEventListener('resize', this.onScrollResize);
  }

  ngOnDestroy(): void {
    window.removeEventListener('scroll', this.onScrollResize, true);
    window.removeEventListener('resize', this.onScrollResize);
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
  }

  private onScrollResize = () => {
    this.positionTourStep();
  };

  @HostListener('window:keydown', ['$event'])
  handleKeyDown(event: KeyboardEvent): void {
    if (event.key === 'ArrowRight') {
      this.nextStep();
    } else if (event.key === 'ArrowLeft') {
      this.prevStep();
    } else if (event.key === 'Escape') {
      this.skipTour();
    }
  }

  nextStep(): void {
    if (this.currentStepIndex() < this.steps.length - 1) {
      this.currentStepIndex.set(this.currentStepIndex() + 1);
    } else {
      this.completeTour();
    }
  }

  prevStep(): void {
    if (this.currentStepIndex() > 0) {
      this.currentStepIndex.set(this.currentStepIndex() - 1);
    }
  }

  skipTour(): void {
    this.saveTourState();
    this.tourSkipped.emit();
  }

  completeTour(): void {
    this.saveTourState();
    this.tourCompleted.emit();
  }

  private saveTourState(): void {
    const emailKey = this.userEmail ? `_${this.userEmail}` : '';
    localStorage.setItem(`seen_tour${emailKey}_${this.tourId}`, 'true');
  }

  private positionTourStep(): void {
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
      this.resizeObserver = null;
    }

    const step = this.steps[this.currentStepIndex()];
    if (!step) return;

    const selector = step.targetSelector;
    let targetEl: HTMLElement | null = null;
    if (selector) {
      targetEl = document.querySelector(selector) as HTMLElement;
    }

    if (targetEl) {
      this.hasTarget.set(true);

      if (typeof ResizeObserver !== 'undefined') {
        this.resizeObserver = new ResizeObserver(() => {
          this.calculateCoordinates(targetEl!, step);
        });
        this.resizeObserver.observe(targetEl);
      }

      this.calculateCoordinates(targetEl, step);
    } else {
      this.hasTarget.set(false);
      this.tooltipPosition.set('center');
      
      const tooltipWidth = 400;
      const tooltipHeight = 220;
      this.tooltipLeft.set(window.innerWidth / 2 - tooltipWidth / 2);
      this.tooltipTop.set(window.innerHeight / 2 - tooltipHeight / 2);
    }
  }

  private calculateCoordinates(targetEl: HTMLElement, step: TourStep): void {
    const rect = targetEl.getBoundingClientRect();
    const margin = 6;
    
    this.highlightTop.set(rect.top - margin);
    this.highlightLeft.set(rect.left - margin);
    this.highlightWidth.set(rect.width + margin * 2);
    this.highlightHeight.set(rect.height + margin * 2);

    const pos = step.position || 'bottom';
    this.tooltipPosition.set(pos);

    const tooltipWidth = 385;
    const tooltipHeight = 200;

    let tLeft = 0;
    let tTop = 0;

    const highlightCenterX = rect.left + rect.width / 2;
    const highlightCenterY = rect.top + rect.height / 2;

    switch (pos) {
      case 'bottom':
        tTop = rect.bottom + 16;
        tLeft = highlightCenterX - tooltipWidth / 2;
        break;
      case 'top':
        tTop = rect.top - tooltipHeight - 16;
        tLeft = highlightCenterX - tooltipWidth / 2;
        break;
      case 'left':
        tTop = highlightCenterY - tooltipHeight / 2;
        tLeft = rect.left - tooltipWidth - 16;
        break;
      case 'right':
        tTop = highlightCenterY - tooltipHeight / 2;
        tLeft = rect.right + 16;
        break;
      case 'center':
      default:
        tTop = window.innerHeight / 2 - tooltipHeight / 2;
        tLeft = window.innerWidth / 2 - tooltipWidth / 2;
        break;
    }

    const padding = 15;
    tLeft = Math.max(padding, Math.min(tLeft, window.innerWidth - tooltipWidth - padding));
    tTop = Math.max(padding, Math.min(tTop, window.innerHeight - tooltipHeight - padding));

    this.tooltipLeft.set(tLeft);
    this.tooltipTop.set(tTop);
  }
}
