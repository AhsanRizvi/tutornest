import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './landing-page.component.html',
  styleUrls: ['./landing-page.component.scss']
})
export class LandingPageComponent implements OnInit, OnDestroy {
  // Mobile Nav Toggle
  isMobileMenuOpen = signal<boolean>(false);

  // Active section tracking
  activeSection = signal<string>('home');

  // Contact Form Fields
  contactForm = {
    name: '',
    email: '',
    message: ''
  };

  // Contact Form Status
  isSubmitting = signal<boolean>(false);
  formSubmitted = signal<boolean>(false);
  submitSuccess = signal<boolean>(false);

  // WhatsApp Link Helper
  whatsappNumber = '+94759923333';
  get whatsappLink(): string {
    const text = encodeURIComponent("Hi TutorNest, I would like to register for the LMS system. Please send me registration details.");
    return `https://wa.me/${this.whatsappNumber}?text=${text}`;
  }

  // Dashboard Mockup Stats (for interactive visual widgets)
  drmStatus = signal<string>('Fully Active');
  activeStreams = signal<number>(142);
  bandwidthSaved = signal<string>('1.2 TB');
  blockAttempts = signal<number>(24);

  // Interval for mockup changes
  private mockupInterval: any;
  private scrollListener: any;

  constructor(private router: Router) {}

  ngOnInit(): void {
    // Periodically update some dashboard stats to make mockup feel "alive"
    this.mockupInterval = setInterval(() => {
      this.activeStreams.update(n => n + (Math.random() > 0.5 ? 1 : -1));
      if (Math.random() > 0.8) {
        this.blockAttempts.update(n => n + 1);
      }
    }, 5000);

    // Watch scroll for active section highlight
    this.scrollListener = this.onScroll.bind(this);
    window.addEventListener('scroll', this.scrollListener);
  }

  ngOnDestroy(): void {
    if (this.mockupInterval) clearInterval(this.mockupInterval);
    if (this.scrollListener) window.removeEventListener('scroll', this.scrollListener);
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen.set(!this.isMobileMenuOpen());
  }

  closeMobileMenu(): void {
    this.isMobileMenuOpen.set(false);
  }

  scrollToSection(sectionId: string, event?: Event): void {
    if (event) {
      event.preventDefault();
    }
    this.closeMobileMenu();
    
    const element = document.getElementById(sectionId);
    if (element) {
      const headerOffset = 80;
      const elementPosition = element.getBoundingClientRect().top;
      const offsetPosition = elementPosition + window.scrollY - headerOffset;

      window.scrollTo({
        top: offsetPosition,
        behavior: 'smooth'
      });
      this.activeSection.set(sectionId);
    }
  }

  onScroll(): void {
    const sections = ['home', 'features', 'about', 'contact'];
    const scrollPosition = window.scrollY + 120; // offset

    for (const section of sections) {
      const element = document.getElementById(section);
      if (element) {
        const top = element.offsetTop;
        const height = element.offsetHeight;
        if (scrollPosition >= top && scrollPosition < top + height) {
          this.activeSection.set(section);
          break;
        }
      }
    }
  }

  submitContactForm(): void {
    if (!this.contactForm.name || !this.contactForm.email || !this.contactForm.message) {
      return;
    }

    this.isSubmitting.set(true);

    // Mock an HTTP API submission
    setTimeout(() => {
      this.isSubmitting.set(false);
      this.formSubmitted.set(true);
      this.submitSuccess.set(true);

      // Reset form after a few seconds
      setTimeout(() => {
        this.contactForm = { name: '', email: '', message: '' };
        this.formSubmitted.set(false);
        this.submitSuccess.set(false);
      }, 5000);
    }, 1500);
  }

  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }
}
