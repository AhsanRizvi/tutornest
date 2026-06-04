const API_URL = 'http://localhost:5299';

async function run() {
  console.log('=== STARTING PHASE 3 SUBSCRIPTIONS & REPORTS INTEGRATION VERIFICATION ===\n');

  try {
    // 1. Log in as Admin
    console.log('1. Logging in as Admin...');
    const adminLoginRes = await fetch(`${API_URL}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: 'admin@tutornest.com', password: 'Admin@Password123' })
    });
    if (!adminLoginRes.ok) throw new Error('Admin login failed');
    const adminData = await adminLoginRes.json();
    const adminToken = adminData.token;
    console.log('✔ Admin logged in successfully.\n');

    // 2. Register a new Teacher
    const randomSuffix = Math.floor(Math.random() * 10000);
    const teacherEmail = `tutor_val_${randomSuffix}@tutornest.com`;
    const teacherPassword = 'Password@123';
    console.log(`2. Registering new Teacher account: ${teacherEmail}...`);
    const regTeacherRes = await fetch(`${API_URL}/api/auth/register-teacher`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${adminToken}`
      },
      body: JSON.stringify({
        email: teacherEmail,
        password: teacherPassword,
        firstName: 'Validation',
        lastName: 'Tutor'
      })
    });
    if (!regTeacherRes.ok) {
      const err = await regTeacherRes.json();
      throw new Error(`Teacher registration failed: ${err.message}`);
    }
    const teacherRegData = await regTeacherRes.json();
    console.log(`✔ Teacher registered successfully. ID: ${teacherRegData.teacherId}\n`);

    // 3. Log in as Teacher
    console.log('3. Logging in as Teacher...');
    const teacherLoginRes = await fetch(`${API_URL}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: teacherEmail, password: teacherPassword })
    });
    if (!teacherLoginRes.ok) throw new Error('Teacher login failed');
    const teacherData = await teacherLoginRes.json();
    const teacherToken = teacherData.token;
    console.log('✔ Teacher logged in successfully.\n');

    // 4. Verify initial plan is Free and check class limit
    console.log('4. Checking current subscription limits (should be Free)...');
    const statusRes = await fetch(`${API_URL}/api/subscription/my-status`, {
      method: 'GET',
      headers: { 'Authorization': `Bearer ${teacherToken}` }
    });
    if (!statusRes.ok) throw new Error('Failed to retrieve teacher subscription status');
    const statusData = await statusRes.json();
    console.log(`✔ Subscription Status: Plan Name: "${statusData.planName}", Class Limit: ${statusData.classLimit}, Student Limit: ${statusData.studentLimit}`);
    if (statusData.planName !== 'Free') throw new Error('Expected default plan to be Free');
    console.log('');

    // 5. Enforce Class Limit (Limit is 2 classes on Free plan)
    console.log('5. Attempting to create classes on Free Plan...');
    // Create Class 1
    console.log('Creating Class 1...');
    const class1Res = await fetch(`${API_URL}/api/teacher/classes`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${teacherToken}`
      },
      body: JSON.stringify({ name: 'Validation Algebra 1', description: 'Basic Algebra' })
    });
    if (!class1Res.ok) throw new Error('Failed to create Class 1');
    const class1 = await class1Res.json();
    console.log('✔ Class 1 created successfully.');

    // Create Class 2
    console.log('Creating Class 2...');
    const class2Res = await fetch(`${API_URL}/api/teacher/classes`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${teacherToken}`
      },
      body: JSON.stringify({ name: 'Validation Geometry 2', description: 'Basic Geometry' })
    });
    if (!class2Res.ok) throw new Error('Failed to create Class 2');
    console.log('✔ Class 2 created successfully.');

    // Create Class 3 (Should fail)
    console.log('Creating Class 3 (Should exceed limit)...');
    const class3Res = await fetch(`${API_URL}/api/teacher/classes`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${teacherToken}`
      },
      body: JSON.stringify({ name: 'Validation Calculus 3', description: 'Exceeds Limit Class' })
    });
    if (class3Res.status === 403) {
      const err = await class3Res.json();
      console.log(`✔ Create Class 3 blocked as expected. Status 403. Message: "${err.message}"\n`);
    } else {
      throw new Error(`Expected HTTP 403 Class limit exceeded, but got status ${class3Res.status}`);
    }

    // 6. Retrieve active plans and simulate subscription upgrade to Basic plan
    console.log('6. Upgrading Subscription via Mock Checkout...');
    const plansRes = await fetch(`${API_URL}/api/subscription/plans`);
    if (!plansRes.ok) throw new Error('Failed to load plans');
    const plans = await plansRes.json();
    const basicPlan = plans.find(p => p.name === 'Basic');
    if (!basicPlan) throw new Error('Basic plan not found');
    console.log(`Found Basic Plan ID: ${basicPlan.id}. Triggering Sandbox Checkout simulation...`);

    const checkoutRes = await fetch(`${API_URL}/api/payment/mock-checkout`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${teacherToken}`
      },
      body: JSON.stringify({ planId: basicPlan.id })
    });
    if (!checkoutRes.ok) throw new Error('Mock checkout failed');
    const checkoutData = await checkoutRes.json();
    console.log(`✔ Sandbox upgrade successful. Transaction ID: ${checkoutData.transactionId}\n`);

    // 7. Verify upgraded limits and create Class 3
    console.log('7. Verifying upgraded limits (should be Basic)...');
    const upgradedStatusRes = await fetch(`${API_URL}/api/subscription/my-status`, {
      method: 'GET',
      headers: { 'Authorization': `Bearer ${teacherToken}` }
    });
    const upgradedStatus = await upgradedStatusRes.json();
    console.log(`✔ Subscription Status: Plan Name: "${upgradedStatus.planName}", Class Limit: ${upgradedStatus.classLimit}`);
    if (upgradedStatus.planName !== 'Basic' || upgradedStatus.classLimit !== 10) {
      throw new Error('Failed to upgrade plan limits to Basic');
    }

    console.log('Attempting to create Class 3 again under upgraded limits...');
    const class3RetryRes = await fetch(`${API_URL}/api/teacher/classes`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${teacherToken}`
      },
      body: JSON.stringify({ name: 'Validation Calculus 3', description: 'Should succeed now' })
    });
    if (!class3RetryRes.ok) throw new Error('Class 3 creation failed after upgrade');
    console.log('✔ Class 3 created successfully under Basic plan.\n');

    // 8. Download Class PDF Report
    console.log('8. Generating and downloading Class Progress PDF report...');
    const classPdfRes = await fetch(`${API_URL}/api/report/class/${class1.id}/pdf`, {
      method: 'GET',
      headers: { 'Authorization': `Bearer ${teacherToken}` }
    });
    if (!classPdfRes.ok) throw new Error('Failed to generate Class PDF report');
    const pdfBlob = await classPdfRes.blob();
    console.log(`✔ PDF Report downloaded successfully. Blob size: ${pdfBlob.size} bytes.\n`);

    // 9. Download Admin Telemetry PDF Report
    console.log('9. Generating and downloading Admin Platform Telemetry PDF report...');
    const adminPdfRes = await fetch(`${API_URL}/api/report/admin/platform/pdf`, {
      method: 'GET',
      headers: { 'Authorization': `Bearer ${adminToken}` }
    });
    if (!adminPdfRes.ok) throw new Error('Failed to generate Admin Platform PDF report');
    const adminPdfBlob = await adminPdfRes.blob();
    console.log(`✔ Admin PDF Report downloaded successfully. Blob size: ${adminPdfBlob.size} bytes.\n`);

    console.log('=== ALL TESTS COMPLETED SUCCESSFULLY! ===');
    process.exit(0);
  } catch (error) {
    console.error('\n❌ VERIFICATION FAILED:', error.message);
    process.exit(1);
  }
}

run();
