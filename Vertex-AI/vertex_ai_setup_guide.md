 📚 Vertex AI Setup & Configuration Guide

This guide outlines the end-to-end steps required to integrate Google Cloud Vertex AI (Gemini 2.5 Flash) into your application, structured perfectly for presentation slides or documentation.

---

 🛠️ Step 1: Google Cloud Console Project Setup
1. Create a Cloud Project:
   - Navigate to [Google Cloud Console](https://console.cloud.google.com).
   - Click the project dropdown and select New Project.
   - Project Name: `accisure`
   - Project ID: `accisure-490603` (assigned automatically).

2. Enable Vertex AI API:
   - In the Search Bar, type "Vertex AI API".
   - Click Enable and associate a billing account. Vertex AI relies on billing activation to access model pipelines.

---

 🔑 Step 2: Authentication & IAM Service Account
1. Locate IAM Credential Scopes:
   - Navigate into IAM & Admin > Service Accounts.
2. Create a Service Account Identifier:
   - Click Create Service Account.
   - Name: `vertex-ai-accessor` or similar.
3. Assign Roles for Minimum Privilege Model Access:
   - Select role: Vertex AI User (allows executing prompt inference queries).
   - Select role: Viewer or Storage Object Viewer (allows downloading blobs if relevant later).
4. Generate Client Keys Access Layouts:
   - Click the created Service Account > Keys tab.
   - Click Add Key > Create New Key.
   - Select JSON format.
   - It will automatically download a file similar to: `accisure-490603-a7fc0ba7649d.json`.

---

 💻 Step 3: Local Environment Configurations
1. Save Credentials:
   - Move the downloaded `.json` key to your project directory.
   - Location: `C:\Sanjay\Vertex-AI\accisure-490603-a7fc0ba7649d.json`

2. Configure `.env` mappings on application roots:
   Add or update these variables inside your `.env` layer:
   ```bash
   GOOGLE_GENAI_USE_VERTEXAI=true
   GOOGLE_CLOUD_PROJECT=accisure-490603
   GOOGLE_CLOUD_LOCATION=us-central1
   GOOGLE_APPLICATION_CREDENTIALS=C:\Sanjay\Vertex-AI\accisure-490603-a7fc0ba7649d.json
   ```

---

 🚀 Step 4: Component Logic Flow Setup
1. SDK Dependency Hooks:
   Install backend drivers over Node stack handlers:
   ```bash
   npm i @google-cloud/vertexai dotenv express
   ```
2. Execution Routings:
   - Inside `app.js`, the standard express routers authenticate automatically feeding `.json` credential references into the Vertex Auth scope context.
   - Prompt engineering endpoints trigger inference layouts over Gemini 2.5 Flash directly, mapping structured parsed analytics back towards clients easily.

---
💡 Presentation Tip: Highlight the Service Account `.json` key as the boundary secure bridging client-to-cloud security token, bypassing manual Google Workspace logins securely for automated API triggers.
