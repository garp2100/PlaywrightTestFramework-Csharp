# PlaywrightTestFramework-Csharp
***
#### Playwright test automation suite written in C# for educational purposes.
#### You can recreate this project from scratch by following the [documentation](https://github.com/garp2100/PlaywrightTestFramework-Csharp/blob/main/PLAYWRIGHT-SETUP.md).
#### You can also run it locally following the instructions below:

### 1. Clone
```
git clone https://github.com/garp2100/PlaywrightTestFramework-Csharp.git                                                                                                                                                         
cd PlaywrightTestFramework-Csharp/PlaywrightTestFramework
```
### 2. Restore & Build
```
dotnet restore                                                                                                                                                                                                                   
dotnet build
```
### 3. Install Playwright browsers (required!)
```
# Optional: install PowerShell
dotnet tool install --global PowerShell
  
# Install Playwright browsers 
pwsh bin/Debug/net8.0/playwright.ps1 install
```

### 4. Create .env file (not committed)
```
echo 'DATABASESETTINGS__CONNECTIONSTRING=your-connection-string-here' > .env
```
### 5. Run tests
```
dotnet test                                               
```