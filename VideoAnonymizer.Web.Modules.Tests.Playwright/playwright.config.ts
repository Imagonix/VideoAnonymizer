import { defineConfig } from '@playwright/test';
import { defineBddConfig } from 'playwright-bdd';

const testDir = defineBddConfig({
    features: ['features/**/*.feature'],
    steps: ['features/**/*.ts']
});

export default defineConfig({
	fullyParallel: true,
    workers: 4,
    testDir,
    timeout: 30_000,
    use: {
        baseURL: 'http://localhost:5217',
        headless: false,
        trace: 'on-first-retry'
    },
    projects: [
        {
            name: 'chromium',
            use: { browserName: 'chromium' }
        }
    ]
});