# 🚀 Azure Deployment Guide for Vertex-AI

This guide explains how to deploy your `Vertex-AI` application to **Azure App Service** securely, without exposing your `.env` or `.json` secret files.

---

## 🛑 DO NOT Upload These Files
Do **NOT** upload or commit the following files to Azure or Git:
1.  `.env`
2.  `accisure-490603-a7fc0ba7649d.json` (GCP Service Account Key)

---

## 🛠️ Step 1: Create Azure App Service
1. Go to the [Azure Portal](https://portal.azure.com).
2. Create a new **App Service**.
3. **Runtime stack**: `Python 3.10` or higher (e.g., `Python 3.11`).
4. **Operating System**: `Linux` (Recommended).
5. Choose your plan/region.

---

## 🔐 Step 2: Configure Environment Variables (Secrets)
Instead of uploading a `.env` file, we use Azure **Environment Variables** (Application Settings).

1. In Azure Portal, go to your **App Service** page.
2. In the left menu, select **Configuration** (under Settings).
3. Click **+ New application setting** and add the following keys:

| Key | Value | Notes |
| :--- | :--- | :--- |
| `GOOGLE_CLOUD_PROJECT` | `accisure-490603` | Your GCP Project ID |
| `GOOGLE_CLOUD_LOCATION`| `us-central1` | Your GCP Region |
| `GCP_SERVICE_ACCOUNT_JSON` | `{"type": "service_account", ...}` | **Open your `.json` file and copy the ENTIRE text block into this value.** |
| `AGENT_NAME` | `Agent_policies_analysis` | From your `.env` |
| `AGENT_MODEL` | `gemini-2.5-flash` | From your `.env` |
| `GOOGLE_GENAI_USE_VERTEXAI` | `true` | From your `.env` |

4. Click **Save** at the top.

---

## ⚙️ Step 3: Startup Command for FastAPI
FastAPI requires a startup command in Azure App Service to run correctly.

1. In your **App Service**, go to **Configuration** -> **General Settings**.
2. Find **Startup Command**.
3. Enter the following:
   ```bash
   gunicorn -w 4 -k uvicorn.workers.UvicornWorker app:app
   ```
4. Click **Save**.

---

## 📦 Step 4: Deploy Your Code
You can deploy using GitHub Actions, Azure CLI, or local Git deployment.
Azure will automatically:
1. Detect `requirements.txt` and install dependencies (`fastapi`, `uvicorn`, `PyMuPDF`, etc.).
2. Run your application using the Startup Command you provided.

Your application will now load the GCP credentials safely from the `GCP_SERVICE_ACCOUNT_JSON` string we configured.
