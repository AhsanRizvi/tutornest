const fs = require('fs');
const path = require('path');

const dir = path.join(__dirname, 'src', 'environments');
if (!fs.existsSync(dir)) {
  fs.mkdirSync(dir, { recursive: true });
}

const targetPath = path.join(dir, 'environment.prod.ts');
const apiUrl = process.env.API_URL || 'http://localhost:5299';

const envConfigFile = `// This file is auto-generated during build time by set-env.js.
export const environment = {
  production: true,
  apiUrl: '${apiUrl}'
};
`;

fs.writeFileSync(targetPath, envConfigFile, 'utf8');
console.log(`Angular environment.prod.ts generated with apiUrl: ${apiUrl}`);
