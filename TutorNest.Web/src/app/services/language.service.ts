import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  private currentLangSubject = new BehaviorSubject<string>('en');
  currentLang$ = this.currentLangSubject.asObservable();

  private translations: Record<string, Record<string, string>> = {
    en: {
      'nav.dashboard': 'Dashboard',
      'nav.liveClasses': 'Live Classes',
      'nav.courses': 'Courses',
      'nav.assignments': 'Assignments',
      'nav.announcements': 'Announcements',
      'nav.analytics': 'Analytics',
      'nav.billing': 'Billing & Plans',
      'nav.profile': 'Profile Settings',
      'nav.referrals': 'Referrals',
      'dashboard.welcome': 'Welcome back,',
      'dashboard.upcomingLive': 'Upcoming Live Classes',
      'dashboard.noLive': 'No upcoming live classes scheduled.',
      'dashboard.scheduleLive': 'Schedule a Live Class',
      'dashboard.liveTitle': 'Live Class Title',
      'dashboard.liveDesc': 'Description',
      'dashboard.liveTime': 'Scheduled Start Time',
      'dashboard.liveDuration': 'Duration (minutes)',
      'dashboard.liveLink': 'Meeting Link (Zoom/Google Meet)',
      'dashboard.liveSubmit': 'Schedule Class',
      'dashboard.uploadRecording': 'Upload Class Recording',
      'dashboard.recordingUrl': 'Recording Video URL',
      'dashboard.submitRecording': 'Submit Recording',
      'courses.title': 'Course Portfolio',
      'courses.create': 'Create New Course',
      'courses.name': 'Course Title',
      'courses.desc': 'Course Description',
      'courses.btnCreate': 'Create Course',
      'courses.assignClasses': 'Assign Classrooms to Course',
      'courses.assigned': 'Assigned Classes',
      'courses.btnAssign': 'Save Curriculum',
      'courses.progress': 'Your Course Progress',
      'courses.certificate': 'Completion Certificate',
      'courses.downloadCert': 'Download PDF Certificate',
      'teacher.bulkUpload': 'Bulk Student CSV Upload',
      'teacher.uploadFile': 'Select CSV File',
      'teacher.uploadBtn': 'Process Bulk Upload',
      'teacher.referralCode': 'Your Referral Link/Code',
      'teacher.referralInvites': 'Referred Tutors',
      'admin.revenueReport': 'Download Revenue Report (PDF)',
      'admin.platformReport': 'Download Platform Report (PDF)',
      'admin.suspendUser': 'Suspend Account',
      'admin.unsuspendUser': 'Unsuspend Account',
      'admin.pricingPlans': 'Manage Pricing Plans',
      'login.title': 'TutorNest Dashboard Login',
      'login.email': 'Email Address',
      'login.password': 'Password',
      'login.btn': 'Sign In',
      'login.referrer': 'Referral Code (Optional)'
    },
    si: {
      'nav.dashboard': 'ප්‍රධාන පුවරුව',
      'nav.liveClasses': 'සජීවී පන්ති',
      'nav.courses': 'පාඨමාලා',
      'nav.assignments': 'පැවරුම්',
      'nav.announcements': 'නිවේදන',
      'nav.analytics': 'විශ්ලේෂණ',
      'nav.billing': 'ගෙවීම් සහ සැලසුම්',
      'nav.profile': 'ගිණුම් සැකසුම්',
      'nav.referrals': 'යොමු කිරීමේ පද්ධතිය',
      'dashboard.welcome': 'නැවත සාදරයෙන් පිළිගනිමු,',
      'dashboard.upcomingLive': 'ඉදිරි සජීවී පන්ති',
      'dashboard.noLive': 'කිසිදු සජීවී පන්තියක් සැලසුම් කර නැත.',
      'dashboard.scheduleLive': 'සජීවී පන්තියක් සැලසුම් කරන්න',
      'dashboard.liveTitle': 'සජීවී පන්ති මාතෘකාව',
      'dashboard.liveDesc': 'විස්තරය',
      'dashboard.liveTime': 'සැලසුම් කළ ආරම්භක වේලාව',
      'dashboard.liveDuration': 'කාලය (විනාඩි)',
      'dashboard.liveLink': 'රැස්වීම් සබැඳිය (Zoom/Google Meet)',
      'dashboard.liveSubmit': 'පන්තිය සැලසුම් කරන්න',
      'dashboard.uploadRecording': 'පන්ති පටිගත කිරීම උඩුගත කරන්න',
      'dashboard.recordingUrl': 'පටිගත කිරීමේ වීඩියෝ සබැඳිය',
      'dashboard.submitRecording': 'පටිගත කිරීම ඉදිරිපත් කරන්න',
      'courses.title': 'පාඨමාලා එකතුව',
      'courses.create': 'නව පාඨමාලාවක් සාදන්න',
      'courses.name': 'පාඨමාලා මාතෘකාව',
      'courses.desc': 'පාඨමාලා විස්තරය',
      'courses.btnCreate': 'පාඨමාලාව සාදන්න',
      'courses.assignClasses': 'පාඨමාලාවට පන්ති කාමර සම්බන්ධ කරන්න',
      'courses.assigned': 'සම්බන්ධිත පන්ති',
      'courses.btnAssign': 'විෂය මාලාව සුරකින්න',
      'courses.progress': 'ඔබේ පාඨමාලා ප්‍රගතිය',
      'courses.certificate': 'සම්පූර්ණ කිරීමේ සහතිකය',
      'courses.downloadCert': 'PDF සහතිකය බාගත කරන්න',
      'teacher.bulkUpload': 'සිසුන් CSV මඟින් තොග වශයෙන් ඇතුළත් කිරීම',
      'teacher.uploadFile': 'CSV ගොනුව තෝරන්න',
      'teacher.uploadBtn': 'තොග උඩුගත කිරීම සකසන්න',
      'teacher.referralCode': 'ඔබේ යොමු කිරීමේ සබැඳිය/කේතය',
      'teacher.referralInvites': 'යොමු කරන ලද ගුරුවරුන්',
      'admin.revenueReport': 'ආදායම් වාර්තාව බාගත කරන්න (PDF)',
      'admin.platformReport': 'වේදිකා වාර්තාව බාගත කරන්න (PDF)',
      'admin.suspendUser': 'ගිණුම අත්හිටුවන්න',
      'admin.unsuspendUser': 'අත්හිටුවීම ඉවත් කරන්න',
      'admin.pricingPlans': 'ගෙවීම් සැලසුම් කළමනාකරණය',
      'login.title': 'TutorNest ප්‍රධාන පුවරුවට ඇතුල් වීම',
      'login.email': 'විද්‍යුත් තැපෑල',
      'login.password': 'මුරපදය',
      'login.btn': 'ඇතුල් වන්න',
      'login.referrer': 'යොමු කිරීමේ කේතය (විකල්ප)'
    }
  };

  setLanguage(lang: string) {
    if (this.translations[lang]) {
      this.currentLangSubject.next(lang);
    }
  }

  getLanguage(): string {
    return this.currentLangSubject.value;
  }

  translate(key: string): string {
    const lang = this.getLanguage();
    return this.translations[lang]?.[key] || this.translations['en']?.[key] || key;
  }
}
