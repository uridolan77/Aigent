# Agent System Deployment and Operations Guide

## Overview

This guide provides comprehensive instructions for deploying and operating the Agent System in production environments.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Local Development](#local-development)
3. [Docker Deployment](#docker-deployment)
4. [Kubernetes Deployment](#kubernetes-deployment)
5. [Cloud Deployment](#cloud-deployment)
6. [Configuration](#configuration)
7. [Monitoring](#monitoring)
8. [Scaling](#scaling)
9. [Backup and Recovery](#backup-and-recovery)
10. [Troubleshooting](#troubleshooting)
11. [Security](#security)
12. [Maintenance](#maintenance)

## Prerequisites

### Required Tools
- .NET 6.0 SDK or later
- Docker Desktop (for containerization)
- kubectl (for Kubernetes deployment)
- Helm 3 (for Kubernetes package management)
- Azure CLI (for Azure deployment)
- Terraform (for infrastructure as code)

### System Requirements
- **CPU**: Minimum 4 cores, recommended 8 cores
- **RAM**: Minimum 8GB, recommended 16GB
- **Storage**: Minimum 50GB SSD
- **Network**: Stable internet connection with low latency

## Local Development

### 1. Clone the Repository
```bash
git clone https://github.com/your-org/agent-system.git
cd agent-system
```

### 2. Build the Solution
```bash
dotnet restore
dotnet build
```

### 3. Run Tests
```bash
dotnet test --verbosity normal
```

### 4. Run Locally
```bash
dotnet run --project src/AgentSystem.Api
```

### 5. Access the Application
- API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- Health Check: http://localhost:5000/health
- Health UI: http://localhost:5000/health-ui

## Docker Deployment

### 1. Build Docker Image
```bash
docker build -t agent-system:latest .
```

### 2. Run with Docker Compose
```bash
docker-compose up -d
```

### 3. View Logs
```bash
docker-compose logs -f agent-system
```

### 4. Stop Services
```bash
docker-compose down
```

## Kubernetes Deployment

### 1. Create Namespace
```bash
kubectl create namespace agent-system
```

### 2. Create Secrets
```bash
kubectl create secret generic agent-secrets \
  --from-literal=redis-connection="your-redis-connection" \
  --from-literal=sql-connection="your-sql-connection" \
  --from-literal=app-insights-key="your-app-insights-key" \
  --namespace=agent-system
```

### 3. Deploy with Helm
```bash
helm install agent-system ./helm-chart \
  --namespace agent-system \
  --values ./helm-chart/values.yaml
```

### 4. Verify Deployment
```bash
kubectl get pods -n agent-system
kubectl get services -n agent-system
```

### 5. Access the Application
```bash
kubectl port-forward service/agent-system-service 8080:80 -n agent-system
```

### 6. Update Deployment
```bash
helm upgrade agent-system ./helm-chart \
  --namespace agent-system \
  --values ./helm-chart/values.yaml
```

## Cloud Deployment

### Azure Deployment

#### 1. Login to Azure
```bash
az login
```

#### 2. Create Resource Group
```bash
az group create --name agent-system-rg --location eastus
```

#### 3. Deploy with Terraform
```bash
cd terraform
terraform init
terraform plan -out=tfplan
terraform apply tfplan
```

#### 4. Get AKS Credentials
```bash
az aks get-credentials --resource-group agent-system-rg --name agent-system-aks
```

#### 5. Deploy Application
```bash
kubectl apply -f k8s/
```

### AWS Deployment

#### 1. Configure AWS CLI
```bash
aws configure
```

#### 2. Deploy EKS Cluster
```bash
eksctl create cluster --name agent-system --region us-east-1
```

#### 3. Deploy Application
```bash
kubectl apply -f k8s/
```

## Configuration

### Environment Variables
```bash
# Core Settings
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80

# Agent System Settings
AgentSystem__MemoryType=Redis
AgentSystem__Redis__ConnectionString=redis:6379
AgentSystem__SQL__ConnectionString=Server=sql;Database=AgentSystem;
AgentSystem__Monitoring__Type=ApplicationInsights
AgentSystem__Monitoring__InstrumentationKey=your-key

# Security Settings
AgentSystem__KeyVault__Url=https://your-keyvault.vault.azure.net/
AgentSystem__OAuth__ClientId=your-client-id
AgentSystem__OAuth__ClientSecret=your-client-secret
```

### Configuration Files
- `appsettings.json`: Base configuration
- `appsettings.Production.json`: Production-specific settings
- `appsettings.Development.json`: Development settings

### Feature Flags
```json
{
  "Features": {
    "NewMLModel": {
      "Enabled": true,
      "Configuration": {
        "ModelPath": "models/v2/",
        "Threshold": 0.85
      }
    }
  }
}
```

## Monitoring

### Application Insights
1. Create Application Insights resource in Azure
2. Configure instrumentation key
3. View metrics and logs in Azure Portal

### Prometheus & Grafana
1. Deploy Prometheus
```bash
helm install prometheus prometheus-community/prometheus
```

2. Deploy Grafana
```bash
helm install grafana grafana/grafana
```

3. Import dashboards from `monitoring/dashboards/`

### Health Checks
- `/health`: Overall system health
- `/health/ready`: Readiness probe
- `/health/live`: Liveness probe

### Key Metrics
- **Response Time**: P50, P95, P99
- **Throughput**: Requests per second
- **Error Rate**: Failed requests percentage
- **Agent Performance**: Decision time, success rate
- **Resource Usage**: CPU, Memory, Network

## Scaling

### Horizontal Scaling
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: agent-system-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: agent-system
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
```

### Vertical Scaling
Adjust resource limits in deployment:
```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "2Gi"
    cpu: "2000m"
```

### Database Scaling
- **Redis**: Use Redis Cluster for horizontal scaling
- **SQL**: Use read replicas for read-heavy workloads

## Backup and Recovery

### Database Backup
```bash
# Redis backup
redis-cli --rdb dump.rdb

# SQL backup
sqlcmd -S server -U user -P password -Q "BACKUP DATABASE AgentSystem TO DISK='backup.bak'"
```

### Kubernetes Backup
```bash
# Backup all resources
kubectl get all --all-namespaces -o yaml > backup.yaml

# Backup specific namespace
kubectl get all -n agent-system -o yaml > agent-system-backup.yaml
```

### Disaster Recovery
1. Regular automated backups
2. Cross-region replication
3. Documented recovery procedures
4. Regular DR drills

## Troubleshooting

### Common Issues

#### 1. Pod Not Starting
```bash
kubectl describe pod <pod-name> -n agent-system
kubectl logs <pod-name> -n agent-system
```

#### 2. Connection Issues
```bash
# Test Redis connection
redis-cli -h redis-service -p 6379 ping

# Test SQL connection
sqlcmd -S sql-service -U sa -P password -Q "SELECT 1"
```

#### 3. Performance Issues
```bash
# Check resource usage
kubectl top pods -n agent-system
kubectl top nodes

# Check application logs
kubectl logs -f deployment/agent-system -n agent-system
```

### Debugging Tools
```bash
# Access pod shell
kubectl exec -it <pod-name> -n agent-system -- /bin/bash

# Port forwarding for debugging
kubectl port-forward <pod-name> 8080:80 -n agent-system
```

## Security

### Best Practices
1. Use RBAC for Kubernetes access
2. Store secrets in Key Vault
3. Enable network policies
4. Use TLS for all communications
5. Regular security audits
6. Implement pod security policies

### Security Checklist
- [ ] All secrets stored securely
- [ ] HTTPS enabled
- [ ] Authentication configured
- [ ] Authorization policies in place
- [ ] Network policies configured
- [ ] Regular vulnerability scans
- [ ] Audit logging enabled

## Maintenance

### Regular Tasks
1. **Daily**
   - Monitor health checks
   - Review error logs
   - Check resource usage

2. **Weekly**
   - Review performance metrics
   - Update dependencies
   - Run security scans

3. **Monthly**
   - Performance optimization
   - Capacity planning
   - DR drill

4. **Quarterly**
   - Architecture review
   - Security audit
   - Technology updates

### Upgrade Procedures
```bash
# Rolling update
kubectl set image deployment/agent-system agent-system=agent-system:v2.0.0 -n agent-system

# Rollback if needed
kubectl rollout undo deployment/agent-system -n agent-system
```

### Log Management
```bash
# View logs
kubectl logs -f deployment/agent-system -n agent-system

# Log aggregation with ELK
kubectl apply -f monitoring/elasticsearch.yaml
kubectl apply -f monitoring/kibana.yaml
kubectl apply -f monitoring/filebeat.yaml
```

## Support

### Resources
- Documentation: https://docs.agentsystem.dev
- Issues: https://github.com/your-org/agent-system/issues
- Wiki: https://github.com/your-org/agent-system/wiki
- Community: https://discord.gg/agent-system

### Contact
- **Development Team**: dev@agentsystem.dev
- **Operations Team**: ops@agentsystem.dev
- **Security Team**: security@agentsystem.dev
- **24/7 Support**: support@agentsystem.dev

### SLA
- **Availability**: 99.9% uptime
- **Response Time**: < 500ms P95
- **Support Response**: < 4 hours for critical issues

## Appendix

### A. Environment Setup Scripts
```bash
#!/bin/bash
# setup-environment.sh

# Install prerequisites
sudo apt-get update
sudo apt-get install -y docker.io kubectl helm

# Configure kubectl
mkdir -p ~/.kube
cp kubeconfig ~/.kube/config

# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

echo "Environment setup complete!"
```

### B. Monitoring Scripts
```bash
#!/bin/bash
# health-check.sh

# Check pod status
kubectl get pods -n agent-system

# Check service endpoints
kubectl get endpoints -n agent-system

# Test health endpoint
curl -f http://agent-system/health || exit 1

echo "Health check passed!"
```

### C. Backup Script
```bash
#!/bin/bash
# backup.sh

DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/backups/$DATE"

mkdir -p $BACKUP_DIR

# Backup Kubernetes resources
kubectl get all -n agent-system -o yaml > $BACKUP_DIR/k8s-resources.yaml

# Backup Redis
redis-cli --rdb $BACKUP_DIR/redis-dump.rdb

# Backup SQL
sqlcmd -S sql-server -U sa -P $SQL_PASSWORD -Q "BACKUP DATABASE AgentSystem TO DISK='$BACKUP_DIR/sql-backup.bak'"

echo "Backup completed: $BACKUP_DIR"
```

---

**Last Updated**: May 2024  
**Version**: 1.0.0  
**Maintained by**: Agent System Operations Team
