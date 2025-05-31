🧰 Local Development Environment This project supports a fully automated
development environment using:

.NET with local tools (dotnet-ef, dotnet-format, etc.)

Docker (via docker-compose)

Terraform for infrastructure as code

Kubernetes (Minikube, EKS, or GKE)

Cloud support for Azure, AWS, GCP

Pulumi for optional multi-cloud deployments

.envrc + direnv for environment variable management

🚀 Quick Start ✅ Prerequisites Ensure you have the following installed:

direnv

.NET SDK

Docker

Terraform

kubectl

Pulumi

AWS CLI, Azure CLI, gcloud CLI

🧪 Environment Setup Enable direnv for auto-loading environment variables:

bash Copy Edit direnv allow Install all tools and configure your dev
environment:

Using make:

bash Copy Edit make init Or using shell script:

bash Copy Edit ./dev.sh all 📦 Available Commands bash Copy Edit make dotnet #
Setup .NET local tools make terraform # Init & validate Terraform make kubectl #
Setup K8s context + namespace make init # Run all setup scripts make clean #
Wipe temp files & local config or:

bash Copy Edit ./dev.sh dotnet ./dev.sh terraform ./dev.sh kubectl ./dev.sh
clean 🔐 Environment Variables Secrets and credentials are managed via:

.envrc for project-level config

.env (gitignored) for sensitive keys

Example .env: env Copy Edit AWS_ACCESS_KEY_ID=your-key
AWS_SECRET_ACCESS_KEY=your-secret AZURE_CLIENT_SECRET=your-secret
GOOGLE_APPLICATION_CREDENTIALS=keys/gcp-service-account.json
PULUMI_ACCESS_TOKEN=your-token 🛠 Local Tools Installed dotnet-ef: Entity
Framework Core CLI

dotnet-format: Code formatter

dotnet-outdated-tool: Dependency audit tool
