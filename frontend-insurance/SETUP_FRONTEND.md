# 🚀 Frontend Setup Guide (Angular)

Welcome to the **Frontend** setup guide! This guide will walk you through setting up the frontend Angular application from scratch, installing all necessary packages, and running the development server.

## 🛠️ Prerequisites
Ensure you have the following installed on your system before proceeding:
- **Node.js**: v18.0.0 or higher
- **npm**: v11.0.0 or higher
- **Angular CLI**: `@angular/cli@21.2.0`

> [!IMPORTANT]
> If you haven't installed Angular CLI globally yet, you can do so by running:
> ```bash
> npm install -g @angular/cli
> ```

---

## 📦 Package Installation

Navigate into the frontend project directory (`c:\Sanjay\frontend-insurance`) and run the following command to install all dependencies defined in the `package.json` file:

```bash
npm install
```

### Key Dependencies Breakdown
Below are the critical packages that power the frontend along with the command to install them individually if you are setting up a fresh project:

#### 1. Core Framework & UI
- **Angular Core & CLI**: The foundational framework.
- **Tailwind CSS**: For utility-first styling.
```bash
npm install @angular/core @angular/common @angular/forms @angular/router
npm install -D tailwindcss @tailwindcss/postcss postcss
```

#### 2. Real-Time Communication
- **SignalR**: For real-time updates and notifications from the .NET backend.
```bash
npm install @microsoft/signalr
```

#### 3. Charts & Analytics
- **Chart.js**: For rendering the beautiful, interactive dashboard charts.
```bash
npm install chart.js
```

#### 4. Document Processing (KYC & Policies)
- **Tesseract.js**: For Optical Character Recognition (OCR) to read text from Aadhar cards.
- **PDF.js**: For rendering PDF documents.
- **jsPDF & html2canvas**: For generating downloadable PDF policy documents.
```bash
npm install tesseract.js pdfjs-dist jspdf jspdf-autotable html2canvas html-to-image
```

#### 5. User Onboarding
- **Driver.js**: For the interactive "Product Tour" on the dashboard.
```bash
npm install driver.js
```

---

## 🚀 Running the Application

Once all dependencies are installed, you can spin up the local development server.

### 1. Start the Development Server
Run the standard Angular serve command:
```bash
npm start
```
*Alternatively, you can run:* `ng serve`

### 2. Access the Application
Open your browser and navigate to:
[http://localhost:4200](http://localhost:4200)

> [!TIP]
> The app is configured to talk to the local .NET backend at `https://localhost:7140/api` in development mode. Make sure your backend is running simultaneously for the full experience!

---

## 🏗️ Building for Production
When you're ready to deploy to production, generate the optimized build by running:
```bash
npm run build
```
This will compile the application into the `dist/` directory, ready to be hosted on any static file server or cloud provider.
