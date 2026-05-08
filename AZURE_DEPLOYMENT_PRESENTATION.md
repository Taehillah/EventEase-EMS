# EventEase EMS Azure Deployment Presentation

## Slide 1: Title

**EventEase EMS**

Azure Publishing, Containerization, and Deployment Breakdown

Prepared from the current repository and live Azure subscription state on March 24, 2026.

---

## Slide 2: Executive Summary

- The application is an ASP.NET Core MVC app targeting `.NET 8`.
- The repository includes a valid multi-stage `Dockerfile` for containerization.
- Azure resources exist for:
  - Azure SQL
  - Azure Container Registry
  - Azure Container Apps Environment
  - Log Analytics
  - Azure VM
- The live app is **currently running on an Azure VM**, not on Azure Container Apps.
- The Azure Container Registry currently has **no repositories/images**, and there is **no deployed Container App**.
- This means the project was prepared for container publishing, but the running production path is a **VM publish + systemd service** deployment.

---

## Slide 3: Application Stack

**Framework**

- ASP.NET Core MVC
- Target framework: `net8.0`

**NuGet Packages**

- `Microsoft.EntityFrameworkCore.Design` `8.0.8`
- `Microsoft.EntityFrameworkCore.InMemory` `8.0.8`
- `Microsoft.EntityFrameworkCore.SqlServer` `8.0.8`

**Azure / Infrastructure Dependencies**

- Azure SQL Database
- Azure VM (Ubuntu)
- Azure NSG / VNet / Public IP
- Azure Container Registry
- Azure Container Apps Environment
- Log Analytics Workspace

---

## Slide 4: Docker Packaging

The repo contains this container strategy in [Dockerfile](/Users/taehillah/EventEase%20EMS/Dockerfile):

- Build stage image: `mcr.microsoft.com/dotnet/sdk:8.0`
- Runtime stage image: `mcr.microsoft.com/dotnet/aspnet:8.0`
- Restore: `dotnet restore`
- Publish: `dotnet publish -c Release -o /app/publish /p:UseAppHost=false`
- Exposed port: `8080`
- Startup command: `dotnet EventEase.EMS.dll`

**What this means**

- The app is container-ready.
- The image produced is a standard ASP.NET Core runtime container.
- The container listens on `8080` internally.

---

## Slide 5: Reproducible Container Build Commands

These commands are the correct local build steps for the containerized version of the app:

```bash
cd "/Users/taehillah/EventEase EMS"

docker build -t eventease-ems:latest .
docker run --rm -p 8080:8080 eventease-ems:latest
```

To inject production configuration into the container:

```bash
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__EventEaseDb="Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<db>;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
  -e AdminUser__Email="<admin-email>" \
  -e AdminUser__Password="<admin-password>" \
  eventease-ems:latest
```

---

## Slide 6: Reproducible ACR Push Commands

These are the standard commands to tag and push the container to the current Azure Container Registry:

```bash
az login
az account set --subscription "Azure subscription 1"

az acr login --name ca982181dbaeacr

docker tag eventease-ems:latest ca982181dbaeacr.azurecr.io/eventease-ems:latest
docker push ca982181dbaeacr.azurecr.io/eventease-ems:latest
```

You can verify the push with:

```bash
az acr repository list --name ca982181dbaeacr --output table
az acr repository show-tags --name ca982181dbaeacr --repository eventease-ems --output table
```

**Important evidence note**

- As of March 24, 2026, `az acr repository list --name ca982181dbaeacr --output table` returned no repositories.
- That means there is no current evidence that an application image was actually pushed into ACR.

---

## Slide 7: If Container Apps Had Been Used

The Azure environment contains:

- Container Apps Environment: `eventease-app-10534346-env`
- ACR: `ca982181dbaeacr`
- Log Analytics Workspace: `workspace-eventeasergeastusLxGS`

The standard deployment command would look like this:

```bash
az containerapp create \
  --name eventease-ems \
  --resource-group eventease-rg-eastus \
  --environment eventease-app-10534346-env \
  --image ca982181dbaeacr.azurecr.io/eventease-ems:latest \
  --target-port 8080 \
  --ingress external \
  --registry-server ca982181dbaeacr.azurecr.io \
  --query properties.configuration.ingress.fqdn
```

For secrets and app settings:

```bash
az containerapp update \
  --name eventease-ems \
  --resource-group eventease-rg-eastus \
  --set-env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__EventEaseDb="Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<db>;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
    AdminUser__Email="<admin-email>" \
    AdminUser__Password="<admin-password>"
```

**Current Azure fact**

- No Container App exists in the resource group today.

---

## Slide 8: What Is Actually Running in Azure

The live deployment is a VM-based publish.

**Verified Azure resources**

- Resource group: `eventease-rg-eastus`
- VM: `eventeasevm10534346za`
- Public IP: `20.164.214.41`
- SQL Server: `eventeasesqlncus10534346`
- Database: `eventease-db`

**Verified runtime model**

- The VM runs `dotnet /var/www/eventease/EventEase.EMS.dll`
- The service is defined in `/etc/systemd/system/eventease.service`
- The app was published into `/var/www/eventease`
- A deployment archive exists locally at `.azure-vm/publish-vm.tar.gz`

This is consistent with a file-based VM publish, not a container deployment.

---

## Slide 9: Actual VM Publish Flow

The running VM deployment can be explained by the following workflow.

### Step 1: Publish the app

```bash
cd "/Users/taehillah/EventEase EMS"

dotnet restore
dotnet publish "EventEase.EMS.csproj" -c Release -o ./publish-vm
```

### Step 2: Package the published output

```bash
tar -czf .azure-vm/publish-vm.tar.gz -C publish-vm .
```

### Step 3: Copy the package to the VM

```bash
scp -i .azure-vm/eventease_vm_key .azure-vm/publish-vm.tar.gz \
  azureuser@20.164.214.41:/tmp/publish-vm.tar.gz
```

### Step 4: Extract to the app directory

```bash
ssh -i .azure-vm/eventease_vm_key azureuser@20.164.214.41 \
  "sudo mkdir -p /var/www/eventease && \
   sudo tar -xzf /tmp/publish-vm.tar.gz -C /var/www/eventease"
```

### Step 5: Create environment configuration

```bash
ssh -i .azure-vm/eventease_vm_key azureuser@20.164.214.41 "sudo tee /etc/eventease.env >/dev/null <<'EOF'
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:8080
ConnectionStrings__EventEaseDb=Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<db>;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
AdminUser__Email=<admin-email>
AdminUser__Password=<admin-password>
EOF"
```

### Step 6: Create the systemd service

```bash
ssh -i .azure-vm/eventease_vm_key azureuser@20.164.214.41 "sudo tee /etc/systemd/system/eventease.service >/dev/null <<'EOF'
[Unit]
Description=EventEase EMS
After=network.target

[Service]
WorkingDirectory=/var/www/eventease
ExecStart=/usr/bin/dotnet /var/www/eventease/EventEase.EMS.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=eventease
User=azureuser
EnvironmentFile=/etc/eventease.env

[Install]
WantedBy=multi-user.target
EOF"
```

### Step 7: Start the service

```bash
ssh -i .azure-vm/eventease_vm_key azureuser@20.164.214.41 \
  "sudo systemctl daemon-reload && \
   sudo systemctl enable eventease.service && \
   sudo systemctl restart eventease.service && \
   sudo systemctl status eventease.service --no-pager"
```

---

## Slide 10: Reverse Proxy and Public Access

The VM originally exposed the app directly on `8080`.

The public setup was improved by placing `nginx` in front of Kestrel:

```bash
ssh -i .azure-vm/eventease_vm_key azureuser@20.164.214.41 \
  "sudo apt-get update && sudo apt-get install -y nginx"
```

`nginx` was configured to proxy:

- Public: `http://20.164.214.41/`
- Backend: `http://127.0.0.1:8080`

Current public access:

```bash
curl -I http://20.164.214.41/Account/Login
```

Current backend isolation:

```bash
curl -I http://20.164.214.41:8080/Account/Login
```

Expected result:

- Port `80`: reachable
- Port `8080`: not publicly reachable

---

## Slide 11: Azure CLI Commands Matching the Current Resource Footprint

The exact historical provisioning commands were not stored in the repo, but the current Azure footprint is consistent with commands like these:

```bash
az group create \
  --name eventease-rg-eastus \
  --location eastus
```

```bash
az sql server create \
  --name eventeasesqlncus10534346 \
  --resource-group eventease-rg-eastus \
  --location northcentralus \
  --admin-user eventeaseadmin \
  --admin-password "<password>"
```

```bash
az sql db create \
  --resource-group eventease-rg-eastus \
  --server eventeasesqlncus10534346 \
  --name eventease-db \
  --service-objective Basic
```

```bash
az sql server firewall-rule create \
  --resource-group eventease-rg-eastus \
  --server eventeasesqlncus10534346 \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

```bash
az acr create \
  --name ca982181dbaeacr \
  --resource-group eventease-rg-eastus \
  --location northcentralus \
  --sku Basic \
  --admin-enabled true
```

```bash
az containerapp env create \
  --name eventease-app-10534346-env \
  --resource-group eventease-rg-eastus \
  --location northcentralus
```

```bash
az vm create \
  --name eventeasevm10534346za \
  --resource-group eventease-rg-eastus \
  --image Ubuntu2204 \
  --admin-username azureuser \
  --ssh-key-values .azure-vm/eventease_vm_key.pub
```

---

## Slide 12: Packages, Images, and Tools Used

**NuGet packages**

- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.InMemory`
- `Microsoft.EntityFrameworkCore.SqlServer`

**Container base images**

- `mcr.microsoft.com/dotnet/sdk:8.0`
- `mcr.microsoft.com/dotnet/aspnet:8.0`

**Linux / runtime tools**

- `dotnet` runtime on Ubuntu VM
- `systemd`
- `nginx`
- `tar`
- `ssh`
- `scp`

**Azure CLI / container tools**

- `az`
- `docker`

---

## Slide 13: Evidence-Based Findings You Should Present

- The repository is container-ready.
- Azure resources for a container deployment were partially prepared.
- The Azure Container Registry exists but currently contains no image repositories.
- The Azure Container Apps Environment exists but currently has no deployed app.
- The live production workload is running from a published `.NET` output on an Azure VM.
- Therefore, the app was **published successfully to Azure**, but the **currently active production deployment is VM-based, not container-based**.

---

## Slide 14: Recommended Talking Points

1. The application was developed as a `.NET 8` MVC solution with Azure SQL support.
2. A multi-stage Docker build was created to containerize the app.
3. Azure infrastructure was prepared for both container-based and VM-based hosting.
4. The current production deployment path uses:
   - Azure SQL for data
   - Azure VM for hosting
   - systemd for process management
   - nginx for public HTTP entry
5. ACR and Container Apps resources exist, but they are not currently hosting the live app.
6. The next modernization step would be to either:
   - complete the container push and deploy to Azure Container Apps, or
   - continue with the VM model and finish HTTPS/domain hardening there.

---

## Slide 15: Appendix - Files Used for This Presentation

- [Dockerfile](/Users/taehillah/EventEase%20EMS/Dockerfile)
- [EventEase.EMS.csproj](/Users/taehillah/EventEase%20EMS/EventEase.EMS.csproj)
- [Program.cs](/Users/taehillah/EventEase%20EMS/Program.cs)
- [README.md](/Users/taehillah/EventEase%20EMS/README.md)
- [.azure-vm/publish-vm.tar.gz](/Users/taehillah/EventEase%20EMS/.azure-vm/publish-vm.tar.gz)
- [publish-vm](/Users/taehillah/EventEase%20EMS/publish-vm)

---

## Presenter Note

If you present this formally, use this wording:

> "The codebase was containerized and is ready for Azure container deployment, but the current live Azure workload is running from a published .NET build on a Linux VM. The ACR and Container Apps environment were provisioned, but the active production path is VM-based."
