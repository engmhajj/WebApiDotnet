
# Makefile for .NET + Docker + Terraform + K8s + Cloud Dev Environment

.PHONY: all init dotnet terraform kubectl clean help

all: init

init: dotnet terraform kubectl
	@echo "✅ Full dev environment initialized."

dotnet:
	@echo "🛠️  Setting up .NET tools..."
	@./init-dotnet-tools.sh

terraform:
	@echo "🌍 Initializing Terraform..."
	@./init-terraform.sh

kubectl:
	@echo "☸️  Configuring Kubernetes context..."
	@./init-kubectl.sh

clean:
	@echo "🧹 Cleaning up dev environment..."
	@rm -rf .terraform terraform.tfstate* .dotnet .config .kube

help:
	@echo ""
	@echo "Available commands:"
	@echo "  make dotnet      - Set up .NET local tools"
	@echo "  make terraform   - Init and validate Terraform"
	@echo "  make kubectl     - Configure K8s context/namespace"
	@echo "  make init        - Run all the above"
	@echo "  make clean       - Remove temporary and build files"
