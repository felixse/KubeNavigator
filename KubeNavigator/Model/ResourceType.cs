using k8s.Models;

namespace KubeNavigator.Model;

// todo this should be called resourcetypeinfo
public record ResourceType(string Group, string Version, string Plural, bool IsNamespaceScoped, string PluralDisplayName, string SingularDisplayName)
{
    public static ResourceType Node => new(V1Node.KubeGroup, V1Node.KubeApiVersion, V1Node.KubePluralName, IsNamespaceScoped: false, PluralDisplayName: "Nodes", SingularDisplayName: "Node");
    public static ResourceType Namespace => new(V1Namespace.KubeGroup, V1Namespace.KubeApiVersion, V1Namespace.KubePluralName, IsNamespaceScoped: false, PluralDisplayName: "Namespaces", SingularDisplayName: "Namespace");
    public static ResourceType Pod => new(V1Pod.KubeGroup, V1Pod.KubeApiVersion, V1Pod.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Pods", SingularDisplayName: "Pod");
    public static ResourceType Deployment => new(V1Deployment.KubeGroup, V1Deployment.KubeApiVersion, V1Deployment.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Deployments", SingularDisplayName: "Deployment");
    public static ResourceType DaemonSet => new(V1DaemonSet.KubeGroup, V1DaemonSet.KubeApiVersion, V1DaemonSet.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Daemon Sets", SingularDisplayName: "Daemon Set");
    public static ResourceType StatefulSet => new(V1StatefulSet.KubeGroup, V1StatefulSet.KubeApiVersion, V1StatefulSet.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Stateful Sets", SingularDisplayName: "Stateful Set");
    public static ResourceType ReplicaSet => new(V1ReplicaSet.KubeGroup, V1ReplicaSet.KubeApiVersion, V1ReplicaSet.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Replica Sets", SingularDisplayName: "Replica Set");
    public static ResourceType ReplicationController => new(V1ReplicationController.KubeGroup, V1ReplicationController.KubeApiVersion, V1ReplicationController.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Replication Controllers", SingularDisplayName: "Replication Controller");
    public static ResourceType Job => new(V1Job.KubeGroup, V1Job.KubeApiVersion, V1Job.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Jobs", SingularDisplayName: "Job");
    public static ResourceType CronJob => new(V1CronJob.KubeGroup, V1CronJob.KubeApiVersion, V1CronJob.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Cron Jobs", SingularDisplayName: "Cron Job");
    public static ResourceType ConfigMap => new(V1ConfigMap.KubeGroup, V1ConfigMap.KubeApiVersion, V1ConfigMap.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Config Maps", SingularDisplayName: "Config Map");
    public static ResourceType Secret => new(V1Secret.KubeGroup, V1Secret.KubeApiVersion, V1Secret.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Secrets", SingularDisplayName: "Secret");
    public static ResourceType ResourceQuota => new(V1ResourceQuota.KubeGroup, V1ResourceQuota.KubeApiVersion, V1ResourceQuota.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Resource Quotas", SingularDisplayName: "Resource Quota");
    public static ResourceType LimitRange => new(V1LimitRange.KubeGroup, V1LimitRange.KubeApiVersion, V1LimitRange.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Limit Ranges", SingularDisplayName: "Limit Range");
    public static ResourceType HorizontalPodAutoscaler => new(V1HorizontalPodAutoscaler.KubeGroup, V1HorizontalPodAutoscaler.KubeApiVersion, V1HorizontalPodAutoscaler.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Horizontal Pod Autoscalers", SingularDisplayName: "Horizontal Pod Autoscaler");
    public static ResourceType PodDisruptionBudget => new(V1PodDisruptionBudget.KubeGroup, V1PodDisruptionBudget.KubeApiVersion, V1PodDisruptionBudget.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Pod Disruption Budgets", SingularDisplayName: "Pod Disruption Budget");
    public static ResourceType PriorityClass => new(V1PriorityClass.KubeGroup, V1PriorityClass.KubeApiVersion, V1PriorityClass.KubePluralName, IsNamespaceScoped: false, PluralDisplayName: "Priority Classes", SingularDisplayName: "Priority Class");
    public static ResourceType RuntimeClass => new(V1RuntimeClass.KubeGroup, V1RuntimeClass.KubeApiVersion, V1RuntimeClass.KubePluralName, IsNamespaceScoped: false, PluralDisplayName: "Runtime Classes", SingularDisplayName: "Runtime Class");
    public static ResourceType Lease => new(V1Lease.KubeGroup, V1Lease.KubeApiVersion, V1Lease.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Leases", SingularDisplayName: "Lease");
    public static ResourceType MutatingWebhookConfiguration => new(V1MutatingWebhookConfiguration.KubeGroup, V1MutatingWebhookConfiguration.KubeApiVersion, V1MutatingWebhookConfiguration.KubePluralName, IsNamespaceScoped: false, PluralDisplayName: "Mutating Webhook Configs", SingularDisplayName: "Mutating Webhook Config");
    public static ResourceType ValidatingWebhookConfiguration => new(V1ValidatingWebhookConfiguration.KubeGroup, V1ValidatingWebhookConfiguration.KubeApiVersion, V1ValidatingWebhookConfiguration.KubePluralName, IsNamespaceScoped: false, PluralDisplayName: "Validating Webhook Configs", SingularDisplayName: "Validating Webhook Config");
    public static ResourceType Service => new(V1Service.KubeGroup, V1Service.KubeApiVersion, V1Service.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Services", SingularDisplayName: "Service");
    public static ResourceType Endpoint => new(V1Endpoints.KubeGroup, V1Endpoints.KubeApiVersion, V1Endpoints.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Endpoints", SingularDisplayName: "Endpoint");
    public static ResourceType Ingress => new(V1Ingress.KubeGroup, V1Ingress.KubeApiVersion, V1Ingress.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Ingresses", SingularDisplayName: "Ingress");
    public static ResourceType IngressClass => new(V1IngressClass.KubeGroup, V1IngressClass.KubeApiVersion, V1IngressClass.KubePluralName, IsNamespaceScoped: false, PluralDisplayName: "Ingress Classes", SingularDisplayName: "Ingress Class");
    public static ResourceType NetworkPolicy => new(V1NetworkPolicy.KubeGroup, V1NetworkPolicy.KubeApiVersion, V1NetworkPolicy.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Network Policies", SingularDisplayName: "Network Policy");
    public static ResourceType PersistentVolumeClaim => new(V1PersistentVolumeClaim.KubeGroup, V1PersistentVolumeClaim.KubeApiVersion, V1PersistentVolumeClaim.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Persistent Volume Claims", SingularDisplayName: "Persistent Volume Claim");
    public static ResourceType PersistentVolume => new(V1PersistentVolume.KubeGroup, V1PersistentVolume.KubeApiVersion, V1PersistentVolume.KubePluralName, IsNamespaceScoped: false, PluralDisplayName: "Persistent Volumes", SingularDisplayName: "Persistent Volume");
    public static ResourceType StorageClass => new(V1StorageClass.KubeGroup, V1StorageClass.KubeApiVersion, V1StorageClass.KubePluralName, IsNamespaceScoped: false, PluralDisplayName: "Storage Classes", SingularDisplayName: "Storage Class");
    public static ResourceType ServiceAccount => new(V1ServiceAccount.KubeGroup, V1ServiceAccount.KubeApiVersion, V1ServiceAccount.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Service Accounts", SingularDisplayName: "Service Account");
    public static ResourceType ClusterRole => new(V1ClusterRole.KubeGroup, V1ClusterRole.KubeApiVersion, V1ClusterRole.KubePluralName, IsNamespaceScoped: false, PluralDisplayName: "Cluster Roles", SingularDisplayName: "Cluster Role");
    public static ResourceType Role => new(V1Role.KubeGroup, V1Role.KubeApiVersion, V1Role.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Roles", SingularDisplayName: "Role");
    public static ResourceType ClusterRoleBinding => new(V1ClusterRoleBinding.KubeGroup, V1ClusterRoleBinding.KubeApiVersion, V1ClusterRoleBinding.KubePluralName, IsNamespaceScoped: false, PluralDisplayName: "Cluster Role Bindings", SingularDisplayName: "Cluster Role Binding");
    public static ResourceType RoleBinding => new(V1RoleBinding.KubeGroup, V1RoleBinding.KubeApiVersion, V1RoleBinding.KubePluralName, IsNamespaceScoped: true, PluralDisplayName: "Role Bindings", SingularDisplayName: "Role Binding");
    public static ResourceType CustomResourceDefinition => new(V1CustomResourceDefinition.KubeGroup, V1CustomResourceDefinition.KubeApiVersion, V1CustomResourceDefinition.KubePluralName, IsNamespaceScoped: false, PluralDisplayName: "Definitions", SingularDisplayName: "Custom Resource Definition");

}
