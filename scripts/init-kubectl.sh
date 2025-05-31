#!/usr/bin/env bash

echo "🔧 Setting up Kubernetes context..."

KUBE_CONTEXT=${KUBE_CONTEXT:-minikube}
KUBE_NAMESPACE=${KUBE_NAMESPACE:-dev}

# Set current context
kubectl config use-context "$KUBE_CONTEXT"

# Create namespace if it doesn't exist
kubectl get namespace "$KUBE_NAMESPACE" >/dev/null 2>&1 ||
    kubectl create namespace "$KUBE_NAMESPACE"

# Set the default namespace for the current context
kubectl config set-context --current --namespace="$KUBE_NAMESPACE"

echo "✅ Kubernetes is ready (context: $KUBE_CONTEXT, namespace: $KUBE_NAMESPACE)"
