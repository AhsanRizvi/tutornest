const API_URL = 'http://localhost:5299';

async function main() {
  console.log('=== TutorNest Integration Verification Script ===\n');

  const randomSuffix = Math.floor(Math.random() * 1000000);
  const teacherEmail = `teacher_${randomSuffix}@tutornest.com`;
  const teacherPassword = 'Password123!';
  const studentEmail = `student_${randomSuffix}@tutornest.com`;
  const studentPassword = 'Password123!';

  let adminToken = '';
  let teacherToken = '';
  let studentToken = '';
  let classId = '';
  let studentId = '';
  let mcqAssignmentId = '';
  let essayAssignmentId = '';
  let mcqSubmissionId = '';
  let essaySubmissionId = '';
  let announcementId = '';

  // Helper for requests
  async function apiCall(endpoint, method = 'GET', body = null, token = null) {
    const headers = { 'Content-Type': 'application/json' };
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }
    const config = { method, headers };
    if (body) {
      config.body = JSON.stringify(body);
    }
    const response = await fetch(`${API_URL}${endpoint}`, config);
    if (!response.ok) {
      const text = await response.text();
      throw new Error(`API Error on ${method} ${endpoint}: ${response.status} - ${text}`);
    }
    return response.status === 204 ? null : await response.json();
  }

  // 1. Admin login
  console.log('1. Logging in as Admin...');
  const adminLoginRes = await apiCall('/api/Auth/login', 'POST', {
    email: 'admin@tutornest.com',
    password: 'Admin@Password123'
  });
  adminToken = adminLoginRes.token;
  console.log('   Admin logged in successfully.\n');

  // 2. Register Teacher (Admin action)
  console.log(`2. Registering new Teacher (${teacherEmail})...`);
  const registerTeacherRes = await apiCall('/api/Auth/register-teacher', 'POST', {
    email: teacherEmail,
    password: teacherPassword,
    firstName: 'Verified',
    lastName: 'Teacher'
  }, adminToken);
  const teacherId = registerTeacherRes.teacherId;
  console.log(`   Teacher registered successfully (ID: ${teacherId}).\n`);

  // 3. Login as Teacher
  console.log('3. Logging in as Teacher...');
  const teacherLoginRes = await apiCall('/api/Auth/login', 'POST', {
    email: teacherEmail,
    password: teacherPassword
  });
  teacherToken = teacherLoginRes.token;
  console.log('   Teacher logged in successfully.\n');

  // 4. Create a Class Group (Teacher action)
  console.log('4. Creating Class Group "Advanced Calculus"...');
  const createClassRes = await apiCall('/api/Teacher/classes', 'POST', {
    name: 'Advanced Calculus',
    description: 'A study of differential and integral calculus.'
  }, teacherToken);
  classId = createClassRes.id;
  console.log(`   Class created successfully (ID: ${classId}).\n`);

  // 5. Register Student (Teacher action)
  console.log(`5. Registering Student (${studentEmail})...`);
  const registerStudentRes = await apiCall('/api/Auth/register-student', 'POST', {
    email: studentEmail,
    password: studentPassword,
    firstName: 'Verified',
    lastName: 'Student'
  }, teacherToken);
  studentId = registerStudentRes.studentId;
  console.log(`   Student registered successfully (ID: ${studentId}).\n`);

  // 6. Enroll Student in Class (Teacher action)
  console.log('6. Enrolling Student in Class...');
  await apiCall(`/api/Teacher/classes/${classId}/enroll`, 'POST', {
    studentId: studentId
  }, teacherToken);
  console.log('   Student enrolled successfully.\n');

  // 7. Create Assignments (Teacher action)
  console.log('7. Creating MCQ and Essay Assignments...');
  const mcqAsg = await apiCall('/api/Assignment', 'POST', {
    title: 'Calculus MCQ Quiz 1',
    description: 'Solve this 5-mark multiple choice question.',
    dueDate: new Date(Date.now() + 86400000).toISOString(),
    totalMarks: 5,
    classId: classId,
    type: 'MultipleChoice',
    configJson: JSON.stringify({
      options: ['Option A', 'Option B', 'Option C', 'Option D'],
      correctAnswer: 'Option B'
    })
  }, teacherToken);
  mcqAssignmentId = mcqAsg.id;
  console.log(`   MCQ Assignment created (ID: ${mcqAssignmentId}).`);

  const essayAsg = await apiCall('/api/Assignment', 'POST', {
    title: 'Limits and Continuity Essay',
    description: 'Explain the delta-epsilon definition of a limit.',
    dueDate: new Date(Date.now() + 86400000 * 2).toISOString(),
    totalMarks: 10,
    classId: classId,
    type: 'ShortAnswer'
  }, teacherToken);
  essayAssignmentId = essayAsg.id;
  console.log(`   Essay Assignment created (ID: ${essayAssignmentId}).\n`);

  // 8. Create Announcement (Teacher action)
  console.log('8. Creating Bulletin Announcement...');
  const annRes = await apiCall('/api/Announcement', 'POST', {
    title: 'Midterm Date Announced',
    content: 'The midterm exam is scheduled for next Friday at 10 AM. Prepare well.',
    classId: classId
  }, teacherToken);
  announcementId = annRes.id;
  console.log(`   Announcement created (ID: ${announcementId}).\n`);

  // 9. Login as Student
  console.log('9. Logging in as Student...');
  const studentLoginRes = await apiCall('/api/Auth/login', 'POST', {
    email: studentEmail,
    password: studentPassword
  });
  studentToken = studentLoginRes.token;
  console.log('   Student logged in successfully.\n');

  // 10. Student: Check Notice Board & Mark Read
  console.log('10. Fetching Student Notices...');
  const notices = await apiCall('/api/Announcement/student', 'GET', null, studentToken);
  const targetNotice = notices.find(n => n.id === announcementId);
  if (!targetNotice) throw new Error('Target notice not found on student notice board.');
  if (targetNotice.isRead) throw new Error('Expected new notice to be unread.');
  console.log(`    Notice found. Title: "${targetNotice.title}" | Read status: ${targetNotice.isRead}`);

  console.log('    Marking Notice as Read...');
  await apiCall(`/api/Announcement/${announcementId}/read`, 'POST', null, studentToken);
  
  const noticesAfterRead = await apiCall('/api/Announcement/student', 'GET', null, studentToken);
  const targetNoticeAfter = noticesAfterRead.find(n => n.id === announcementId);
  if (!targetNoticeAfter.isRead) throw new Error('Expected notice to be marked as read.');
  console.log(`    Notice read status updated to: ${targetNoticeAfter.isRead}\n`);

  // 11. Student: Fetch Assignments and Submit Answers
  console.log('11. Fetching Assignments as Student...');
  const assignments = await apiCall(`/api/Assignment/class/${classId}`, 'GET', null, studentToken);
  const checkMcq = assignments.find(a => a.id === mcqAssignmentId);
  const checkEssay = assignments.find(a => a.id === essayAssignmentId);
  if (!checkMcq || checkMcq.isSubmitted) throw new Error('MCQ assignment check failed.');
  if (!checkEssay || checkEssay.isSubmitted) throw new Error('Essay assignment check failed.');
  console.log('    Found assigned homework tasks.');

  console.log('    Submitting MCQ answer ("Option B")...');
  const mcqSubmitRes = await apiCall(`/api/Assignment/${mcqAssignmentId}/submit`, 'POST', {
    answerText: 'Option B'
  }, studentToken);
  console.log('    Submitting Essay answer...');
  const essaySubmitRes = await apiCall(`/api/Assignment/${essayAssignmentId}/submit`, 'POST', {
    answerText: 'The delta-epsilon definition states that for every epsilon > 0 there exists delta > 0...'
  }, studentToken);
  console.log('    Submissions complete.\n');

  // 12. Student Check In-App Notifications
  console.log('12. Checking Student Notifications...');
  const notifications = await apiCall('/api/Notification', 'GET', null, studentToken);
  console.log(`    Notifications count: ${notifications.length}`);
  for (const n of notifications) {
    console.log(`    - Notification [Type: ${n.type}]: "${n.message}" | Read: ${n.isRead}`);
  }
  // Mark all read
  await apiCall('/api/Notification/read-all', 'POST', null, studentToken);
  const notificationsAfter = await apiCall('/api/Notification', 'GET', null, studentToken);
  if (notificationsAfter.some(n => !n.isRead)) throw new Error('Expected all notifications to be marked read.');
  console.log('    All notifications marked read successfully.\n');

  // 13. Teacher: Retrieve Submissions & Grade
  console.log('13. Teacher retrieving student submissions...');
  const mcqSubs = await apiCall(`/api/Assignment/${mcqAssignmentId}/submissions`, 'GET', null, teacherToken);
  const essaySubs = await apiCall(`/api/Assignment/${essayAssignmentId}/submissions`, 'GET', null, teacherToken);
  
  const mcqSub = mcqSubs.find(s => s.studentId === studentId);
  const essaySub = essaySubs.find(s => s.studentId === studentId);
  if (!mcqSub || !essaySub) throw new Error('Student submissions not found.');
  mcqSubmissionId = mcqSub.id;
  essaySubmissionId = essaySub.id;

  console.log(`    Grading MCQ (Awarding 5 marks, Correct Answer)...`);
  await apiCall(`/api/Assignment/submission/${mcqSubmissionId}/grade`, 'POST', {
    grade: 5.0,
    feedback: 'Excellent job, correct choice!'
  }, teacherToken);

  console.log(`    Grading Essay (Awarding 8.5 marks)...`);
  await apiCall(`/api/Assignment/submission/${essaySubmissionId}/grade`, 'POST', {
    grade: 8.5,
    feedback: 'Clear and rigorous explanation of limits.'
  }, teacherToken);
  console.log('    Grading complete.\n');

  // 14. Student: Check Graded Results
  console.log('14. Student checking graded assignments...');
  const assignmentsGraded = await apiCall(`/api/Assignment/class/${classId}`, 'GET', null, studentToken);
  const gradedMcq = assignmentsGraded.find(a => a.id === mcqAssignmentId);
  const gradedEssay = assignmentsGraded.find(a => a.id === essayAssignmentId);
  
  console.log(`    MCQ Result - Graded: ${gradedMcq.isGraded} | Score: ${gradedMcq.scoreEarned}/${gradedMcq.totalMarks}`);
  console.log(`    Essay Result - Graded: ${gradedEssay.isGraded} | Score: ${gradedEssay.scoreEarned}/${gradedEssay.totalMarks}`);
  if (!gradedMcq.isGraded || gradedMcq.scoreEarned !== 5) throw new Error('MCQ grading check failed.');
  if (!gradedEssay.isGraded || gradedEssay.scoreEarned !== 8.5) throw new Error('Essay grading check failed.');
  console.log('    Scores verified successfully!\n');

  // 15. Analytics Verification
  console.log('15. Checking Teacher Analytics...');
  const teacherAnalytics = await apiCall('/api/Analytics/teacher', 'GET', null, teacherToken);
  console.log(`    Teacher Analytics payload:`);
  console.log(`    - Classes count in progress: ${teacherAnalytics.classProgress?.length}`);
  console.log(`    - Top performers count: ${teacherAnalytics.topPerformers?.length}`);
  if (teacherAnalytics.topPerformers?.length > 0) {
    const top = teacherAnalytics.topPerformers[0];
    console.log(`      * Top student: ${top.studentName} | Avg Score: ${top.averageScorePercentage}%`);
  }

  console.log('    Checking Admin Analytics...');
  const adminAnalytics = await apiCall('/api/Analytics/admin', 'GET', null, adminToken);
  console.log(`    Admin Analytics payload:`);
  console.log(`    - Total Teachers: ${adminAnalytics.totalTeachers}`);
  console.log(`    - Total Students: ${adminAnalytics.totalStudents}`);
  console.log(`    - Total Videos: ${adminAnalytics.totalVideos}`);
  console.log(`    - Total Submissions: ${adminAnalytics.totalSubmissions}`);
  console.log(`    - Total Classes: ${adminAnalytics.totalClasses}\n`);

  console.log('=== ALL PHASE 2 INTEGRATION VERIFICATIONS PASSED SUCCESSFULLY! ===');
}

main().catch(err => {
  console.error('\n❌ VERIFICATION FAILED:');
  console.error(err);
  process.exit(1);
});
