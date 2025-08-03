# 🚨 **Authentication ErrorMessage: CMS Backend Connection Failed**

## ❌ **Current ErrorMessage**

```
ECONNREFUSED - Cannot connect to CMS backend on http://localhost:5000
```

The authentication is failing because the **CMS backend is not running**. Since we removed the development fallback, the
CMS backend is **required** for authentication.

## 🛠️ **How to Fix**

### **Step 1: Start CMS Backend**

Open a **new terminal** in the CMS directory and start the backend:

```bash
# Navigate to CMS directory
cd apps/cms

# Start the CMS backend
dotnet run
```

**Expected Output:**

```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### **Step 2: Verify CMS is Running**

Open your browser and visit: **http://localhost:5000/health**

**Expected Response:**

```json
{
  "status": "healthy",
  "timestamp": "2025-06-19T...",
  "environment": "Development",
  "database": "connected"
}
```

### **Step 3: Test Authentication**

1. Go back to your web app: **http://localhost:3000**
2. Check the "CMS Backend Status" - should show **"Connected"**
3. Click **"Fazer Login com Google"**
4. Authentication should now work with **real CMS tokens**

## 🔍 **Troubleshooting**

### **If CMS Won't Start**

```bash
# Make sure you have .NET 9.0 SDK
dotnet --version

# Clean and restore
cd apps/cms
dotnet clean
dotnet restore
dotnet run
```

### **If Port 5000 is Busy**

```bash
# Check what's using port 5000
netstat -ano | findstr :5000

# Kill the process if needed (replace PID)
taskkill /PID [PID] /F
```

### **If Database Issues**

```bash
# Reset database (delete and recreate)
cd apps/cms
rm app.db
dotnet run
```

## ✅ **What Should Happen**

### **Console Output (Frontend)**

```
Attempting Google ID token validation with CMS backend: http://localhost:5000
CMS backend authentication successful: { userId: ..., tenantId: ..., availableTenants: ... }
```

### **Authentication Flow**

1. ✅ Google OAuth completes
2. ✅ Frontend sends ID token to CMS `/auth/google/id-token`
3. ✅ CMS validates token with Google
4. ✅ CMS creates/finds user in database
5. ✅ CMS returns JWT tokens and tenant data
6. ✅ Frontend creates authenticated session

## 🎯 **Required Services**

For the application to work, you need **both** services running:

### **Terminal 1: CMS Backend**

```bash
cd apps/cms
dotnet run
# Should show: Now listening on: http://localhost:5000
```

### **Terminal 2: Web Frontend**

```bash
cd apps/web
npm run dev
# Should show: Ready - started server on 0.0.0.0:3000
```

## 📋 **Quick Checklist**

- [ ] CMS backend running on localhost:5000
- [ ] Web frontend running on localhost:3000
- [ ] Environment variable `NEXT_PUBLIC_API_URL=http://localhost:5000`
- [ ] Google OAuth credentials configured
- [ ] No other service using port 5000

**Once the CMS backend is running, authentication should work perfectly!** 🚀
