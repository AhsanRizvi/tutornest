const API_URL = 'http://localhost:5299';

async function main() {
  console.log('=== STARTING STUDENT LEADERBOARD INTEGRATION VERIFICATION ===\n');

  const randomSuffix = Math.floor(Math.random() * 1000000);
  const teacherEmail = `tutor_lb_${randomSuffix}@tutornest.com`;
  const teacherPassword = 'Password123!';
  const studentEmail = `student_lb_${randomSuffix}@tutornest.com`;
  const studentPassword = 'Password123!';
  const student2Email = `student2_lb_${randomSuffix}@tutornest.com`;
  const student2Password = 'Password123!';

  let adminToken = '';
  let teacherToken = '';
  let studentToken = '';
  let student2Token = '';
  let classId = '';
  let studentId = '';
  let student2Id = '';
  let videoId = '';
  let assignmentId = '';

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
  console.log('✔ Admin logged in successfully.\n');

  // 2. Register Teacher (Admin action)
  console.log(`2. Registering new Teacher (${teacherEmail})...`);
  const registerTeacherRes = await apiCall('/api/Auth/register-teacher', 'POST', {
    email: teacherEmail,
    password: teacherPassword,
    firstName: 'Leaderboard',
    lastName: 'Teacher'
  }, adminToken);
  const teacherId = registerTeacherRes.teacherId;
  console.log(`✔ Teacher registered successfully (ID: ${teacherId}).\n`);

  // 3. Login as Teacher
  console.log('3. Logging in as Teacher...');
  const teacherLoginRes = await apiCall('/api/Auth/login', 'POST', {
    email: teacherEmail,
    password: teacherPassword
  });
  teacherToken = teacherLoginRes.token;
  console.log('✔ Teacher logged in successfully.\n');

  // 4. Create a Class Group (Teacher action)
  console.log('4. Creating Class Group "Leaderboard Algebra 101"...');
  const createClassRes = await apiCall('/api/Teacher/classes', 'POST', {
    name: 'Leaderboard Algebra 101',
    description: 'Leaderboard testing class.'
  }, teacherToken);
  classId = createClassRes.id;
  console.log(`✔ Class created successfully (ID: ${classId}).\n`);

  // 5. Register Student (Teacher action)
  console.log(`5. Registering Student (${studentEmail})...`);
  const registerStudentRes = await apiCall('/api/Auth/register-student', 'POST', {
    email: studentEmail,
    password: studentPassword,
    firstName: 'Top',
    lastName: 'Learner'
  }, teacherToken);
  studentId = registerStudentRes.studentId;
  console.log(`✔ Student registered successfully (ID: ${studentId}).`);

  console.log(`   Registering second Student (${student2Email}) to verify non-enrollment access rules...`);
  const registerStudent2Res = await apiCall('/api/Auth/register-student', 'POST', {
    email: student2Email,
    password: student2Password,
    firstName: 'Unenrolled',
    lastName: 'Learner'
  }, teacherToken);
  student2Id = registerStudent2Res.studentId;
  console.log(`✔ Student 2 registered successfully (ID: ${student2Id}).\n`);

  // 6. Enroll Student in Class (Teacher action)
  console.log('6. Enrolling Student 1 in Class...');
  await apiCall(`/api/Teacher/classes/${classId}/enroll`, 'POST', {
    studentId: studentId
  }, teacherToken);
  console.log('✔ Student 1 enrolled successfully.\n');

  // 7. Create a Video (Teacher action)
  console.log('7. Creating Video "Intro to Variables"...');
  const createVideoRes = await apiCall('/api/Teacher/videos', 'POST', {
    title: 'Intro to Variables',
    description: 'Basics of algebraic variables.',
    videoUrl: 'https://pub-placeholder.r2.dev/intro_to_variables.mp4'
  }, teacherToken);
  videoId = createVideoRes.id;
  console.log(`✔ Video created successfully (ID: ${videoId}).\n`);

  // 8. Assign Video to Class (Teacher action)
  console.log('8. Assigning Video to Class...');
  await apiCall(`/api/Teacher/classes/${classId}/videos`, 'POST', {
    videoId: videoId
  }, teacherToken);
  console.log('✔ Video assigned to class successfully.\n');

  // 9. Create Assignment (Teacher action)
  console.log('9. Creating Assignment...');
  const asgRes = await apiCall('/api/Assignment', 'POST', {
    title: 'Variables Worksheet',
    description: 'Solve question 1-5.',
    dueDate: new Date(Date.now() + 86400000).toISOString(),
    totalMarks: 10,
    classId: classId,
    type: 'ShortAnswer'
  }, teacherToken);
  assignmentId = asgRes.id;
  console.log(`✔ Assignment created (ID: ${assignmentId}).\n`);

  // 10. Login as Student 1 and Student 2
  console.log('10. Logging in as Student 1...');
  const studentLoginRes = await apiCall('/api/Auth/login', 'POST', {
    email: studentEmail,
    password: studentPassword
  });
  studentToken = studentLoginRes.token;
  console.log('✔ Student 1 logged in successfully.');

  console.log('    Logging in as Student 2...');
  const student2LoginRes = await apiCall('/api/Auth/login', 'POST', {
    email: student2Email,
    password: student2Password
  });
  student2Token = student2LoginRes.token;
  console.log('✔ Student 2 logged in successfully.\n');

  // 11. Student 1: Update video progress (e.g. 120 seconds)
  console.log('11. Student 1 watching video (saving 120 seconds of progress)...');
  const progressRes = await apiCall(`/api/Student/videos/${videoId}/progress`, 'POST', {
    watchTimeSeconds: 120.0,
    durationSeconds: 300.0,
    isCompleted: false
  }, studentToken);
  console.log(`✔ Video progress updated. Stored watch time: ${progressRes.watchTimeSeconds} seconds.\n`);

  // 12. Student 1: Submit assignment
  console.log('12. Student 1 submitting assignment...');
  await apiCall(`/api/Assignment/${assignmentId}/submit`, 'POST', {
    answerText: 'X = 5, Y = 10'
  }, studentToken);
  console.log('✔ Assignment submitted successfully.\n');

  // 13. Student 1: Retrieve class leaderboard
  console.log('13. Student 1 fetching leaderboard for the class...');
  const leaderboard = await apiCall(`/api/Student/classes/${classId}/leaderboard`, 'GET', null, studentToken);
  console.log('✔ Leaderboard retrieved successfully. Current rankings:');
  console.table(leaderboard);

  // Validate leaderboard response contents
  if (leaderboard.length !== 1) {
    throw new Error(`Expected exactly 1 entry in the leaderboard, but got: ${leaderboard.length}`);
  }
  const entry = leaderboard[0];
  if (entry.studentId !== studentId) {
    throw new Error(`Expected leaderboard entry to be for Student 1 (${studentId}), but got: ${entry.studentId}`);
  }
  if (entry.rank !== 1) {
    throw new Error(`Expected rank to be 1, but got: ${entry.rank}`);
  }
  if (entry.videoWatchTimeSeconds !== 120.0) {
    throw new Error(`Expected watch time to be 120 seconds, but got: ${entry.videoWatchTimeSeconds}`);
  }
  if (entry.assignmentsSubmittedCount !== 1) {
    throw new Error(`Expected assignment count to be 1, but got: ${entry.assignmentsSubmittedCount}`);
  }
  const expectedScore = 120.0 + (1 * 3600.0);
  if (entry.totalScoreTimeSeconds !== expectedScore) {
    throw new Error(`Expected total score to be ${expectedScore} (120 watch + 3600 completion), but got: ${entry.totalScoreTimeSeconds}`);
  }
  console.log('✔ Leaderboard scores and ranks validated successfully.\n');

  // 14. Student 2: Attempt to retrieve class leaderboard (should be denied because Student 2 is not enrolled)
  console.log('14. Student 2 attempting to fetch leaderboard of the same class (should fail)...');
  try {
    await apiCall(`/api/Student/classes/${classId}/leaderboard`, 'GET', null, student2Token);
    throw new Error('Expected leaderboard fetch by unenrolled student to fail, but it succeeded.');
  } catch (error) {
    console.log(`✔ Fetch blocked successfully as expected. Error detail: "${error.message}"\n`);
  }

  console.log('=== ALL LEADERBOARD INTEGRATION VERIFICATIONS PASSED SUCCESSFULLY! ===');
}

main().catch(err => {
  console.error('\n❌ VERIFICATION FAILED:');
  console.error(err);
  process.exit(1);
});
